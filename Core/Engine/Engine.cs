///
/// WF.Player.Core - A Wherigo Player Core for different platforms.
/// Copyright (C) 2012-2013  Dirk Weltz <web@weltz-online.de>
/// Copyright (C) 2012-2013  Brice Clocher <contact@cybisoft.net>
///
/// This program is free software: you can redistribute it and/or modify
/// it under the terms of the GNU Lesser General Public License as
/// published by the Free Software Foundation, either version 3 of the
/// License, or (at your option) any later version.
/// 
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
/// GNU Lesser General Public License for more details.
/// 
/// You should have received a copy of the GNU Lesser General Public License
/// along with this program.  If not, see <http://www.gnu.org/licenses/>.
///

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using NLua;
using System.Windows;
using System.Collections;

namespace WF.Player.Core
{

    /// <summary>
    /// Engine handling all things for the cartridge, including loading, saving and other things.
    /// </summary>
    #if MONOTOUCH
	    [MonoTouch.Foundation.Preserve(AllMembers=true)]
    #endif
    public class Engine : IDisposable, INotifyPropertyChanged
    {

        #region Private variables

		private Cartridge cartridge;
		private double lat = 0;
		private double lon = 0;
		private double alt = 0;
		private double accuracy = 0;
		private double heading = 0;
		private string device = "unknown";
		private string deviceId = "unknown";
		private string uiVersion = "unknown";
		private List<Thing> visibleInventory;
		private List<Thing> visibleObjects;
		private List<Zone> activeVisibleZones;
		private List<Task> activeVisibleTasks;
		private EngineGameState gameState;
		private bool isReady;
        private Lua luaState;
		private LuaExecutionQueue luaExecQueue;
        private WIGInternalImpl wherigo;
        private LuaTable player;
		private Dictionary<int, System.Threading.Timer> timers = new Dictionary<int, System.Threading.Timer>();
		private Dictionary<int,UIObject> uiObjects = new Dictionary<int, UIObject> ();
		private object syncRoot = new object();

        #endregion

		#region Constants

		private const int internalTimerDuration = 1000;

		#endregion

        #region Engine version

        public static readonly string CorePlatform = "WF.Player.Core";
        public static readonly string CoreVersion = "0.2.0";

        #endregion

		#region Event handlers

		public event EventHandler<AttributeChangedEventArgs> AttributeChanged;
		public event EventHandler<CartridgeEventArgs> CartridgeCompleted;
		public event EventHandler<CartridgeEventArgs> SaveRequested;
		public event EventHandler<ObjectEventArgs<Command>> CommandChanged;
		public event EventHandler<ObjectEventArgs<Input>> InputRequested;
		public event EventHandler<InventoryChangedEventArgs> InventoryChanged;
		public event EventHandler<LogMessageEventArgs> LogMessageRequested;
		public event EventHandler<NotifyOSEventArgs> NotifyOS;
		public event EventHandler<ObjectEventArgs<Media>> PlayMediaRequested;
		public event EventHandler<MessageBoxEventArgs> ShowMessageBoxRequested;
		public event EventHandler<ScreenEventArgs> ShowScreenRequested;
		public event EventHandler<StatusTextEventArgs> ShowStatusTextRequested;
		public event EventHandler<SynchronizeEventArgs> SynchronizeRequested;
		public event EventHandler<ZoneStateChangedEventArgs> ZoneStateChanged;
		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

        #region Constructor and Destructors

        public Engine()
        {			
			luaState = new Lua();

            // Create Wherigo environment
            wherigo = new WIGInternalImpl(this, luaState);

            // Register events
            wherigo.OnTimerStarted += HandleTimerStarted;
            wherigo.OnTimerStopped += HandleTimerStopped;
			wherigo.OnCartridgeChanged += HandleCartridgeChanged;
			wherigo.OnZoneStateChanged += HandleZoneStateChanged;
			wherigo.OnInventoryChanged += HandleInventoryChanged;
			wherigo.OnAttributeChanged += HandleAttributeChanged;
			wherigo.OnCommandChanged += HandleCommandChanged;

			// Set definitions from Wherigo for ShowScreen
			luaState ["Wherigo.MAINSCREEN"] = (int)ScreenType.Main;
			luaState ["Wherigo.LOCATIONSCREEN"] = (int)ScreenType.Locations;
			luaState ["Wherigo.ITEMSCREEN"] = (int)ScreenType.Items;
			luaState ["Wherigo.INVENTORYSCREEN"] = (int)ScreenType.Inventory;
			luaState ["Wherigo.TASKSCREEN"] = (int)ScreenType.Tasks;
			luaState ["Wherigo.DETAILSCREEN"] = (int)ScreenType.Details;

            // Set definitions from Wherigo for LogMessage
			luaState ["Wherigo.LOGDEBUG"] = (int)LogLevel.Debug;
			luaState ["Wherigo.LOGCARTRIDGE"] = (int)LogLevel.Cartridge;
			luaState ["Wherigo.LOGINFO"] = (int)LogLevel.Info;
			luaState ["Wherigo.LOGWARNING"] = (int)LogLevel.Warning;
			luaState ["Wherigo.LOGERROR"] = (int)LogLevel.Error;

            // Get information about the player
            // Create table for Env, ...
			luaState.NewTable ("Env");
			LuaTable env = luaState.GetTable ("Env");

            // Set defaults
            env["CartFolder"] = "nothing";
            env["SyncFolder"] = "nothing";
            env["LogFolder"] = "nothing";
            env["PathSep"] = System.IO.Path.DirectorySeparatorChar;
            env["Downloaded"] = 0.0;
            env["Platform"] = CorePlatform;
            env["Device"] = device;
            env["DeviceID"] = deviceId;
            env["Version"] = uiVersion + " (" + CorePlatform + " " + CoreVersion + ")";

			// Creates an execution queue that runs in another thread.
			luaExecQueue = new LuaExecutionQueue(luaState);

			// Sets the game state.
			GameState = EngineGameState.Uninitialized;
        }

		~Engine()
		{
			Dispose();
		}

		public void Dispose()
		{			
			// Sets the state as disposed. Returns if it is already.
			lock (syncRoot)
			{
				if (gameState == EngineGameState.Disposed)
				{
					return;
				}

				gameState = EngineGameState.Disposed;
			}

			// Bye bye threads.
			if (luaExecQueue != null)
			{
				luaExecQueue.Dispose();
				luaExecQueue = null;
			}
			
			// Disposes the underlying objects.
			if (luaState != null)
			{
				lock (luaState)
				{
					luaState.Dispose(); 
				}
				luaState = null;
			}

			// Requests the GC to not finalize this object (best practice).
			GC.SuppressFinalize(this);			
		}

        #endregion

        #region Properties

		#region Public
		public double Altitude 
		{ 
			get 
			{
				lock (syncRoot)
				{
					return alt;
				}
			}
		}

		public double Accuracy 
		{ 
			get 
			{
				lock (syncRoot)
				{
					return accuracy;
				}
			} 
		}

		public Cartridge Cartridge 
		{ 
			get 
			{
				lock (syncRoot)
				{
					return cartridge;
				}
			} 
		}

		public string Device
		{
			get
			{
				lock (syncRoot)
				{
					return device;
				}
			}
			set
			{
				CheckStateForLuaAccess();

				if (device != value)
				{
					lock (syncRoot)
					{
						device = value; 
					}
					lock (luaState)
					{
						luaState.GetTable("Env")["Device"] = device;
					}
				}
			}
		}

		public string DeviceId
		{
			get
			{
				lock (syncRoot)
				{
					return deviceId; 
				}
			}
			set
			{
				CheckStateForLuaAccess();

				if (deviceId != value)
				{
					lock (syncRoot)
					{
						deviceId = value;
					}
					lock (luaState)
					{
						luaState.GetTable("Env")["DeviceId"] = deviceId; 
					}
				}
			}
		}

		public string UIVersion
		{
			get
			{
				lock (syncRoot)
				{
					return uiVersion; 
				}
			}
			set
			{
				CheckStateForLuaAccess();
				if (uiVersion != value)
				{
					lock (syncRoot)
					{
						uiVersion = value; 
					}
					lock (luaState)
					{
						luaState.GetTable("Env")["Version"] = uiVersion + " (" + CorePlatform + " " + CoreVersion + ")"; 
					}
				}
			}
		}

		public double Heading
		{ 
			get 
			{
				lock (syncRoot)
				{
					return heading;  
				}
			} 
		}

		public double Latitude
		{ 
			get 
			{
				lock (syncRoot)
				{
					return lat;  
				}
			} 
		}

		public double Longitude
		{ 
			get 
			{
				lock (syncRoot)
				{
					return lon;
				}
			} 
		}

