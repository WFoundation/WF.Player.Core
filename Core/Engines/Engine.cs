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
using System.Windows;
using System.Collections;
using WF.Player.Core.Utils;
using WF.Player.Core.Utils.Threading;
using WF.Player.Core.Formats;
using WF.Player.Core.Lua;

namespace WF.Player.Core.Engines
{

    /// <summary>
    /// The core component of the Wherigo player, that is orchestrating the game engine and
	/// gives feedback to the client user interface.
    /// </summary>
    #if MONOTOUCH
	    [MonoTouch.Foundation.Preserve(AllMembers=true)]
    #endif
    public class Engine : IDisposable, INotifyPropertyChanged
    {

        #region Private variables

		private IPlatformHelper platformHelper;
		private Cartridge cartridge;
		private double lat = 0;
		private double lon = 0;
		private double alt = 0;
		private double accuracy = 0;
		private double heading = 0;
		private List<Thing> visibleInventory;
		private List<Thing> visibleObjects;
		private List<Zone> activeVisibleZones;
		private List<Task> activeVisibleTasks;
		private EngineGameState gameState;
		private bool isReady;
		private bool isBusy;
		private LuaRuntime luaState;
		private SafeLua safeLuaState;
		private LuaExecutionQueue luaExecQueue;
		private ActionPump uiDispatchPump;
        private WIGInternalImpl wherigo;
        private LuaTable player;
		private Dictionary<int, System.Threading.Timer> timers;
		private Dictionary<int,UIObject> uiObjects;
		private object syncRoot = new object();

        #endregion

		#region Constants

		private const int internalTimerDuration = 1000;

		#endregion

        #region Engine version

        public static readonly string CorePlatform = "WF.Player.Core";
        public static readonly string CoreVersion = "0.2.0";

        #endregion

		#region Events

		public event EventHandler<AttributeChangedEventArgs> AttributeChanged;
		public event EventHandler<WherigoEventArgs> CartridgeCompleted;
		public event EventHandler<ObjectEventArgs<Command>> CommandChanged;
		public event EventHandler<ObjectEventArgs<Input>> InputRequested;
		public event EventHandler<InventoryChangedEventArgs> InventoryChanged;
		public event EventHandler<LogMessageEventArgs> LogMessageRequested;
		public event EventHandler<ObjectEventArgs<Media>> PlayMediaRequested;
		public event EventHandler<WherigoEventArgs> PlayAlertRequested;
		public event EventHandler<SavingEventArgs> SaveRequested;
		public event EventHandler<MessageBoxEventArgs> ShowMessageBoxRequested;
		public event EventHandler<ScreenEventArgs> ShowScreenRequested;
		public event EventHandler<StatusTextEventArgs> ShowStatusTextRequested;
		public event EventHandler<WherigoEventArgs> StopSoundsRequested;
		public event EventHandler<ZoneStateChangedEventArgs> ZoneStateChanged;
		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

        #region Constructor and Destructors

		/// <summary>
		/// Creates an instance of Engine that is uninitialized and cannot give
		/// user interface feedback.
		/// </summary>
        public Engine()
        {
			InitInstance(new DefaultPlatformHelper());
		}

		/// <summary>
		/// Creates an instance of Engine that is uninitialized, using a helper 
		/// that implements platform-specific operations.
		/// </summary>
		/// <param name="platform">The platform-specific helper.</param>
		public Engine(IPlatformHelper platform)
		{
			InitInstance(platform);
		}