		public Character Player 
		{ 
			get 
			{
				LuaTable p;
				lock (syncRoot)
				{
					p = player;
				}

				if (p == null)
				{
					return null;
				}

				return (Character)GetTable(player); 
			} 
		}

		public List<Task> ActiveVisibleTasks
		{
			get
			{
				lock (syncRoot)
				{
					return activeVisibleTasks ?? new List<Task>();
				}
			}

			private set
			{
				bool valueChanged = false;

				// Thread-safely sets the value.
				lock (syncRoot)
				{
					if (activeVisibleTasks != value)
					{
						activeVisibleTasks = value;
						valueChanged = true;
					}
				}

				// Raises the property changed event.
				if (valueChanged)
					RaisePropertyChanged("ActiveVisibleTasks");
			}
		}

		public List<Zone> ActiveVisibleZones
		{
			get
			{
				lock (syncRoot)
				{
					return activeVisibleZones ?? new List<Zone>();
				}
			}

			private set
			{
				bool valueChanged = false;

				// Thread-safely sets the value.
				lock (syncRoot)
				{
					if (activeVisibleZones != value)
					{
						activeVisibleZones = value;
						valueChanged = true;
					}
				}

				// Raises the property changed event.
				if (valueChanged)
					RaisePropertyChanged("ActiveVisibleZones");
			}
		}

		public List<Thing> VisibleInventory
		{
			get
			{
				lock (syncRoot)
				{
					return visibleInventory ?? new List<Thing>();
				}
			}

			private set
			{
				bool valueChanged = false;

				// Thread-safely sets the value.
				lock (syncRoot)
				{
					if (visibleInventory != value)
					{
						visibleInventory = value;
						valueChanged = true;
					}
				}

				// Raises the property changed event.
				if (valueChanged)
					RaisePropertyChanged("VisibleInventory");
			}
		}

		public List<Thing> VisibleObjects
		{
			get
			{
				lock (syncRoot)
				{
					return visibleObjects ?? new List<Thing>();
				}
			}

			private set
			{
				bool valueChanged = false;

				// Thread-safely sets the value.
				lock (syncRoot)
				{
					if (visibleObjects != value)
					{
						visibleObjects = value;
						valueChanged = true;
					}
				}

				// Raises the property changed event.
				if (valueChanged)
					RaisePropertyChanged("VisibleObjects");
			}
		}

		public EngineGameState GameState
		{
			get
			{
				lock (syncRoot)
				{
					return gameState;
				}
			}

			private set
			{
				EngineGameState gs;
				lock (syncRoot)
				{
					gs = gameState;
				}

				if (gs != value)
				{
					// Changes the game state.
					lock (syncRoot)
					{
						gameState = value; 
					}
					RaisePropertyChanged("GameState");

					// Checks if IsReady needs to be changed.
					bool newIsReady = value != EngineGameState.Uninitialized
						&& value != EngineGameState.Initializing
						&& value != EngineGameState.Disposed;
					if (newIsReady != IsReady)
					{
						// Changes IsReady.
						IsReady = newIsReady; 
					}
				}
			}
		}

		public bool IsReady
		{
			get
			{
				lock (syncRoot)
				{
					return isReady;
				}
			}

			private set
			{
				bool ir;
				lock (syncRoot)
				{
					ir = isReady;
				}

				if (ir != value)
				{
					lock (syncRoot)
					{
						isReady = value;
					}
					RaisePropertyChanged("IsReady");
				}
			}
		}

		#endregion

		#region Internal

		internal LuaExecutionQueue LuaExecQueue { get { return luaExecQueue; } }

		#endregion

		#endregion

        #region Start/Stop/Load/Save

        /// <summary>
        /// Start engine.
        /// </summary>
        public void Start()
        {
			// Sanity checks.
			CheckStateForLuaAccess();
			CheckStateForConcurrentGameOperation();
			CheckStateIsNot(EngineGameState.Playing, "The engine is already playing.");
			
			GameState = EngineGameState.Starting;

			// Starts the game.
			LuaExecQueue.BeginCallSelf(cartridge.WIGTable, "Start");
			LuaExecQueue.WaitEmpty();

			// Refreshes the values.
			RefreshActiveVisibleTasksAsync();
			RefreshActiveVisibleZonesAsync();
			RefreshVisibleInventoryAsync();
			RefreshVisibleObjectsAsync();

			GameState = EngineGameState.Playing;
        }

        /// <summary>
        /// Stop engine.
        /// </summary>
        public void Stop()
        {
			// Sanity checks.
			CheckStateForLuaAccess();
			CheckStateForConcurrentGameOperation();
			CheckStateIsNot(EngineGameState.Initialized, "The engine is aldreay stopped.");
			
			GameState = EngineGameState.Stopping;

			foreach(System.Threading.Timer t in timers.Values)
				t.Dispose ();

			HandleNotifyOS ("StopSound");

			LuaExecQueue.BeginCallSelf(cartridge.WIGTable, "Stop");
			LuaExecQueue.WaitEmpty();

			GameState = EngineGameState.Initialized;
		}

        /// <summary>
        /// Start engine and restore a saved cartridge.
        /// </summary>
        /// <param name="stream">Stream, where the cartridge load from.</param>
        public void Restore(Stream stream)
        {
			// Sanity checks.
			CheckStateForLuaAccess();
			CheckStateForConcurrentGameOperation();
			CheckStateIsNot(EngineGameState.Playing, "The engine is aldreay playing.");
			
			GameState = EngineGameState.Restoring;
			
			LoadGWS(stream);

			LuaExecQueue.BeginCallSelf(cartridge.WIGTable, "OnRestore");
			LuaExecQueue.WaitEmpty();

			GameState = EngineGameState.Playing;
        }

        /// <summary>
        /// Load and init all data belonging to the selected cartridge.
        /// </summary>
        /// <param name="input">Stream to load cartridge load from.</param>
        /// <param name="cartridge">Cartridge object to load and init.</param>
        public void Init(Stream input, Cartridge cartridge)
        {
			// Sanity checks.
			CheckStateIs(EngineGameState.Uninitialized, "The engine cannot be initialized in this state", true);
			
			GameState = EngineGameState.Initializing;
			
			this.cartridge = cartridge;
			cartridge.Engine = this;

            luaState["Env.CartFilename"] = cartridge.Filename;

			FileFormats.Load(input, cartridge);

			// Set player relevant data
			player = (LuaTable)luaState["Wherigo.Player"];
			player["CompletionCode"] = cartridge.CompletionCode;
			player["Name"] = cartridge.Player;
			player["ObjectLocation.latitude"] = lat;
			player["ObjectLocation.longitude"] = lon;
			player["ObjectLocation.altitude"] = alt;

			try
            {
                // Now start Lua binary chunk
                byte[] luaBytes = cartridge.Resources[0].Data;

				// TODO: Asynchronize this!
				cartridge.WIGTable = (LuaTable)luaState.DoString(luaBytes, cartridge.Filename)[0];
				player["Cartridge"] = cartridge.WIGTable;

				GameState = EngineGameState.Initialized;

            }
            catch (Exception e)
            {
                // TODO
                // Rethrow exception
                Console.WriteLine(e.Message);

				GameState = EngineGameState.Uninitialized;
            }

        }

        /// <summary>
        /// Save the cartridge.
        /// </summary>
        /// <param name="stream">Stream, where the cartridge is saved.</param>
        public void Save(Stream stream)
        {
			// Sanity checks.
			CheckStateIs(EngineGameState.Playing, "The engine is not playing.");

			// State change.
			GameState = EngineGameState.Saving;

			// Informs the cartridge that saving starts.
			LuaExecQueue.BeginCallSelf(cartridge.WIGTable, "OnSync");
			LuaExecQueue.WaitEmpty();

            // Serialize all objects
			SaveGWS(stream);

			// State change.
			GameState = EngineGameState.Playing;
        }

		#endregion

        #region User Input (Refresh)

        /// <summary>
        /// Refresh location, altitude and accuracy with new values.
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <param name="alt">Altitude</param>
        /// <param name="accuracy">Accuracy</param>
        public void RefreshLocation(double lat, double lon, double alt, double accuracy)
        {
			// Sanity checks.
			CheckStateForLuaAccess();
			
			this.lat = lat;
			this.lon = lon;
			this.alt = alt;
			this.accuracy = accuracy;

			LuaExecQueue.BeginCallSelf(player, "ProcessLocation", lat, lon, alt, accuracy);
        }

        /// <summary>
        /// Refresh compass heading of device.
        /// </summary>
        /// <param name="heading">New heading in degrees.</param>
		public void RefreshHeading(double heading)
		{
			this.heading = heading;

			// TODO: Give it out to the lua engine?
		}

        #endregion

		#region Global functions for all players

		public string CreateLogMessage(string message)
		{
			lock (syncRoot)
			{
				return String.Format("{0:yyyyMMddhhmmss}|{1:+0.00000}|{2:+0.00000}|{3:+0.00000}|{4:+0.00000}|{5}", DateTime.Now.ToLocalTime(), lat, lon, alt, accuracy, message); 
			}
		}

		#endregion

		#region Property getters backed by the Lua engine

		private void RefreshActiveVisibleTasksAsync()
		{
			// Sanity checks.
			if (!IsReady)
			{
				return;
			}

			// If we're not in the lua exec thread, be in it!
			if (!LuaExecQueue.IsSameThread)
			{
				LuaExecQueue.BeginAction(RefreshActiveVisibleTasksAsync);
				return;
			}

			if (player == null)
				ActiveVisibleTasks = null;

			// This executes in the lua exec thread, so it's fine to block.
			lock (luaState)
			{
				ActiveVisibleTasks = GetTableListFromLuaTable<Task>(player.CallSelf("GetActiveVisibleTasks")); 
			}
		}

		private void RefreshActiveVisibleZonesAsync()
		{
			// Sanity checks.
			if (!IsReady)
			{
				return;
			}

			// If we're not in the lua exec thread, be in it!
			if (!LuaExecQueue.IsSameThread)
			{
				LuaExecQueue.BeginAction(RefreshActiveVisibleZonesAsync);
				return;
			}

			if (player == null)
				ActiveVisibleZones = null;

			// This executes in the lua exec thread, so it's fine to block.
			lock (luaState)
			{
				ActiveVisibleZones = GetTableListFromLuaTable<Zone>(player.CallSelf("GetActiveVisibleZones")); 
			}
		}

		private void RefreshVisibleInventoryAsync()
		{
			// Sanity checks.
			if (!IsReady)
			{
				return;
			}

			// If we're not in the lua exec thread, be in it!
			if (!LuaExecQueue.IsSameThread)
			{
				LuaExecQueue.BeginAction(RefreshVisibleInventoryAsync);
				return;
			}

			if (player == null)
				VisibleInventory = null;

			// This executes in the lua exec thread, so it's fine to block.
			lock (luaState)
			{
				VisibleInventory = GetTableListFromLuaTable<Thing>(player.CallSelf("GetVisibleInventory")); 
			}
		}

		private void RefreshVisibleObjectsAsync()
		{
			// Sanity checks.
			if (!IsReady)
			{
				return;
			}

			// If we're not in the lua exec thread, be in it!
			if (!LuaExecQueue.IsSameThread)
			{
				LuaExecQueue.BeginAction(RefreshVisibleObjectsAsync);
				return;
			}

			if (player == null)
				VisibleObjects = null;

			// This executes in the lua exec thread, so it's fine to block.
			lock (luaState)
			{
				VisibleObjects = GetTableListFromLuaTable<Thing>(player.CallSelf("GetVisibleObjects")); 
			}
		}

		#endregion

		#region WIGInternal Events Handlers

		/// <summary>
        /// Event, which is called, if the attribute of an object has changed.
        /// </summary>
        /// <param name="t">LuaTable for object, which attribute has changed.</param>
        /// <param name="attribute">String with the name of the attribute that has changed.</param>
		internal void HandleAttributeChanged(LuaTable t, string attribute)
		{
			Table obj = GetTable(t);
			string classname;
			lock (luaState)
			{
				classname = (string)t["ClassName"]; 
			}

			if (obj != null) {
				
				// Raises the NotifyPropertyChanged event if this is a UIObject.
				if (IsUIObject(obj))
					RaisePropertyChangedInObject((UIObject)obj, attribute);

				// Refreshes the zone in order to make it fire its events.
				if (cartridge.WIGTable != null && ("Zone".Equals(classname) && "Active".Equals(attribute)))
					lock (luaState)
					{
						player.CallSelf("ProcessLocation", lat, lon, alt, accuracy); 
					}

				// Checks if an engine property has changed.
				bool isAttributeVisibleOrActive = "Active".Equals(attribute) || "Visible".Equals(attribute);
				if (isAttributeVisibleOrActive && "ZTask".Equals(classname))
				{
					// Recomputes active visible tasks and raises the property changed event.
					RefreshActiveVisibleTasksAsync();
				}
				else if (isAttributeVisibleOrActive && "Zone".Equals(classname))
				{
					// Recomputes active visible zones and raises the property changed event.
					RefreshActiveVisibleZonesAsync();
				}

				// Raises the AttributeChanged event.
				RaiseAttributeChanged(obj, attribute);

			} else {

				// Raises the NotifyPropertyChanged event if this is a Cartridge.
				if ("ZCartridge".Equals(classname))
					RaisePropertyChangedInObject(cartridge, attribute);

			}
		}
		
        /// <summary>
        /// Event, which is called, if the cartridge has changed.
        /// </summary>
        /// <param name="s">String with the name of the cartridge attribute, that has changed.</param>
		internal void HandleCartridgeChanged(string s)
		{
			string ls = s == null ? "" : s.ToLower();

			if ("complete".Equals(s))
			{
				// Marks the cartridge as completed.
				cartridge.Complete = true;

				// Raises the event.
				RaiseCartridgeCompleted(cartridge);
			}
			else if ("sync".Equals(s))
			{
				// Raises the event.
				RaiseSaveRequested(cartridge);
			}
			
		}

        /// <summary>
        /// Event, which is called, if a command has changed.
        /// </summary>
        /// <param name="c">LuaTable for command, that has changed.</param>
		internal void HandleCommandChanged(LuaTable ltCommand)
		{
			Command c = (Command)GetTable (ltCommand);

			// Raises PropertyChanged on the command's owner.
			if (c.Owner != null && IsUIObject (c.Owner))
				RaisePropertyChangedInObject((UIObject)(c.Owner), "Commands");

			// TODO: Reciprocal commands need to raise PropertyChanged on the targets too.


			// Raises the event.
			RaiseCommandChanged(c);
		}

		/// <summary>
		/// Get an input from the user interface.
		/// </summary>
		/// <param name="input">Detail object for the input.</param>
		internal void HandleGetInput (Input input)
		{
			// Raise the event.
			RaiseInputRequested(input);
		}

		/// <summary>
		/// Event, which is called, if the inventory has changed.
		/// </summary>
		/// <param name="t">LuaTable for item/character object.</param>
		/// <param name="from">LuaTable for container, there the object was.</param>
		/// <param name="to">LuaTable for container, to which the object goes.</param>
		internal void HandleInventoryChanged(LuaTable ltThing, LuaTable ltFrom, LuaTable ltTo)
		{
			Thing obj = (Thing)GetTable (ltThing);
			Thing from = (Thing)GetTable (ltFrom);
			Thing to = (Thing)GetTable (ltTo);

			// Raises the PropertyChanged events on the objects.
			if (obj != null)
				RaisePropertyChangedInObject((UIObject)obj, "Container");
			if (from != null)
				RaisePropertyChangedInObject((UIObject)from, "Inventory");
			if (to != null)
				RaisePropertyChangedInObject((UIObject)to, "Inventory");

			// Check for player inventory changes.
			if (player.Equals(ltTo) || player.Equals(ltFrom))
			{
				// Recomputes the visible inventory and raises the property changed event.
				RefreshVisibleInventoryAsync();
			}

			// Check for visible objects changes.
			if (IsZone(from) || IsZone(ltTo))
			{
				// Recomputes the visible objects and raises the property changed event.
				RefreshVisibleObjectsAsync();
			}

			// Raises the event.
			RaiseInventoryChanged(obj, from, to);
		}

		/// <summary>
		/// Logs the message via the user interface.
		/// </summary>
		/// <param name="level">Level of the message.</param>
		/// <param name="message">Text of the message.</param>
		internal void HandleLogMessage (int level, string message)
		{
			// Raise the event.
			RaiseLogMessageRequested((LogLevel)Enum.ToObject(typeof(LogLevel), level), message);
		}

		/// <summary>
		/// Notifies the user interface about a special command, which is sent from Lua.
		/// </summary>
		/// <param name="command">Name of command.</param>
		internal void HandleNotifyOS (string command)
		{
			// TODO: Replace by meaningful events.
			if (NotifyOS != null)
				NotifyOS (this, new NotifyOSEventArgs(command));
		}

		/// <summary>
		/// Play the media via user interface.
		/// </summary>
		/// <param name="type">Type of media.</param>
		/// <param name="mediaObj">Media object itself.</param>
		internal void HandlePlayMedia (int type, Media mediaObj)
		{
			// The Groundspeak engine only should give 1 as a type.
			if (type != 1)
			{
				throw new NotImplementedException(String.Format("Discarded media event had type {0}, != 1.", type));
			}
			
			// Raises the event.
			RaisePlayMediaRequested(mediaObj);
		}