		private void InitInstance(IPlatformHelper platform)
		{
			if (platform == null)
				throw new ArgumentNullException("platform");
			
			// Base objects.
			platformHelper = platform;

			luaState = new LuaRuntime();

			safeLuaState = new Utils.SafeLua(luaState)
			{
				RethrowsExceptions = true,
				RethrowsDisposedLuaExceptions = false
			};

			timers = new Dictionary<int, System.Threading.Timer>();
			uiObjects = new Dictionary<int, UIObject>();

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
			LuaTable wherigoTable = (LuaTable)luaState.Globals["Wherigo"];
			wherigoTable["MAINSCREEN"] = (int)ScreenType.Main;
			wherigoTable["LOCATIONSCREEN"] = (int)ScreenType.Locations;
			wherigoTable["ITEMSCREEN"] = (int)ScreenType.Items;
			wherigoTable["INVENTORYSCREEN"] = (int)ScreenType.Inventory;
			wherigoTable["TASKSCREEN"] = (int)ScreenType.Tasks;
			wherigoTable["DETAILSCREEN"] = (int)ScreenType.Details;

			// Set definitions from Wherigo for LogMessage
			wherigoTable["LOGDEBUG"] = (int)LogLevel.Debug;
			wherigoTable["LOGCARTRIDGE"] = (int)LogLevel.Cartridge;
			wherigoTable["LOGINFO"] = (int)LogLevel.Info;
			wherigoTable["LOGWARNING"] = (int)LogLevel.Warning;
			wherigoTable["LOGERROR"] = (int)LogLevel.Error;

			// Get information about the player
			// Create table for Env, ...
			luaState.Globals["Env"] = luaState.CreateTable();
			LuaTable env = (LuaTable)luaState.Globals["Env"];

			// Set defaults
			env["CartFolder"] = platformHelper.CartridgeFolder;
			env["SyncFolder"] = platformHelper.SavegameFolder;
			env["LogFolder"] = platformHelper.LogFolder;
			env["PathSep"] = platformHelper.PathSeparator;
			env["Downloaded"] = 0.0;
			env["Platform"] = String.Format("{0} ({1})", CorePlatform, platformHelper.Platform);
			env["Device"] = platformHelper.Device;
			env["DeviceID"] = platformHelper.DeviceId;
			//env["Version"] = uiVersion + " (" + CorePlatform + " " + CoreVersion + ")";
			env["Version"] = String.Format("{0} ({1} {2})", platformHelper.ClientVersion, CorePlatform, CoreVersion);

			// Creates job queues that runs in another thread.
			luaExecQueue = new LuaExecutionQueue(safeLuaState);
			uiDispatchPump = new ActionPump();

			// Sets some event handlers for the job queues.
			luaExecQueue.IsBusyChanged += new EventHandler(HandleLuaExecQueueIsBusyChanged);
			uiDispatchPump.IsBusyChanged += new EventHandler(HandleUIDispatchPumpIsBusyChanged);

			// Sets the game state.
			GameState = EngineGameState.Uninitialized;
		}

		~Engine()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);