		/// <summary>
		/// Show the message via the user interface.
		/// </summary>
		/// <param name="text">Text of the message.</param>
		/// <param name="media">Media which belongs to the message.</param>
		/// <param name="btn1Label">Button1 label.</param>
		/// <param name="btn2Label">Button2 label.</param>
		/// <param name="par">Callback function, which is called, if one of the buttons is pressed or the message is abondend.</param>
		internal void HandleShowMessage (string text, Media media, string btn1Label, string btn2Label, Action<string> par)
		{
			// Raise the event.
			RaiseMessageBoxRequested(new MessageBox(text, media, btn1Label, btn2Label, par));
		}

		/// <summary>
		/// Shows the screen via the user interface.
		/// </summary>
		/// <param name="screen">Screen number to show.</param>
		/// <param name="idxObj">Index of the object to show.</param>
		internal void HandleShowScreen (int screen, int idxObj)
		{
			// Gets the event parameters.
			ScreenType st = (ScreenType)Enum.ToObject(typeof(ScreenType), screen);
			UIObject obj = st == ScreenType.Details && idxObj > -1 ? (UIObject)GetObject(idxObj) : null;

			// Raise the event.
			RaiseScreenRequested(st, obj);
		}

		/// <summary>
		/// Shows the status text via user interface.
		/// </summary>
		/// <param name="text">Text to show.</param>
		internal void HandleShowStatusText (string text)
		{
			// Raise the event.
			RaiseShowStatusTextRequested(text);
		}


		/// <summary>
		/// Event, which is called, if the state of a zone has changed.
		/// </summary>
		/// <param name="z">LuaTable for zone object.</param>
		internal void HandleZoneStateChanged(LuaTable zones)
		{
			List<Zone> list = new List<Zone>();

			// Generates the list of zones.
			IDictionaryEnumerator z;
			bool run = true;
			lock (luaState)
			{
				z = zones.GetEnumerator();
				run = z.MoveNext();
			}
			while (run)
			{
				// Gets a zone from the table.
				Zone zone = (Zone)GetTable((LuaTable)z.Value);

				// Performs notifications.
				if (zone != null)
				{
					RaisePropertyChangedInObject((UIObject)zone, "State");
					RaisePropertyChangedInObject((UIObject)zone, "VectorFromPlayer");
				}

				// Adds the zone to the list.
				list.Add(zone);

				// Keep on running?
				lock (luaState)
				{
					run = z.MoveNext();
				}
			}


			// The list of zones and objects has changed.
			RefreshActiveVisibleZonesAsync();
			RefreshVisibleObjectsAsync();

			// Notifies all visible objects that their distances have changed.
			VisibleObjects.ForEach(t => RaisePropertyChangedInObject(t, "VectorFromPlayer"));

			// Raise the event.
			RaiseZoneStateChanged(list);
		}

        #endregion

        #region Timers

        /// <summary>
		/// Starts an OS timer corresponding to a Wherigo ZTimer.
        /// </summary>
        /// <param name="t">Timer to start.</param>
        internal void HandleTimerStarted(LuaTable t)
        {
            // Gets the object index of the Timer that started.
			int objIndex;
			lock (luaState)
			{
				objIndex = Convert.ToInt32((double)t["ObjIndex"]); 
			}

			// Initializes a corresponding internal timer, but do not start it yet.
			System.Threading.Timer timer = new System.Threading.Timer(InternalTimerTick, objIndex, Timeout.Infinite, internalTimerDuration);

			// Keeps track of the timer.
			// TODO: What happens if the timer is already in the dictionary?
			lock (syncRoot)
			{
				if (!timers.ContainsKey(objIndex))
					timers.Add(objIndex, timer); 
			}

			// Starts the timer, now that it is registered.
			timer.Change(internalTimerDuration, internalTimerDuration);

			// Call OnStart of this timer
			lock (luaState)
			{
				t.CallSelf("Start"); 
			}
        }

        /// <summary>
        /// Stops an OS timer corresponding to a Wherigo ZTimer.
        /// </summary>
        /// <param name="t">Timer to stop.</param>
        internal void HandleTimerStopped(LuaTable t)
        {
			int objIndex;
			lock (luaState)
			{
				objIndex = Convert.ToInt32((double)t["ObjIndex"]); 
			}

			// TODO: What happens if the timer is not in the dictionary?
			bool shouldRemove = false;
			lock (syncRoot)
			{
				shouldRemove = timers.ContainsKey (objIndex);
			}
			if (shouldRemove) {
				System.Threading.Timer timer = timers [objIndex];

				timer.Dispose ();
				lock (syncRoot)
				{
					timers.Remove(objIndex); 
				}
			}

			// Call OnStop of this timer
			t.CallSelf("Stop");
        }

        /// <summary>
        /// Updates the ZTimer's attributes and checks if its Tick event should be called.
        /// </summary>
        /// <param name="source">ObjIndex of the timer that released the tick.</param>
        private void InternalTimerTick(object source)
        {
			int objIndex = (int)source;

			LuaTable t = GetObject(objIndex).WIGTable;

			// Gets the ZTimer's properties.
			object elapsedRaw;
			object remainingRaw; 
			lock (luaState)
			{
				elapsedRaw = t["Elapsed"];
				remainingRaw = t["Remaining"]; 
			}
			if (elapsedRaw == null)
				elapsedRaw = 0.0d;
			if (remainingRaw == null)
				lock (luaState)
				{
					remainingRaw = t["Duration"]; 
				}

			double elapsed = (double)elapsedRaw * internalTimerDuration;
			double remaining = (double)remainingRaw * internalTimerDuration;

			// Updates the ZTimer properties and considers if it should tick.
			elapsed += internalTimerDuration;
			remaining -= internalTimerDuration;

			bool shoudTimerTick = false;
			if (remaining <= 0.0d)
			{
				remaining = 0;

				shoudTimerTick = true;
			}

			lock (luaState)
			{
				t["Elapsed"] = elapsed / internalTimerDuration;
				t["Remaining"] = remaining / internalTimerDuration; 
			}

			// Call only, if timer still exists.
			// It could be, that function is called from thread, even if the timer didn't exists anymore.
			bool timerExists = false;
			lock (syncRoot)
			{
				timerExists = timers.ContainsKey(objIndex);
			}
			if (shoudTimerTick && timerExists)
            	// Call Tick syncronized with the GUI (for not thread save interfaces)
				RaiseSynchronizeRequested(new Action(() => WherigoTimerTickCore(source)));
        }

        /// <summary>
        /// Function for tick of a timer in source.
        /// </summary>
        /// <param name="source">ObjIndex of the timer that released the tick.</param>
        private void WherigoTimerTickCore(object source)
        {
            int objIndex = (int)source;

			bool timerExists = false;
			lock (syncRoot)
			{
				timerExists = timers.ContainsKey (objIndex);
			}
			if (timerExists) {
				System.Threading.Timer timer = timers [objIndex];

				timer.Dispose ();
				lock (syncRoot)
				{
					timers.Remove(objIndex); 
				}
			}

			LuaTable t = GetObject(objIndex).WIGTable;

			// Call OnTick of this timer
			LuaExecQueue.BeginCallSelf(t, "Tick");
		}

        #endregion

        #region Retrive data from cartridge

        /// <summary>
        /// Get ZObject for given ObjIndex idx.
        /// </summary>
        /// <param name="idx">ObjIndex for ZObject.</param>
        /// <returns>LuaTable for ZObject.</returns>
		public Table GetObject(int idx)
		{
			// Sanity checks
			CheckStateForLuaAccess();

			LuaTable lt;
			lock (luaState)
			{
				lt = (LuaTable)((LuaTable)cartridge.WIGTable["AllZObjects"])[idx]; 
			}

			return idx == -1 ? null : GetTable (lt);
		}

		/// <summary>
		/// Gets the distance and bearing vector from the player to a thing.
		/// </summary>
		/// <param name="thing"></param>
		/// <returns></returns>
		internal LocationVector GetVectorFromPlayer(Thing thing)
		{
			// Sanity checks.
			CheckStateForLuaAccess();
			
			object thingLoc;
			lock (luaState)
			{
				thingLoc = thing.WIGTable["ObjectLocation"]; 
			}
			bool isZone = thing is Zone;

			// If the Thing is not a zone and has no location, consider it is close to the player.
			if (!isZone && thingLoc == null)
			{
				LuaTable lt;
				lock (luaState)
				{
					lt = (LuaTable)luaState.DoString("return Wherigo.Distance(0)")[0];
				}
				return new LocationVector((Distance)GetTable(lt), 0);
			}

			object[] ret;
			if (isZone)
			{
				lock (luaState)
				{
					ret = luaState.GetFunction("WIGInternal.VectorToZone").Call(new object[] { player["ObjectLocation"], thing.WIGTable }); 
				}
			}
			else
			{
				lock (luaState)
				{
					ret = luaState.GetFunction("WIGInternal.VectorToPoint").Call(new object[] { player["ObjectLocation"], thingLoc }); 
				}
			}

			return new LocationVector((Distance)GetTable((LuaTable)ret[0]), (double)ret[1]);
		}

        #endregion

		#region Wherigo Objects Type Checkers
		/// <summary>
		/// Check, if the given object is a ZCartridge object.
		/// </summary>
		/// <returns><c>true</c> if obj is a ZCartridge object; otherwise, <c>false</c>.</returns>
		/// <param name="obj">LuaTable with object to check.</param>
		private bool IsCartridge(object obj)
		{
			return obj is Cartridge || IsLuaTableWithClassName(obj, "ZCartridge");
		}

		/// <summary>
		/// Check, if the given object is a ZCharacter object.
		/// </summary>
		/// <returns><c>true</c> if obj is a ZCharacter object; otherwise, <c>false</c>.</returns>
		/// <param name="obj">LuaTable with object to check.</param>
		private bool IsCharacter(object obj)
		{
			return obj is Character || IsLuaTableWithClassName(obj, "ZCharacter");
		}

		/// <summary>
		/// Check, if the given object is a Distance object.
		/// </summary>
		/// <returns><c>true</c> if obj is a Distance object; otherwise, <c>false</c>.</returns>
		/// <param name="obj">LuaTable with object to check.</param>
		private bool IsDistance(object obj)
		{
			return obj is Distance || IsLuaTableWithClassName(obj, "Distance");
		}

		/// <summary>
		/// Check, if the given element is in the Inventory.
		/// </summary>
		/// <returns><c>true</c> if obj is in the Inventory of player; otherwise, <c>false</c>.</returns>
		/// <param name="obj">LuaTable with object to check.</param>
		public bool IsInInventory(Thing obj)
		{
			if (player == null)
				return false;

			return Player.Inventory.Contains(obj);
		}

		/// <summary>
		/// Check, if the given object is a ZItem object.
		/// </summary>
		/// <returns><c>true</c> if obj is a ZItem object; otherwise, <c>false</c>.</returns>
		/// <param name="obj">LuaTable with object to check.</param>
		private bool IsItem(object obj)
		{
			return obj is Item || IsLuaTableWithClassName(obj, "ZItem");
		}

		/// <summary>
		/// Check, if the given object is a ZTask object.
		/// </summary>
		/// <returns><c>true</c> if obj is a ZTask object; otherwise, <c>false</c>.</returns>
		/// <param name="obj">LuaTable with object to check.</param>
		private bool IsTask(object obj)
		{
			return obj is Task || IsLuaTableWithClassName(obj, "ZTask");
		}

		/// <summary>
		/// Check, if the given object is a Thing object.
		/// </summary>
		/// <returns><c>true</c> if obj is a Thing object; otherwise, <c>false</c>.</returns>
		/// <param name="obj">LuaTable with object to check.</param>
		private bool IsThing(object obj)
		{
			return obj is Thing || IsLuaTableWithClassName(obj, new string[] { "Zone", "ZCharacter", "ZItem" });
		}

		/// <summary>
		/// Check, if the given object is a UIObject.
		/// </summary>
		/// <returns><c>true</c> if obj is a UIObject; otherwise, <c>false</c>.</returns>
		/// <param name="obj">LuaTable with object to check.</param>
		private bool IsUIObject(object obj)
		{
			return obj is UIObject || IsLuaTableWithClassName(obj, new string[] { "Zone", "ZTask", "ZCharacter", "ZItem" });
		}

		/// <summary>
		/// Check, if the given object is a Zone object.
		/// </summary>
		/// <returns><c>true</c> if obj is a Zone object; otherwise, <c>false</c>.</returns>
		/// <param name="obj">LuaTable with object to check.</param>
		private bool IsZone(object obj)
		{
			return obj is Zone || IsLuaTableWithClassName(obj, "Zone");
		}

		/// <summary>
		/// Checks if an object is a LuaTable with a specific ClassName.
		/// </summary>
		/// <param name="obj">Object to check.</param>
		/// <param name="classname">ClassName to check for.</param>
		/// <returns>True if and only if <paramref name="obj"/> is a <code>LuaTable</code>
		/// whose <code>ClassName</code> field is equals to <paramref name="classname"/>.</returns>
		private bool IsLuaTableWithClassName(object obj, string classname)
		{
			return IsLuaTableWithClassName(obj, new string[] { classname });
		}

		/// <summary>
		/// Checks if an object is a LuaTable with a specific ClassName.
		/// </summary>
		/// <param name="obj">Object to check.</param>
		/// <param name="classname">ClassNames to check for.</param>
		/// <returns>True if and only if <paramref name="obj"/> is a <code>LuaTable</code>
		/// whose <code>ClassName</code> field is equals to one of the string of
		/// <paramref name="classnames"/>.</returns>
		private bool IsLuaTableWithClassName(object obj, IEnumerable<string> classnames)
		{
			LuaTable lt = obj as LuaTable;
			string cn;
			lock (luaState)
			{
				cn = lt != null ? lt["ClassName"] as string : null; 
			}

			foreach (string classname in classnames)
				if (String.Equals(cn, classname))
					return true;

			return false;
		}
		#endregion

        #region Helpers

		/// <summary>
		/// Gets a list of Table entities from a LuaTable.
		/// </summary>
		/// <typeparam name="T">Type of entities.</typeparam>
		/// <param name="table">LuaTable to convert.</param>
		/// <returns>A list of Table entities that corresponds to all entries of the input
		/// table that could convert to <typeparamref name="T"/>.</returns>
		internal List<T> GetTableListFromLuaTable<T>(LuaTable table) where T : Table
		{
			if (table == null)
				return null;

			List<T> result = new List<T>();

			lock (luaState)
			{
				var t = table.GetEnumerator();

				while (t.MoveNext())
				{
					T val = GetTable((LuaTable)t.Value) as T;
					if (val != null)
						result.Add(val);
				}
			}

			return result;
		}

		/// <summary>
		/// Gets a list of Table entities from a LuaTable.
		/// </summary>
		/// <typeparam name="T">Type of entities.</typeparam>
		/// <param name="table">LuaTable to convert.</param>
		/// <returns>A list of Table entities that corresponds to all entries of the input
		/// table that could convert to <typeparamref name="T"/>.</returns>
		internal List<T> GetListFromLuaTable<T>(LuaTable table)
		{
			if (table == null)
				return null;

			List<T> result = new List<T>();

			lock (luaState)
			{
				var t = table.GetEnumerator();

				while (t.MoveNext())
				{
					if (t.Value != null && t.Value is T)
						result.Add((T)t.Value);
				}
			}

			return result;
		}

		internal T SafeGetField<T>(LuaTable table, string key)
		{
			object o;
			lock (luaState)
			{
				o = table[key];
			}

			return (o != null && o is T) ? (T)o : default(T);
		}

		internal T SafeGetField<T>(LuaTable table, double key)
		{
			object o;
			lock (luaState)
			{
				o = table[key];
			}

			return (o != null && o is T) ? (T)o : default(T);
		}

		internal LuaTable SafeCallSelf(LuaTable table, string func, params object[] parameters)
		{
			lock (luaState)
			{
				return table.CallSelf(func, parameters);
			}
		}

		/// <summary>
		/// Get Media for given ObjIndex idx.
		/// </summary>
		/// <param name="idx">ObjIndex for media.</param>
		/// <returns>Media for ObjIndex.</returns>
		internal Media GetMedia(int idx)
		{
			return idx == -1 ? null : cartridge.Resources[idx];
		}