			// Requests the GC to not finalize this object (best practice).
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposeManagedResources, bool noStateChange = false)
		{			
			// Sets the state as disposed. Returns if it is already.
			lock (syncRoot)
			{
				if (gameState == EngineGameState.Disposed)
				{
					return;
				}

				// Safe lua goes into disposal mode.
				safeLuaState.RethrowsExceptions = false;
			 
				// Clears some members set by Init().
				if (cartridge != null)
				{
					cartridge.Engine = null;
					cartridge = null;
				}
				player = null;

				// Unhooks WIGInternal.
				if (wherigo != null)
				{
					wherigo.OnTimerStarted -= HandleTimerStarted;
					wherigo.OnTimerStopped -= HandleTimerStopped;
					wherigo.OnCartridgeChanged -= HandleCartridgeChanged;
					wherigo.OnZoneStateChanged -= HandleZoneStateChanged;
					wherigo.OnInventoryChanged -= HandleInventoryChanged;
					wherigo.OnAttributeChanged -= HandleAttributeChanged;
					wherigo.OnCommandChanged -= HandleCommandChanged;
					wherigo = null;
				}
			}

			// Cleans managed resources in here.
			if (disposeManagedResources)
			{
				// Bye bye timers.
				DisposeTimers();

				// Bye bye UI objects.
				foreach (UIObject uiObject in uiObjects.Values)
				{
					// TODO: Check, if this is correct
					//					uiObject.WIGTable.Dispose(disposeManagedResources);
					uiObject.WIGTable.Dispose();
				}
				uiObjects.Clear();
				
				// Bye bye threads.
				if (luaExecQueue != null)
				{
					luaExecQueue.IsBusyChanged -= new EventHandler(HandleLuaExecQueueIsBusyChanged);					
					luaExecQueue.Dispose();
					lock (syncRoot)
					{
						luaExecQueue = null; 
					}
				}

				if (uiDispatchPump != null)
				{
					uiDispatchPump.IsBusyChanged -= new EventHandler(HandleUIDispatchPumpIsBusyChanged);
					uiDispatchPump.Dispose();
					lock (syncRoot)
					{
						uiDispatchPump = null; 
					}
				}

				// Disposes the underlying objects.
				if (luaState != null)
				{
					lock (luaState)
					{
						luaState.Dispose();
					}
					luaState = null;
					safeLuaState = null;
				} 
			}

			if (!noStateChange)
			{
				gameState = EngineGameState.Disposed;
			}
		}

		private void DisposeTimers()
		{
			AutoResetEvent waitHandle = new AutoResetEvent(false);
			
			foreach (var timer in timers.Values.ToList())
			{
				timer.Dispose(waitHandle);
				waitHandle.WaitOne();
			}

			lock (syncRoot)
			{
				timers.Clear();
			}
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
						&& value != EngineGameState.Disposed
						&& value != EngineGameState.Uninitializing;
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

		public bool IsBusy
		{
			get
			{
				lock (syncRoot)
				{
					return isBusy;
				}
			}

			private set
			{
				bool ib;
				lock (syncRoot)
				{
					ib = isBusy;
				}

				if (ib != value)
				{
					lock (syncRoot)
					{
						isBusy = value;
					}

					// This event is raised bypassing the ui dispatch pump because
					// it carries information that needs immediate processing by the UI,
					// even if lua processing has already started.
					RaisePropertyChanged("IsBusy", false);
				}
			}
		}

		#endregion

		#region Internal

		internal LuaExecutionQueue LuaExecQueue { get { return luaExecQueue; } }

		internal SafeLua SafeLuaState { get { return safeLuaState; } }

		#endregion

		#endregion

        #region Game Operations (Init, Start...)

		/// <summary>
		/// Initializes this Engine with the data of a Cartridge, loaded from a stream.
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

			((LuaTable)luaState.Globals["Env"])["CartFilename"] = cartridge.Filename;

			FileFormats.Load(input, cartridge);

			// Set player relevant data
			player = (LuaTable)((LuaTable)luaState.Globals["Wherigo"])["Player"];
			var temp = cartridge.Player;
			player["CompletionCode"] = cartridge.CompletionCode;
			player["Name"] = cartridge.Player;
			LuaTable objLoc = (LuaTable)player["ObjectLocation"];
			objLoc["latitude"] = lat;
			objLoc["longitude"] = lon;
			objLoc["altitude"] = alt;

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

		public void Reset()
		{
			// Sanity checks.
			CheckStateIs(EngineGameState.Initialized, "The engine is not in state Initialized.", true);

			// State change.
			GameState = EngineGameState.Uninitializing;

			// Silent dispose.
			Dispose(true, true);

			// Reinit instance.
			InitInstance(this.platformHelper);

			// State change.
			GameState = EngineGameState.Uninitialized;
		}

        /// <summary>
        /// Starts the engine, playing a new game.
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
        /// Stops the engine, terminating the game session.
        /// </summary>
        public void Stop()
        {
			// Sanity checks.
			CheckStateForLuaAccess();
			CheckStateForConcurrentGameOperation();
			CheckStateIsNot(EngineGameState.Initialized, "The engine is aldreay stopped.");
			
			GameState = EngineGameState.Stopping;

			DisposeTimers();

			HandleNotifyOS ("StopSound");

			LuaExecQueue.BeginCallSelf(cartridge.WIGTable, "Stop");
			LuaExecQueue.WaitEmpty();

			GameState = EngineGameState.Initialized;
		}

        /// <summary>
        /// Starts the engine, restoring a saved game for the current cartridge.
        /// </summary>
        /// <param name="stream">Stream, where the save game load from.</param>
        public void Restore(Stream stream)
        {
			// Sanity checks.
			CheckStateForLuaAccess();
			CheckStateForConcurrentGameOperation();
			CheckStateIsNot(EngineGameState.Playing, "The engine is aldreay playing.");
			
			GameState = EngineGameState.Restoring;

			new FileGWS(this).Load(stream);

			LuaExecQueue.BeginCallSelf(cartridge.WIGTable, "OnRestore");
			LuaExecQueue.WaitEmpty();

			GameState = EngineGameState.Playing;
        }

        /// <summary>
        /// Saves the game for the current cartridge.
        /// </summary>
        /// <param name="stream">Stream, where the cartridge is saved.</param>
        public void Save(Stream stream)
        {
			// Sanity checks.
			CheckStateIs(EngineGameState.Playing, "The engine is not playing.", true);

			// State change.
			GameState = EngineGameState.Saving;

			// Informs the cartridge that saving starts.
			LuaExecQueue.BeginCallSelf(cartridge.WIGTable, "OnSync");
			LuaExecQueue.WaitEmpty();

            // Serialize all objects
			new FileGWS(this).Save(stream);

			// State change.
			GameState = EngineGameState.Playing;
        }

		/// <summary>
		/// Pauses the engine, suspending its ongoing actions.
		/// </summary>
		public void Pause()
		{
			// Sanity checks.
			CheckStateIs(EngineGameState.Playing, "The engine is not playing.", true);

			// State change.
			GameState = EngineGameState.Pausing;

			// Stops and disposes the timers.
			DisposeTimers();

			// Pauses the action queues.
			uiDispatchPump.IsPumping = false;
			luaExecQueue.IsRunning = false;

			// State change.
			GameState = EngineGameState.Paused;
		}

		/// <summary>
		/// Resumes the engine after a pause, continuing its suspended actions.
		/// </summary>
		public void Resume()
		{
			// Sanity checks.
			CheckStateIs(EngineGameState.Paused, "The engine is not paused.", true);

			// State change.
			GameState = EngineGameState.Resuming;

			// Resumes the action queues.
			uiDispatchPump.IsPumping = true;
			luaExecQueue.IsRunning = true;

			// Restarts the timers.
			var e = safeLuaState.SafeGetEnumerator((LuaTable)safeLuaState.SafeCallSelf(player, "GetActiveTimers")[0]);
			while (e.MoveNext())
			{
				LuaTable obj = (LuaTable) e.Current.Value;

				// Should the timer be restarted?
				bool shouldRestart = safeLuaState.SafeGetField<bool>((LuaTable)safeLuaState.SafeCallSelf(obj, "Restart")[0], 0);
				if (!shouldRestart)
				{
					continue;
				}

				// Creates the internal timer.
				int objIndex = safeLuaState.SafeGetField<int>(obj, "ObjIndex");
				CreateAndStartInternalTimer(objIndex);
			}

			// State change.
			GameState = EngineGameState.Playing;
		}

		public void FreeMemory()
		{
			lock (luaState) {
				luaState.DoString ("collectgarbage(\"collect\")");
			}
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
				ActiveVisibleTasks = GetTableListFromLuaTable<Task>((LuaTable)player.CallSelf("GetActiveVisibleTasks")[0]); 
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
				ActiveVisibleZones = GetTableListFromLuaTable<Zone>((LuaTable)player.CallSelf("GetActiveVisibleZones")[0]); 
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
				VisibleInventory = GetTableListFromLuaTable<Thing>((LuaTable)player.CallSelf("GetVisibleInventory")[0]); 
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
				VisibleObjects = GetTableListFromLuaTable<Thing>((LuaTable)player.CallSelf("GetVisibleObjects")[0]); 
			}
		}

		private void RefreshThingVectorFromPlayerAsync(Thing t)
		{
			// Sanity checks.
			if (!IsReady)
			{
				return;
			}

			// If we're not in the lua exec thread, be in it!
			if (!LuaExecQueue.IsSameThread)
			{
				LuaExecQueue.BeginAction(() => RefreshThingVectorFromPlayerAsync(t));
				return;
			}

			/// This below executes in the lua exec thread, so it's fine to block.

			// Gets more info about the thing.
			LuaValue thingLoc;
			lock (luaState)
			{
				thingLoc = t.WIGTable["ObjectLocation"];
			}
			bool isZone = t is Zone;

			// If the Thing is not a zone and has no location, consider it is close to the player.
			if (!isZone && thingLoc == null)
			{
				LuaTable lt;
				lock (luaState)
				{
					lt = (LuaTable)luaState.DoString("return Wherigo.Distance(0)")[0];
				}
				t.VectorFromPlayer = new LocationVector((Distance)GetTable(lt), 0);
				RaisePropertyChangedInObject(t, "VectorFromPlayer");
				return;
			}

			LuaVararg ret;
			if (isZone)
			{
				lock (luaState)
				{
					ret = wherigo.VectorToZone(player["ObjectLocation"], t.WIGTable);
				}
			}
			else
			{
				lock (luaState)
				{
					ret = wherigo.VectorToPoint(player["ObjectLocation"], thingLoc);
				}
			}

			t.VectorFromPlayer = new LocationVector((Distance)GetTable((LuaTable)ret[0]), (double)ret[1].ToNumber());
			RaisePropertyChangedInObject(t, "VectorFromPlayer");
			return;
		}

		#endregion

		#region WIGInternal Event Handlers

		/// <summary>
        /// Event, which is called, if the attribute of an object has changed.
        /// </summary>
        /// <param name="t">LuaTable for object, which attribute has changed.</param>
        /// <param name="attribute">String with the name of the attribute that has changed.</param>
		internal void HandleAttributeChanged(LuaTable t, string attribute)
		{
			WherigoObject obj = GetTable(t);
			string classname;
			lock (luaState)
			{
				classname = (string)t["ClassName"].ToString(); 
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

			if ("complete".Equals(ls))
			{
				// Marks the cartridge as completed.
				cartridge.Complete = true;

				// Raises the event.
				RaiseCartridgeCompleted(cartridge);
			}
			else if ("sync".Equals(ls))
			{
				// Raises the event.
				RaiseSaveRequested(cartridge,false);
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
			if ("SaveClose".Equals(command))
			{
				RaiseSaveRequested(cartridge, true);
			}
			else if ("DriveTo".Equals(command))
			{
				// TODO: Make sure that this is unused. If so, remove it.
				throw new NotImplementedException("The DriveTo command is not implemented.");
			}
			else if ("StopSound".Equals(command))
			{
				RaiseStopSoundsRequested();
			}
			else if ("Alert".Equals(command))
			{
				RaisePlayAlertRequested();
			}
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
			IEnumerator<KeyValuePair<LuaValue,LuaValue>> z;
			bool run = true;
			lock (luaState)
			{
				z = zones.GetEnumerator();
				run = z.MoveNext();
			}
			while (run)
			{
				// Gets a zone from the table.
				Zone zone = (Zone)GetTable((LuaTable)z.Current.Value);

				// Performs notifications.
				if (zone != null)
				{
					RaisePropertyChangedInObject((UIObject)zone, "State");
					RefreshThingVectorFromPlayerAsync(zone);
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
			VisibleObjects.ForEach(t => RefreshThingVectorFromPlayerAsync(t));

			// Raise the event.
			RaiseZoneStateChanged(list);
		}

        #endregion

		#region Internal Event Handlers

		private void HandleLuaExecQueueIsBusyChanged(object sender, EventArgs e)
		{
			bool leqIsBusy = luaExecQueue.IsBusy;

			// Sets the UI dispatching action pump to be pumping when the lua exec queue
			// is not busy.
			uiDispatchPump.IsPumping = !leqIsBusy;

			// The engine is busy if the lua execution queue or the ui dispatch pump are busy.
			IsBusy = leqIsBusy || uiDispatchPump.IsBusy;
		}

		private void HandleUIDispatchPumpIsBusyChanged(object sender, EventArgs e)
		{
			// The engine is busy if the lua execution queue or the ui dispatch pump are busy.
			IsBusy = luaExecQueue.IsBusy || uiDispatchPump.IsBusy;
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
				objIndex = Convert.ToInt32((double)t["ObjIndex"].ToNumber()); 
			}

			// Starts a timer.
			CreateAndStartInternalTimer(objIndex);

			// Call OnStart of this timer
			lock (luaState)
			{
				t.CallSelf("Start"); 
			}
        }

		/// <summary>
		/// Creates, registers and starts an internal timer for an object index.
		/// </summary>
		/// <param name="objIndex">Key of the newly created timer.</param>
		/// <returns></returns>
		private System.Threading.Timer CreateAndStartInternalTimer(int objIndex)
		{
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

			return timer;
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
				objIndex = Convert.ToInt32((double)t["ObjIndex"].ToNumber()); 
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
			lock (luaState)
			{
				t.CallSelf("Stop");
			}
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
			LuaValue elapsedRaw;
			LuaValue remainingRaw; 
			lock (luaState)
			{
				elapsedRaw = t["Elapsed"];
				remainingRaw = t["Remaining"]; 
			}
			if (elapsedRaw == null || elapsedRaw is LuaNil)
				elapsedRaw = 0.0d;
			if (remainingRaw == null || remainingRaw is LuaNil)
			{
				lock (luaState)
				{
					remainingRaw = t["Duration"];
				}
			}

			double elapsed = (double)(LuaNumber)elapsedRaw.ToNumber() * internalTimerDuration;
			double remaining = (double)(LuaNumber)remainingRaw.ToNumber() * internalTimerDuration;

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
			{
				// Disables and removes the current timer.
				System.Threading.Timer timer = timers[objIndex];
				timer.Dispose();
				lock (syncRoot)
				{
					timers.Remove(objIndex);
				}

				// Call OnTick of this timer
				LuaExecQueue.BeginCallSelf(GetObject(objIndex).WIGTable, "Tick");
			}
        }

        #endregion

        #region Retrive data from cartridge

        /// <summary>
        /// Get ZObject for given ObjIndex idx.
        /// </summary>
        /// <param name="idx">ObjIndex for ZObject.</param>
        /// <returns>LuaTable for ZObject.</returns>
		public WherigoObject GetObject(int idx)
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
				cn = lt != null ? lt["ClassName"].ToString() as string : null; 
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
		internal List<T> GetTableListFromLuaTable<T>(LuaTable table) where T : WherigoObject
		{
			if (table == null)
				return null;

			List<T> result = new List<T>();

			lock (luaState)
			{
				var t = table.GetEnumerator();

				while (t.MoveNext())
				{
					T val = GetTable((LuaTable)t.Current.Value) as T;
					if (val != null)
						result.Add(val);
				}
			}

			return result;
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
		internal WherigoObject GetTable(LuaTable t)
		{
			if (t == null)
				return null;

			string className;
			lock (luaState)
			{
				className = (string)t["ClassName"].ToString(); 
			}

			// Check if object is a AllZObject
			LuaValue oi;
			lock (luaState)
			{
				t.TryGetValue("ObjIndex",out oi);
			}
			if (oi != null && !(oi is LuaNil)) 
			{
				int objIndex = Convert.ToInt32 ((double)oi.ToNumber());

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
					WherigoObject tab = null;
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

		#region Event Raisers

		/// <summary>
		/// Raises this Engine's PropertyChanged event.
		/// </summary>
		/// <param name="propName">Name of the property to raise.</param>
		protected void RaisePropertyChanged(string propName)
		{
			RaisePropertyChanged(propName, true);
		}

		/// <summary>
		/// Asynchronously invokes an action to be run in the UI thread.
		/// </summary>
		/// <param name="action"></param>
		private void BeginInvokeInUIThread(Action action, bool usePump = false)
		{
			// Throws an exception if there is no handler for this.
			if (!platformHelper.CanDispatchOnUIThread)
				throw new InvalidOperationException("Unable to dispatch on UI Thread. Make sure to construct Engine with a IPlatformHelper that implements DispatchOnUIThread() and has CanDispatchOnUIThread return true.");

			if (usePump)
			{
				// Adds a sync request action to the action pump.
				uiDispatchPump.AcceptAction(new Action(() => platformHelper.BeginDispatchOnUIThread(action))); 
			}
			else
			{
				// Invokes the event right away.
				platformHelper.BeginDispatchOnUIThread(action);
			}
		}

		private void RaisePropertyChangedInObject(UIObject obj, string propName)
		{
			BeginInvokeInUIThread(() =>
			{
				obj.NotifyPropertyChanged(propName);
			}, true);
		}

		private void RaisePropertyChangedInObject(Cartridge obj, string propName)
		{
			BeginInvokeInUIThread(() =>
			{
				obj.NotifyPropertyChanged(propName);
			}, true);
		}

		private void RaisePropertyChanged(string propName, bool usePump = true)
		{
			BeginInvokeInUIThread(() =>
			{
				if (PropertyChanged != null)
				{
					PropertyChanged(this, new PropertyChangedEventArgs(propName));
				}
			}, usePump);
		}

		private void RaiseLogMessageRequested(LogLevel level, string message)
		{
			BeginInvokeInUIThread(() =>
			{
				if (LogMessageRequested != null)
				{
						LogMessageRequested(this, new LogMessageEventArgs(cartridge, level, message));
				}
			});
		}

		private void RaiseInputRequested(Input input, bool throwIfNoHandler = true)
		{
			BeginInvokeInUIThread(() =>
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

					InputRequested(this, new ObjectEventArgs<Input>(cartridge, input));
			});
		}

		private void RaiseMessageBoxRequested(MessageBox mb, bool throwIfNoHandler = true)
		{
			BeginInvokeInUIThread(() =>
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

					ShowMessageBoxRequested(this, new MessageBoxEventArgs(cartridge, mb));
			});
		}

		private void RaisePlayMediaRequested(Media media, bool throwIfNoHandler = true)
		{
			BeginInvokeInUIThread(() =>
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

					PlayMediaRequested(this, new ObjectEventArgs<Media>(cartridge, media));
			});
		}

		private void RaiseScreenRequested(ScreenType kind, UIObject obj, bool throwIfNoHandler = true)
		{
			BeginInvokeInUIThread(() =>
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

					ShowScreenRequested(this, new ScreenEventArgs(cartridge, kind, obj));
			});
		}

		private void RaiseInventoryChanged(Thing obj, Thing fromContainer, Thing toContainer)
		{
			BeginInvokeInUIThread(() =>
			{
				if (InventoryChanged != null)
				{
						InventoryChanged(this, new InventoryChangedEventArgs(cartridge, obj, fromContainer, toContainer));
				}
			});
		}

		private void RaiseAttributeChanged(WherigoObject obj, string propName)
		{
			BeginInvokeInUIThread(() =>
			{
				if (AttributeChanged != null)
				{
						AttributeChanged(this, new AttributeChangedEventArgs(cartridge, obj, propName));
				}
			}, true);
		}

		private void RaiseCommandChanged(Command command)
		{
			BeginInvokeInUIThread(() =>
			{
				if (CommandChanged != null)
				{
						CommandChanged(this, new ObjectEventArgs<Command>(cartridge, command));
				}
			});
		}

		private void RaiseCartridgeCompleted(Cartridge cartridge)
		{
			BeginInvokeInUIThread(() =>
			{
				if (CartridgeCompleted != null)
				{
						CartridgeCompleted(this, new WherigoEventArgs(cartridge));
				}
			});
		}

		private void RaiseSaveRequested(Cartridge cartridge, bool closeAfterSave, bool throwIfNoHandler = true)
		{
			BeginInvokeInUIThread(() =>
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

					SaveRequested(this, new SavingEventArgs(cartridge, closeAfterSave));
			});
		}

		private void RaiseZoneStateChanged(List<Zone> list)
		{
			BeginInvokeInUIThread(() =>
			{
				if (ZoneStateChanged != null)
				{
						ZoneStateChanged(this, new ZoneStateChangedEventArgs(cartridge, list));
				}
			});
		}

		private void RaiseStopSoundsRequested(bool throwIfNoHandler = true)
		{
			BeginInvokeInUIThread(() =>
			{
				if (StopSoundsRequested == null)
				{
					if (throwIfNoHandler)
					{
						throw new InvalidOperationException("No StopSoundsRequested handler has been found.");
					}
					else
					{
						return;
					}
				}
		
				StopSoundsRequested(this, new WherigoEventArgs(cartridge));
			});
		}

		private void RaisePlayAlertRequested(bool throwIfNoHandler = true)
		{
			BeginInvokeInUIThread(() =>
			{
				if (PlayAlertRequested == null)
				{
					if (throwIfNoHandler)
					{
						throw new InvalidOperationException("No PlayAlertRequested handler has been found.");
					}
					else
					{
						return;
					}
				}
				
				PlayAlertRequested(this, new WherigoEventArgs(cartridge));
			});
		}

		private void RaiseShowStatusTextRequested(string text, bool throwIfNoHandler = true)
		{
			BeginInvokeInUIThread(() =>
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

					ShowStatusTextRequested(this, new StatusTextEventArgs(cartridge, text));
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

				case EngineGameState.Uninitializing:
					throw new InvalidOperationException("The engine is uninitializing.");

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

				case EngineGameState.Pausing:
					throw new InvalidOperationException("The engine is busy pausing the game.");

				case EngineGameState.Resuming:
					throw new InvalidOperationException("The engine is busy resuming the game.");

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
		Uninitializing,
		Uninitialized,
		Initializing,
		Initialized,
		Starting,
		Restoring,
		Saving,
		Playing,
		Pausing,
		Resuming,
		Stopping,
		Paused,
		Disposed
	}

	#endregion

}