		/// <summary>
		/// Convert given LuaTable t into a valid object.
		/// </summary>
		/// <returns>The correct object.</returns>
		/// <param name="t">LuaTable for object.</param>
		internal Table GetTable(LuaTable t)
		{
			if (t == null)
				return null;

			string className;
			lock (luaState)
			{
				className = (string)t["ClassName"]; 
			}

			// Check if object is a AllZObject
			object oi;
			lock (luaState)
			{
				oi = t["ObjIndex"];
			}
			if (oi != null) 
			{
				int objIndex = Convert.ToInt32 ((double)oi);

				bool uiObjectKnown;
				lock (syncRoot)
				{
					uiObjectKnown = uiObjects.ContainsKey (objIndex);
				}
				if (uiObjectKnown)
					lock (syncRoot)
					{
						return uiObjects[objIndex]; 
					}
				else {
					Table tab = null;
					// Check for objects, that have a ObjIndex, but didn't derived from UIObject
					if (className.Equals("ZInput"))
						return new Input(this, t);
					else if (className.Equals("ZTimer"))
						return new Timer(this, t);
					// Now check for UIObjects
					else if (className.Equals("ZCharacter"))
						tab = new Character (this, t);
					else if (className.Equals("ZItem"))
						tab = new Item (this, t);
					else if (className.Equals("ZTask"))
						tab = new Task (this, t);
					else if (className.Equals("Zone"))
						tab = new Zone (this, t);
					// Save UIObject for later use
					if (tab != null)
						lock (syncRoot)
						{
							uiObjects.Add(objIndex, (UIObject)tab); 
						}
					return tab;
				}
			}
			else {
				//TODO: Delete
				if (className.Equals ("ZonePoint"))
					return new ZonePoint (this, t);
				if (className.Equals ("ZCommand"))
					return new Command (this, t);
				if (className.Equals ("ZReciprocalCommand"))
					return new Command (this, t);
				if (className.Equals("Distance"))
					return new Distance (this, t);
				return null;
			}
		}
		
        #endregion

        #region Serialization

		// TODO: Move into other class, write multithreading safeties.


        ///
        /// Save File Data Format Emulator (GWS File)
        /// 
        /// Signature: 02 0A 53 59 4E 43 00
        /// Length of header in bytes: 4 byte
        /// Name of cartridge as zero terminated string
        /// Date of creation of cartridge: 8 byte (long in seconds since 2004-02-10 01:00)
        /// Name of downloader? as zero terminated string
        /// Name of device as zero terminated string
        /// Name of player? as zero terminated string
        /// Date of saving the cartridge: 8 byte (long in seconds since 2004-02-10 01:00)
        /// Name of the save file as zero terminated string
        /// Latitude: 8 byte (double)
        /// Longitude: 8 byte (double)
        /// Altitude: 8 byte (double)
        /// 
		/// Number of objects + 1 (#AllZObjects + 1 for player): 4 byte
        /// 
        /// For number of objects
        ///   Object name length: 4 bytes
        ///   Object name: length byte
        /// End
        /// 
        /// Start of table (05)
        ///   Player as ZCharacter
        /// End of table (06)
        /// 
        /// For number of objects
        ///   Object name length: 4 bytes
        ///   Object name: length byte
        ///   Start of table
        ///     Data for each object
        ///   End of table
        /// End
        /// 
        /// 
        /// Type of entry (first byte):
        /// 
        /// 01: bool + 1 byte (0 = false / 1 = true), also nil as false (01 00)
        /// 02: number + 8 Byte for value (double)
        /// 03: string + 4 byte length + characters without \0
        /// 04: function + 4 byte length + bytes of dump
        /// 05: start of table
        /// 06: end of table
        /// 07: reference + 2 byte for ObjIndex
        /// 08: object + 4 byte length of type name string 
        /// 

        /// <summary>
        /// Load active cartridge from a gws file.
        /// </summary>
        /// <param name="stream">Stream to read the data from.</param>
        private void LoadGWS(Stream stream)
	    {
			string objectType;
			byte[] signatureGWS = new byte[] { 0x02, 0x0A, 0x53, 0x59, 0x4E, 0x43, 0x00 };

			BinaryReader input = new BinaryReader(stream);



		    // Read signature and version
		    byte[] signature = input.ReadBytes(7);

			// Check, if signature is of GWS file
			if (!signature.Equals (signatureGWS))
				throw new Exception ("Try to load a none GWS file");

            int lengthOfHeader = input.ReadInt32();
		    string cartName = readCString(input);
			DateTime cartCreateDate = new DateTime(2004,02,10,01,00,00).AddSeconds(input.ReadInt64());

			// Belongs this GWS file to the cartridge
			if (!cartCreateDate.Equals (cartridge.CreateDate))
				throw new Exception ("Try to load a GWS file with different creation date of cartridge");

            string cartPlayerName = readCString(input);
            string cartDeviceName = readCString(input);
            string cartDeviceID = readCString(input);
            DateTime cartSaveDate = new DateTime(2004,02,10,01,00,00).AddSeconds(input.ReadInt64());
            string cartSaveName = readCString(input);
            input.ReadDouble();	// Latitude of last position
            input.ReadDouble();	// Longitude of last position
            input.ReadDouble();	// Altitude of last position

            // TODO
            // Check, if all fields are the same as the fields from the GWC cartridge.
            // If not, than ask, if we should go on, even it could get problems.

            int numOfObjects = input.ReadInt32();
			int numAllZObjects = ((LuaTable)cartridge.WIGTable["AllZObjects"]).Keys.Count;
                
            for (int i = 1; i < numOfObjects; i++)
            {
                objectType = readString(input);
                if (i > numAllZObjects)
                {
                    // Create new objects
                    // TODO: Cartridge=
                    luaState.DoString("Wherigo." + objectType + "()", "");
                }
                else
                {
                    // TODO: Check, if objectType and real type of object are the same
                }
            }

			LuaTable obj = player;
			objectType = readString(input);

			byte b = input.ReadByte();

			readTable(input,obj);

            for (int i = 0; i < numAllZObjects; i++)
            {
                objectType = readString(input);
                b = input.ReadByte();
                if (b != 5)
                {
                    // error
                }
                else
                {
                    obj = (LuaTable)((LuaTable)cartridge.WIGTable["AllZObjects"])[i];
                    readTable(input, obj);
					LuaExecQueue.BeginCallSelf(obj, "deserialize");
                }
            }

            input.Close();
        }

        /// <summary>
        /// Read the table obj from the binary reader input. 
        /// </summary>
        /// <param name="output">BinaryReader to read the table from.</param>
        /// <param name="obj">Table to read from binary writer.</param>
        private void readTable(BinaryReader input, LuaTable obj)
        {
			string className = "unknown";
			LuaFunction rawset = null;
			LuaTable tab;
            object key = 1;

			if (obj != null)
			{
				className = (string)obj["ClassName"];
	            if (className != null)
    	            rawset = (LuaFunction)obj["rawset"];
			}

            byte b = input.ReadByte();

            while (b != 6)
            {
                // Key
                switch(b)
                {
                    case 1:
                        key = input.ReadByte() == 0 ? false : true;
                        break;
                    case 2:
                        key = input.ReadDouble();
                        break;
                    case 3:
                        key = readString(input);
                        break;
					default:
						throw new Exception(String.Format ("Unsupported table key: {0} at byte {1}",key,input.BaseStream.Position));
                }

                b = input.ReadByte();

                // Value
                switch(b)
                {
                    case 1:
						if (className != null)
							rawset.Call (new object[] { obj, key, input.ReadByte() == 0 ? false : true });
						else
                        	obj[key] = input.ReadByte() == 0 ? false : true;
                        break;
                    case 2:
						if (className != null)
							rawset.Call (new object[] { obj, key, input.ReadDouble() });
						else
							obj[key] = input.ReadDouble();
						break;
                    case 3:
						if (className != null)
							rawset.Call (new object[] { obj, key, readString(input) });
						else
							obj[key] = readString(input);
                        break;
                    case 4:
						byte[] chunk = input.ReadBytes(input.ReadInt32());
						if (className != null)
							rawset.Call (new object[] { obj, key, (LuaFunction)luaState.LoadString(chunk, key.ToString()) });
						else
							obj[key] = (LuaFunction)luaState.LoadString(chunk, key.ToString());
                        break;
                    case 5:
						tab = luaState.EmptyTable();
						if (className != null)
							rawset.Call (new object[] { obj, key, tab } );
						else
							obj[key] = tab;
						readTable(input,(LuaTable)obj[key]);
                        break;
                    case 7:
						if (className != null)
							rawset.Call (new object[] { obj, key, (LuaTable)((LuaTable)cartridge.WIGTable["AllZObjects"])[input.ReadInt16()] } );
						else
							obj[key] = (LuaTable)((LuaTable)cartridge.WIGTable["AllZObjects"])[input.ReadInt16()];
                        break;
                    case 8:
						tab = (LuaTable)luaState.DoString("return Wherigo." + readString(input) + "()", "")[0];
						if (className != null)
							rawset.Call (new object[] { obj, key, tab });
						else
							obj[key] = tab;
						// After an object, there is allways a table with the content
                        input.ReadByte();
                        readTable(input,tab);
                        break;
                }

                b = input.ReadByte();
            }
        }

        /// <summary>
        /// Save active cartridge to a gws file.
        /// </summary>
        /// <param name="stream">Stream to write the data to.</param>
        /// <param name="saveName">Description for the save file, which is put into the file.</param>
        private void SaveGWS(Stream stream,string saveName = "UI initiated sync")
        {
            BinaryWriter output = new BinaryWriter(stream);
			byte[] className;

            // Write signature and version
            output.Write(new byte[] { 0x02, 0x0A, 0x53, 0x59, 0x4E, 0x43, 0x00 });
			output.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 });
			int lengthOfHeader = 0;
            lengthOfHeader += writeCString(output,cartridge.Name);
			output.Write (BitConverter.GetBytes ((long)((cartridge.CreateDate.Ticks - new DateTime(2004,02,10,01,00,00).Ticks) / TimeSpan.TicksPerSecond)));
            lengthOfHeader += 8;
            lengthOfHeader += writeCString(output, cartridge.Player);
			// MUST be "Windows PPC" for Emulator
			lengthOfHeader += writeCString (output, "Windows PPC"); // TODO: Replace with ui.GetDevice ());
			// MUST be "Desktop" for Emulator
			lengthOfHeader += writeCString (output, "Desktop"); // TODO: Replace with ui.GetDeviceId());
			output.Write (BitConverter.GetBytes ((long)((DateTime.Now.Ticks - new DateTime(2004,02,10,01,00,00).Ticks) / TimeSpan.TicksPerSecond)));
            lengthOfHeader += 8;
			lengthOfHeader += writeCString (output, saveName);
            output.Write(BitConverter.GetBytes(lat));
            lengthOfHeader += 8;
            output.Write(BitConverter.GetBytes(lon));
            lengthOfHeader += 8;
            output.Write(BitConverter.GetBytes(alt));
            lengthOfHeader += 8;

            var pos = output.BaseStream.Position;
			output.BaseStream.Position = 7;
            output.Write(BitConverter.GetBytes(lengthOfHeader));
			output.BaseStream.Position = pos;

			int numAllZObjects = ((LuaTable)cartridge.WIGTable["AllZObjects"]).Keys.Count;
			output.Write(numAllZObjects);

            for (int i = 1; i < numAllZObjects; i++)
            {
                className = Encoding.UTF8.GetBytes((string)((LuaTable)((LuaTable)cartridge.WIGTable["AllZObjects"])[i])["ClassName"]);
                output.Write(className.Length);
                output.Write(className);
            }

            LuaTable obj = player;
			className = Encoding.UTF8.GetBytes((string)obj["ClassName"]);
			output.Write(className.Length);
			output.Write(className);
			LuaTable data = obj.CallSelf("serialize");
            writeTable(output, data);

            for (int i = 0; i < numAllZObjects; i++)
            {
                className = Encoding.UTF8.GetBytes((string)((LuaTable)((LuaTable)cartridge.WIGTable["AllZObjects"])[i])["ClassName"]);
                output.Write(className.Length);
                output.Write(className);
                obj = (LuaTable)((LuaTable)cartridge.WIGTable["AllZObjects"])[i];
				data = obj.CallSelf("serialize");
                writeTable(output,data);
            }

            output.Flush();
            output.Close();
        }

        /// <summary>
        /// Write the table obj to the binary writer output. 
        /// </summary>
        /// <param name="output">BinaryWriter to write the table to.</param>
        /// <param name="obj">Table to write to binary writer.</param>
        private void writeTable(BinaryWriter output, LuaTable obj)
        {
            output.Write((byte)5);

            var entry = obj.GetEnumerator();
            while (entry.MoveNext())
            {
                // Save key
                if (entry.Key is bool)
                {
                    output.Write((byte)1);
                    output.Write((bool)entry.Key ? (byte)1 : (byte)0);
                }
                if (entry.Key is double)
                {
                    output.Write((byte)2);
                    output.Write((double)entry.Key);
                }
                if (entry.Key is string)
                {
                    output.Write((byte)3);
                    byte[] array = Encoding.UTF8.GetBytes((string)entry.Key);
                    output.Write(array.Length);
                    output.Write(array);
                }

                // Save value
                if (entry.Value is bool)
                {
                    output.Write((byte)1);
                    output.Write((bool)entry.Value ? (byte)1 : (byte)0);
                }
                if (entry.Value is double)
                {
                    output.Write((byte)2);
                    output.Write((double)entry.Value);
                }
                if (entry.Value is string)
                {
                    output.Write((byte)3);
					byte[] array = toArray((string)entry.Value);
                    output.Write(array.Length);
                    output.Write(array);
                }
                if (entry.Value is LuaFunction)
                {
                    output.Write((byte)4);
					byte[] array = toArray((string)luaState.GetFunction ("string.dump").Call ((LuaFunction)entry.Value) [0]);
					// TODO: Delete
//					string path = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
//					string filePath = Path.Combine(path, "out.txt");
//					BinaryWriter bw = new BinaryWriter (new FileStream(filePath,FileMode.Create));
//					bw.Write (array);
//					bw.Close ();
                    output.Write(array.Length);
                    output.Write(array);
                }
                if (entry.Value is LuaTable)
                {
                    string className = (string)((LuaTable)entry.Value)["ClassName"];

					if (className != null && (className.Equals ("Distance") || className.Equals ("ZonePoint") || className.Equals ("ZCommand") || className.Equals ("ZReciprocalCommand")))
                    {
	                    output.Write((byte)8);
                        byte[] array = Encoding.UTF8.GetBytes(className);
                        output.Write(array.Length);
                        output.Write(array);
						LuaTable data = ((LuaTable)entry.Value).CallSelf("serialize");
						writeTable (output, data);
					}
					else if (className != null && (className.Equals ("ZCartridge") || className.Equals ("ZCharacter") || className.Equals ("ZInput") || className.Equals ("ZItem") || 
					    className.Equals ("ZMedia") || className.Equals ("Zone") || className.Equals ("ZTask") || className.Equals ("ZTimer")))
					{
	                    output.Write((byte)7);
                        output.Write(Convert.ToInt16(((LuaTable)entry.Value)["ObjIndex"]));
					}
					else
					{
						LuaTable data = (LuaTable)entry.Value;
						if (((LuaTable)entry.Value)["serialize"] is LuaFunction)
							data = ((LuaTable)entry.Value).CallSelf("serialize");
						writeTable(output, data); 
                    }
                }
            }

            output.Write((byte)6);
        }

        /// <summary>
        /// Read a null terminated string from binary stream.
        /// </summary>
        /// <param name="reader">Binary stream with file as input.</param>
        /// <returns>String, which represents the C# string.</returns>
		private string readCString(BinaryReader input)
		{
			var bytes = new List<byte>();
			byte b;

			while ((b = input.ReadByte()) != 0)
			{
				bytes.Add(b);
			}

            return Encoding.UTF8.GetString(bytes.ToArray(), 0, bytes.ToArray().Length);
		}

        /// <summary>
        /// Write a null terminated string to binary stream.
        /// </summary>
        /// <param name="reader">Binary stream with file as output.</param>
        /// <returns>String, which represents the C# string.</returns>	
		private int writeCString(BinaryWriter output, string str)
		{
			byte[] array = Encoding.UTF8.GetBytes(str);
			
			output.Write(array);
			output.Write((byte)0);
			
			return array.Length+1;
		}

        /// <summary>
        /// Read string from binary reader. First four bytes are the length, the next length bytes are the string. 
        /// </summary>
        /// <param name="input">BinaryReader to read from.</param>
        /// <returns>String readed from binary reader.</returns>
		private string readString(BinaryReader input)
        {
            var b = input.ReadBytes(input.ReadInt32()).ToArray();

            return Encoding.UTF8.GetString(b, 0, b.Length);
        }

        /// <summary>
        /// Convert byte array to a string.
        /// </summary>
        /// <param name="array">Byte array to convert to string.</param>
        /// <returns>String with converted byte array.</returns>
        private string toString(byte[] array)
        {
			StringBuilder s = new StringBuilder(array.Length);

			foreach (byte b in array)
				s.Append(b);

			return s.ToString();
        }

        /// <summary>
        /// Convert string to byte array.
        /// </summary>
        /// <param name="str">String to convert to byte array.</param>
        /// <returns>Byte array with converted string.</returns>
		private byte[] toArray(string str)
		{
			if (str == null)
				return null;

			byte[] result = new byte[str.Length];

			for (int i = 0; i < str.Length; i++)
				result[i] = (byte)(str[i] & 0xff);

			return result;
		}

        #endregion

		#region Event Raisers

		/// <summary>
		/// Asynchronously invokes an action to be run in the UI thread.
		/// </summary>
		/// <param name="action"></param>
		private void BeginInvokeOnUIDispatcher(Action action)
		{
#if SILVERLIGHT
			// Silverlight and Windows Phone's way.
			Deployment.Current.Dispatcher.BeginInvoke(
#else
			// WPF's way.
			Application.Current.Dispatcher.BeginInvoke(
#endif
				action
			);
		}

		private void RaisePropertyChangedInObject(UIObject obj, string propName)
		{
			BeginInvokeOnUIDispatcher(() =>
			{
				obj.NotifyPropertyChanged(propName);
			});
		}

		private void RaisePropertyChangedInObject(Cartridge obj, string propName)
		{
			BeginInvokeOnUIDispatcher(() =>
			{
				obj.NotifyPropertyChanged(propName);
			});
		}

		private void RaisePropertyChanged(string propName)
		{
			BeginInvokeOnUIDispatcher(() =>
			{
				if (PropertyChanged != null)
				{
					PropertyChanged(this, new PropertyChangedEventArgs(propName));
				}
			});
		}

		private void RaiseLogMessageRequested(LogLevel level, string message)
		{
			BeginInvokeOnUIDispatcher(() =>
			{
				if (LogMessageRequested != null)
				{
					LogMessageRequested(this, new LogMessageEventArgs(level, message));
				}
			});
		}

		private void RaiseInputRequested(Input input, bool throwIfNoHandler = true)
		{
			BeginInvokeOnUIDispatcher(() =>
			{
				if (InputRequested == null)
				{
					if (throwIfNoHandler)
					{
						throw new InvalidOperationException("No InputRequested handler has been found.");
					}
					else
					{
						return;
					}
				}

				InputRequested(this, new ObjectEventArgs<Input>(input));
			});
		}

		private void RaiseMessageBoxRequested(MessageBox mb, bool throwIfNoHandler = true)
		{
			BeginInvokeOnUIDispatcher(() =>
			{
				if (ShowMessageBoxRequested == null)
				{
					if (throwIfNoHandler)
					{
						throw new InvalidOperationException("No MessageBoxRequested handler has been found.");
					}
					else
					{
						return;
					}
				}

				ShowMessageBoxRequested(this, new MessageBoxEventArgs(mb));
			});
		}

		private void RaisePlayMediaRequested(Media media, bool throwIfNoHandler = true)
		{
			BeginInvokeOnUIDispatcher(() =>
			{
				if (PlayMediaRequested == null)
				{
					if (throwIfNoHandler)
					{
						throw new InvalidOperationException("No PlayMediaRequested handler has been found.");
					}
					else
					{
						return;
					}
				}

				PlayMediaRequested(this, new ObjectEventArgs<Media>(media));
			});
		}

		private void RaiseScreenRequested(ScreenType kind, UIObject obj, bool throwIfNoHandler = true)
		{
			BeginInvokeOnUIDispatcher(() =>
			{
				if (ShowScreenRequested == null)
				{
					if (throwIfNoHandler)
					{
						throw new InvalidOperationException("No ScreenRequested handler has been found.");
					}
					else
					{
						return;
					}
				}

				ShowScreenRequested(this, new ScreenEventArgs(kind, obj));
			});
		}

		private void RaiseInventoryChanged(Thing obj, Thing fromContainer, Thing toContainer)
		{
			BeginInvokeOnUIDispatcher(() =>
			{
				if (InventoryChanged != null)
				{
					InventoryChanged(this, new InventoryChangedEventArgs(obj, fromContainer, toContainer));
				}
			});
		}

		private void RaiseAttributeChanged(Table obj, string propName)
		{
			BeginInvokeOnUIDispatcher(() =>
			{
				if (AttributeChanged != null)
				{
					AttributeChanged(this, new AttributeChangedEventArgs(obj, propName));
				}
			});
		}

		private void RaiseCommandChanged(Command command)
		{
			BeginInvokeOnUIDispatcher(() =>
			{
				if (CommandChanged != null)
				{
					CommandChanged(this, new ObjectEventArgs<Command>(command));
				}
			});
		}

		private void RaiseCartridgeCompleted(Cartridge cartridge)
		{
			BeginInvokeOnUIDispatcher(() =>
			{
				if (CartridgeCompleted != null)
				{
					CartridgeCompleted(this, new CartridgeEventArgs(cartridge));
				}
			});
		}

		private void RaiseSaveRequested(Cartridge cartridge, bool throwIfNoHandler = true)
		{
			BeginInvokeOnUIDispatcher(() =>
			{
				if (SaveRequested == null)
				{
					if (throwIfNoHandler)
					{
						throw new InvalidOperationException("No SaveRequested handler has been found.");
					}
					else
					{
						return;
					}
				}

				SaveRequested(this, new CartridgeEventArgs(cartridge));
			});
		}

		private void RaiseZoneStateChanged(List<Zone> list)
		{
			BeginInvokeOnUIDispatcher(() =>
			{
				if (ZoneStateChanged != null)
				{
					ZoneStateChanged(this, new ZoneStateChangedEventArgs(list));
				}
			});
		}

		private void RaiseShowStatusTextRequested(string text, bool throwIfNoHandler = true)
		{
			BeginInvokeOnUIDispatcher(() =>
			{
				if (ShowStatusTextRequested == null)
				{
					if (throwIfNoHandler)
					{
						throw new InvalidOperationException("No ShowStatusTextRequested handler has been found.");
					}
					else
					{
						return;
					}
				}

				ShowStatusTextRequested(this, new StatusTextEventArgs(text));
			});
		}

		private void RaiseSynchronizeRequested(Action tick, bool throwIfNoHandler = true)
		{
			BeginInvokeOnUIDispatcher(() =>
			{
				if (SynchronizeRequested == null)
				{
					if (throwIfNoHandler)
					{
						throw new InvalidOperationException("No SynchronizeRequested handler has been found.");
					}
					else
					{
						return;
					}
				}

				SynchronizeRequested(this, new SynchronizeEventArgs(tick));
			});
		}

		#endregion

		#region GameState Checks

		/// <summary>
		/// Checks if this Engine's internal state is ready to provide access to its Lua resources.
		/// </summary>
		internal void CheckStateForLuaAccess()
		{			
			switch (GameState)
			{
				case EngineGameState.Uninitialized:
					throw new InvalidOperationException("Uninitialized engine. Call Engine.Init() first.");

				case EngineGameState.Initializing:
					throw new InvalidOperationException("The engine is initializing.");

				case EngineGameState.Disposed:
					throw new ObjectDisposedException("Engine instance", "The engine has been disposed.");

				default:
					return;
			}
		}

		/// <summary>
		/// Checks if this Engine's internal state is ready to perform a state-changing game operation
		/// such as Start, Stop or Resume.
		/// </summary>
		private void CheckStateForConcurrentGameOperation()
		{
			switch (GameState)
			{
				case EngineGameState.Starting:
					throw new InvalidOperationException("The engine is busy starting a new game.");

				case EngineGameState.Saving:
					throw new InvalidOperationException("The engine is busy saving the game.");

				case EngineGameState.Restoring:
					throw new InvalidOperationException("The engine is busy restoring a game.");

				case EngineGameState.Stopping:
					throw new InvalidOperationException("The engine is busy stopping a game.");

				default:
					return;
			}
		}

		/// <summary>
		/// Checks if this Engine's internal state is equal to a target.
		/// </summary>
		/// <param name="target">Target state to compare.</param>
		/// <param name="exMessage">Exception message in the case the two states are not equal.</param>
		/// <param name="exShowsDetails">True to show details about the internal state in the exception.</param>
		private void CheckStateIs(EngineGameState target, string exMessage, bool exShowsDetails = false)
		{
			EngineGameState gs = GameState;
			
			if (gs != target)
			{
				throw new InvalidOperationException(exMessage + (exShowsDetails ? String.Format(" Current {0} != Target {1}", gs, target) : ""));
			}
		}

		/// <summary>
		/// Checks if this Engine's internal state is not equal to a target.
		/// </summary>
		/// <param name="target">Target state to compare.</param>
		/// <param name="exMessage">Exception message in the case the two states are equal.</param>
		/// <param name="exShowsDetails">True to show details about the internal state in the exception.</param>
		private void CheckStateIsNot(EngineGameState target, string exMessage, bool exShowsDetails = false)
		{
			if (GameState == target)
			{
				throw new InvalidOperationException(exMessage + (exShowsDetails ? String.Format(" Current == {0}", target) : ""));
			}
		}

		#endregion
    }

	#region Classes and Enums

	/// <summary>
	/// A state of the game that the engine can be in.
	/// </summary>
	public enum EngineGameState
	{
		Uninitialized,
		Initializing,
		Initialized,
		Starting,
		Restoring,
		Saving,
		Playing,
		Stopping,
		Disposed
	}

	#endregion

}
