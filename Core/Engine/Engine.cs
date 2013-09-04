///
/// WF.Player.Core - A Wherigo Player Core for different platforms.
/// Copyright (C) 2012-2013  Dirk Weltz <web@weltz-online.de>
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
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NLua;
using System.ComponentModel;


namespace WF.Player.Core
{

	#region Enums

	public enum ScreenType {
		Main = 0,
		Locations,
		Items,
		Inventory,
		Tasks,
		Details
		//DialogScreen // TODO: Not part of the specification?
	}

	public enum LogLevel {
		LogDebug = 0,
		LogCartridge,
		LogInfo,
		LogWarning,
		LogError
	}

	#endregion

    /// <summary>
    /// Engine handling all things for the cartridge, including loading, saving and other things.
    /// </summary>
    #if MONOTOUCH
	    [MonoTouch.Foundation.Preserve(AllMembers=true)]
    #endif
    public class Engine : INotifyPropertyChanged
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
        private Lua luaState;
        private WIGInternalsImpl wherigo;
        private LuaTable player;
		private Dictionary<int, Timer> timers = new Dictionary<int,Timer> ();
		private Dictionary<int,UIObject> uiObjects = new Dictionary<int, UIObject> ();
		private object[] threadResult;

        #endregion

        #region Engine version

        public string CorePlatform = "WF.Player.Core";
        public string CoreVersion = "0.1.0";

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

        #region Constructor

        public Engine()
        {
            this.luaState = new Lua();

            // Create Wherigo environment
            wherigo = new WIGInternalsImpl(this, luaState);

            // Register events
            wherigo.OnTimerStarted += TimerStarted;
            wherigo.OnTimerStopped += TimerStopped;
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
			//luaState ["Wherigo.DIALOGSCREEN"] = (int)ScreenType.DialogScreen;

            // Set definitions from Wherigo for LogMessage
			luaState ["Wherigo.LOGDEBUG"] = (int)LogLevel.LogDebug;
			luaState ["Wherigo.LOGCARTRIDGE"] = (int)LogLevel.LogCartridge;
			luaState ["Wherigo.LOGINFO"] = (int)LogLevel.LogInfo;
			luaState ["Wherigo.LOGWARNING"] = (int)LogLevel.LogWarning;
			luaState ["Wherigo.LOGERROR"] = (int)LogLevel.LogError;

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
        }

        #endregion

        #region Property

        public double Altitude { get { return alt; } }

        public double Accuracy { get { return accuracy; } }

        public Cartridge Cartridge { get { return cartridge; } }

		public string Device {
			get { 
				return device; 
			} 
			set { 
				if (device != value) {
					device = value;
					luaState.GetTable ("Env") ["Device"] = device;
				}
			}
		}

		public string DeviceId {
			get { 
				return deviceId; 
			} 
			set { 
				if (deviceId != value) {
					deviceId = value;
					luaState.GetTable ("Env") ["DeviceId"] = deviceId;
				}
			}
		}

		public string UIVersion {
			get { 
				return uiVersion; 
			} 
			set { 
				if (uiVersion != value) {
					uiVersion = value;
					luaState.GetTable ("Env") ["Version"] = uiVersion + " (" + CorePlatform + " " + CoreVersion + ")";
				}
			}
		}

        public double Heading { get { return heading; } }

        public double Latitude { get { return lat; } }
		
		public double Longitude { get { return lon; } }

        public Character Player { get { return player == null ? null : (Character) GetTable(player); } }

		#endregion

        #region Start/Stop/Load/Save

        /// <summary>
        /// Start engine.
        /// </summary>
        public void Start()
        {
			Call (cartridge.WIGTable,"Start",new object[] { cartridge.WIGTable });
        }

        /// <summary>
        /// Stop engine.
        /// </summary>
        public void Stop()
        {
			foreach(Timer t in timers.Values)
				t.Dispose ();

			HandleNotifyOS ("StopSound");

			Call (cartridge.WIGTable,"Stop",new object[] { cartridge.WIGTable });
		}

        /// <summary>
        /// Start engine and restore a saved cartridge.
        /// </summary>
        /// <param name="stream">Stream, where the cartridge load from.</param>
        public void Restore(Stream stream)
        {
			LoadGWS(stream);

			Call (cartridge.WIGTable,"Restore",new object[] { cartridge.WIGTable });
        }

        /// <summary>
        /// Load and init all data belonging to the selected cartridge.
        /// </summary>
        /// <param name="input">Stream to load cartridge load from.</param>
        /// <param name="cartridge">Cartridge object to load and init.</param>
        public void Init(Stream input, Cartridge cartridge)
        {
            this.cartridge = cartridge;
			cartridge.Engine = this;

            luaState["Env.CartFilename"] = cartridge.Filename;

			FileFormats.Load(input, cartridge);

            try
            {
                // Now start Lua binary chunk
                byte[] luaBytes = cartridge.Resources[0].Data;

//				string s = (string)luaState.DoString ("s = \"\\66\\106\\195\\182\\114\\110\"; print(s); return s")[0];

				cartridge.WIGTable = (LuaTable)luaState.DoString(luaBytes,cartridge.Filename)[0];

                // Set player relevant data
                player = (LuaTable)luaState["Player"];
                player["Cartridge"] = cartridge.WIGTable;
                player["CompletionCode"] = cartridge.CompletionCode;
                player["Name"] = cartridge.Player;

                LuaTable startLocation = (LuaTable)cartridge.WIGTable["StartingLocation"];

                // Check starting location
                if ((double)startLocation["latitude"] == 360.0 && (double)startLocation["longitude"] == 360.0)
                {
                    // TODO: Wait, until we have a signal or the accuracy has the right level

                    // This is a play anywhere cartridge, so set CartridgeStartingLocation to gps position
                    LuaTable playerPosition = (LuaTable)player["ObjectLocation"];

                    startLocation["latitude"] = playerPosition["latitude"];
                    startLocation["longitude"] = playerPosition["longitude"];
                    startLocation["altitude"] = playerPosition["altitude"];
                }
            }
            catch (Exception e)
            {
                // TODO
                // Rethrow exception
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Save the cartridge.
        /// </summary>
        /// <param name="stream">Stream, where the cartridge is saved.</param>
        public void Save(Stream stream)
        {
			Call (cartridge.WIGTable,"OnSync",new object[] { cartridge.WIGTable });

            // Serialize all objects
			SaveGWS(stream);
        }

		/// <summary>
		/// Sync the cartridge.
		/// </summary>
		/// <param name="stream">Stream, where the cartridge is synced.</param>
		public void Sync(Stream stream)
		{
			Call (cartridge.WIGTable, "Sync", new object[] { cartridge.WIGTable });

			// Serialize all objects
			SaveGWS(stream);
		}

		#endregion

        #region Refresh values

        /// <summary>
        /// Refresh location, altitude and accuracy with new values.
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <param name="alt">Altitude</param>
        /// <param name="accuracy">Accuracy</param>
        public void RefreshLocation(double lat, double lon, double alt, double accuracy)
        {
			this.lat = lat;
			this.lon = lon;
			this.alt = alt;
			this.accuracy = accuracy;

            // TODO: Remove
			Console.WriteLine (String.Format ("{0}, {1}, {2}, {3}",lat,lon,alt,accuracy));

			Call (player,"ProcessLocation",new object[] { player, lat, lon, alt, accuracy });
        }

        /// <summary>
        /// Refresh compass heading of device.
        /// </summary>
        /// <param name="heading">New heading in degrees.</param>
		public void RefreshHeading(double heading)
		{
			this.heading = heading;
		}

        #endregion

		#region Global functions for all players

		public string CreateLogMessage(string message)
		{
			return String.Format ("{0:yyyymmddhhmmss}|{1:+0.00000}|{2:+0.00000}|{3:+0.00000}|{4:+0.00000}|{5}", DateTime.Now.ToLocalTime (), lat, lon, alt, accuracy, message);
		}

		#endregion

		#region Wherigo Events

		/// <summary>
        /// Event, which is called, if the attribute of an object has changed.
        /// </summary>
        /// <param name="t">LuaTable for object, which attribute has changed.</param>
        /// <param name="attribute">String with the name of the attribute that has changed.</param>
		internal void HandleAttributeChanged(LuaTable t, string attribute)
		{
			Table obj = GetTable(t);
			string classname = (string)t["ClassName"];

			if (obj != null) {
				
				// Raises the NotifyPropertyChanged event if this is a UIObject.
				if (IsUIObject (obj))
					((UIObject)obj).NotifyPropertyChanged (attribute);

				// TODO: Why is this doing that?
				if (cartridge.WIGTable != null && ("Zone".Equals(classname) && "Active".Equals(attribute)))
					RefreshLocation(lat, lon, alt, accuracy);

				// Checks if an engine property has changed.
				bool isAttributeVisibleOrActive = "Active".Equals(attribute) || "Visible".Equals(attribute);
				if (isAttributeVisibleOrActive && "ZTask".Equals(classname))
				{
					RaisePropertyChanged("ActiveVisibleTasks");
				}
				else if (isAttributeVisibleOrActive && "Zone".Equals(classname))
				{
					RaisePropertyChanged("ActiveVisibleTasks");
				}

				// Raises the AttributeChanged event.
				RaiseAttributeChanged(obj, attribute);

			} else {

				// Raises the NotifyPropertyChanged event if this is a Cartridge.
				if ("ZCartridge".Equals(classname)) {
					cartridge.NotifyPropertyChanged (attribute);
				}

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
				((UIObject)c.Owner).NotifyPropertyChanged ("Commands");

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
				((UIObject)obj).NotifyPropertyChanged ("Container");
			if (from != null)
				((UIObject)from).NotifyPropertyChanged ("Inventory");
			if (to != null)
				((UIObject)obj).NotifyPropertyChanged ("Inventory");

			// Check for player inventory changes.
			if (player.Equals(ltTo) || player.Equals(ltFrom))
			{
				// The player visible inventory has changed.
				RaisePropertyChanged("VisibleInventory");
			}

			// Check for visible objects changes.
			if (IsZone(from) || IsZone(ltTo))
			{
				// Make a guess.
				RaisePropertyChanged("VisibleObjects");
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
			List<Zone> list = new List<Zone> ();

			// Generates the list of zones.
			var z = zones.GetEnumerator ();
			while(z.MoveNext())
			{
				// Gets a zone from the table.
				Zone zone = (Zone)GetTable ((LuaTable)z.Value);

				// Notifies that its state has changed.
				if (zone != null)
					((UIObject)zone).NotifyPropertyChanged ("State");

				// Adds the zone to the list.
				list.Add ((Zone)GetTable((LuaTable)z.Value));
			}

			// The list of zones and objects has changed.
			RaisePropertyChanged("ActiveVisibleZones");
			RaisePropertyChanged("VisibleObjects");

			// Raise the event.
			RaiseZoneStateChanged(list);
		}


        #endregion

        #region Timer

        /// <summary>
        /// Start timer.
        /// </summary>
        /// <param name="t">Timer to start.</param>
        internal void TimerStarted(LuaTable t)
        {
            int objIndex = Convert.ToInt32 ((double)t["ObjIndex"]);

            Timer timer = new Timer(TimerTickSync, objIndex, Convert.ToInt32((double)t["Duration"])*1000, Timeout.Infinite);

			if (!timers.ContainsKey(objIndex))
            	timers.Add(objIndex, timer);
        }

        /// <summary>
        /// Stop timer.
        /// </summary>
        /// <param name="t">Timer to stop.</param>
        internal void TimerStopped(LuaTable t)
        {
			int objIndex = Convert.ToInt32 ((double)t["ObjIndex"]);
            Timer timer = timers[objIndex];

            timer.Dispose();
            timers.Remove(objIndex);
        }

        /// <summary>
        /// Function, which calls the syncronize function of the ui.
        /// </summary>
        /// <param name="source">ObjIndex of the timer that released the tick.</param>
        private void TimerTickSync(object source)
        {
			int objIndex = (int)source;

			// Call only, if timer still exists.
			// It could be, that function is called from thread, even if the timer didn't exists anymore.
			if (timers.ContainsKey(objIndex))
            	// Call Tick syncronized with the GUI (for not thread save interfaces)
				if (SynchronizeRequested != null)
					SynchronizeRequested(this, new SynchronizeEventArgs(new Action(() => timerTick(source))));
        }

        /// <summary>
        /// Function for tick of a timer in source.
        /// </summary>
        /// <param name="source">ObjIndex of the timer that released the tick.</param>
        private void timerTick(object source)
        {
            int objIndex = (int)source;
            Timer timer = timers[objIndex];

			timer.Dispose();
			timers.Remove(objIndex);

			LuaTable t = (LuaTable)((LuaTable)cartridge.WIGTable["AllZObjects"])[objIndex];

			Call (t,"Tick",new object[] { t });
		}

        #endregion

		#region Properties

		/// <summary>
		/// Gets the active visible tasks.
		/// </summary>
		/// <value>The active visible tasks.</value>
		public List<Task> ActiveVisibleTasks {
			get {
				List<Task> result = new List<Task> ();

				if (player == null)
					return result;

				var t = ((LuaTable)((LuaTable)Call(player,"GetActiveVisibleTasks",new object[] { player })[0])).GetEnumerator();
				while (t.MoveNext())
					result.Add ((Task)GetTable((LuaTable)t.Value));

				return result;
			}
		}

		/// <summary>
		/// Gets the active visible zones.
		/// </summary>
		/// <value>The active visible zones.</value>
		public List<Zone> ActiveVisibleZones {
			get {
				List<Zone> result = new List<Zone> ();

				if (player == null)
					return result;

				var z = ((LuaTable)((LuaTable)Call (player,"GetActiveVisibleZones",new object[] { player })[0])).GetEnumerator();
				while (z.MoveNext())
					result.Add ((Zone)GetTable((LuaTable)z.Value));

				return result;
			}
		}

		/// <summary>
		/// Gets the visible inventory.
		/// </summary>
		/// <value>The visible objects.</value>
		public List<Thing> VisibleInventory {
			get {
				List<Thing> result = new List<Thing> ();

				if (player == null)
					return result;

				var t = ((LuaTable)((LuaTable)Call (player,"GetVisibleInventory",new object[] { player })[0])).GetEnumerator();
				while (t.MoveNext())
					result.Add ((Thing)GetTable((LuaTable)t.Value));

				return result;
			}
		}

		/// <summary>
		/// Gets the visible objects.
		/// </summary>
		/// <value>The visible objects.</value>
		public List<Thing> VisibleObjects {
			get {
				List<Thing> result = new List<Thing> ();

				if (player == null)
					return result;

				var t = ((LuaTable)((LuaTable)Call (player,"GetVisibleObjects",new object[] { player })[0])).GetEnumerator();
				while(t.MoveNext())
					result.Add ((Thing)GetTable((LuaTable)t.Value));

				return result;
			}
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
			return idx == -1 ? null : GetTable ((LuaTable)((LuaTable)cartridge.WIGTable["AllZObjects"])[idx]);
		}

        /// <summary>
        /// Get bearing to object in meters.
        /// </summary>
        /// <param name="obj">Object for bearing calculation.</param>
        /// <returns>Bearing in degrees.</returns>
		public double GetBearingOf(Thing obj)
		{
			return (double)Call (obj.WIGTable,"GetCurrentBearing",new object[] { obj.WIGTable, 0 })[0];
		}
		
        /// <summary>
        /// Get bearing to object as text.
        /// </summary>
        /// <param name="obj">Object for bearing calculation.</param>
        /// <returns>Text representing the bearing.</returns>
		public string GetBearingTextOf(Thing obj)
		{
			return String.Format ("{0}°",GetBearingOf(obj));
		}

        /// <summary>
        /// Get distance to object in meters.
        /// </summary>
        /// <param name="obj">Object for distance calculation.</param>
        /// <returns>a distance object.</returns>
		public Distance GetDistanceOf(Thing obj)
		{
			LuaTable dist = (LuaTable)Call (obj.WIGTable,"GetCurrentDistance",new object[] { obj.WIGTable, 0 })[0];
			return (Distance) GetTable(dist);
		}
		
        /// <summary>
        /// Get distance to object in meters as text.
        /// </summary>
        /// <param name="obj">Object for distance calculation.</param>
        /// <returns>Text representing the distance.</returns>
		public string GetDistanceTextOf(Thing obj)
		{
			double dist = GetDistanceOf (obj).Value;
			if (dist >= 1000.0)
				return String.Format ("{0:0.0} km",dist/1000.0);
			else if (dist >= 100)
				return String.Format ("{0:0} m",dist);
			else
				return String.Format ("{0:0} m",dist);
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
			string cn = lt != null ? lt["ClassName"] as string : null;

			foreach (string classname in classnames)
				if (String.Equals(cn, classname))
					return true;

			return false;
		}
		#endregion

        #region Helpers

        /// <summary>
        /// Call a Lua function in a new thread.
        /// </summary>
        /// <param name="obj">Object, to which the Lua function belongs.</param>
        /// <param name="func">Name of the function to call.</param>
        /// <param name="parameter">Parameters for the function call.</param>
        /// <returns></returns>
        internal object[] Call(LuaTable obj, string func, object[] parameter)
        {
            if (obj[func] is LuaFunction)
            {
                // Start function in a new thread
                ThreadParams param = new ThreadParams(obj, func, parameter);
                threadResult = null;
                callFunc(param);
                return threadResult;
            }
            else
            {
                return new object[] { };
            }
        }

        /// <summary>
        /// Function, which calls the Lua function in the new thread.
        /// </summary>
        /// <param name="arg">ThreadParams object for the function call.</param>
        private void callFunc(object arg)
        {
            ThreadParams param = (ThreadParams)arg;
            param.Running = true;
            threadResult = ((LuaFunction)param.Obj[param.Func]).Call(param.Parameter);
            param.Running = false;
        }

		/// <summary>
		/// Create an empty table.
		/// </summary>
		/// <returns>New table.</returns>
		internal LuaTable EmptyTable()
		{
			return (LuaTable)luaState.DoString("return {}")[0];
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

			string className = (string)t ["ClassName"];

			// Check if object is a AllZObject
			if (t["ObjIndex"] != null) 
			{
				int objIndex = Convert.ToInt32 ((double)t["ObjIndex"]);
				if (uiObjects.ContainsKey (objIndex))
					return uiObjects[objIndex];
				else {
					Table tab = null;
					// Check for objects, that have a ObjIndex, but didn't derived from UIObject
					if (className.Equals ("ZInput"))
						return new Input (this, t);
					// Now check for UIObjects
					if (className.Equals ("ZCharacter"))
						tab = new Character (this, t);
					if (className.Equals ("ZItem"))
						tab = new Item (this, t);
					if (className.Equals ("ZTask"))
						tab = new Task (this, t);
					if (className.Equals ("Zone"))
						tab = new Zone (this, t);
					// Save UIObject for later use
					if (tab != null)
						uiObjects.Add (objIndex, (UIObject)tab);
					return tab;
				}
			}
			else {
				//TODO: Delete
				if (className.Equals ("ZonePoint"))
					return new ZonePoint (this, t);
				if (className.Equals ("ZCommand")) {
					return new Command (this, t); }
				if (className.Equals ("ZReciprocalCommand"))
					return new Command (this, t);
				if (className.Equals("Distance"))
					return new Distance (this, t);
				return null;
			}
		}
		
        #endregion

        #region Serialization

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
                    Call(obj, "deserialize", new object[] { obj });
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
						tab = EmptyTable();
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
			LuaTable data = (LuaTable)Call(obj, "serialize", new object[] { obj })[0];
            writeTable(output, data);

            for (int i = 0; i < numAllZObjects; i++)
            {
                className = Encoding.UTF8.GetBytes((string)((LuaTable)((LuaTable)cartridge.WIGTable["AllZObjects"])[i])["ClassName"]);
                output.Write(className.Length);
                output.Write(className);
                obj = (LuaTable)((LuaTable)cartridge.WIGTable["AllZObjects"])[i];
				data = (LuaTable)Call(obj,"serialize",new object[] { obj })[0];
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
						LuaTable data = (LuaTable)Call((LuaTable)entry.Value, "serialize", new object[] { (LuaTable)entry.Value })[0];
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
							data = (LuaTable)Call((LuaTable)entry.Value, "serialize", new object[] { (LuaTable)entry.Value })[0];
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

		private void RaisePropertyChanged(string propName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
			}
		}

		private void RaiseLogMessageRequested(LogLevel level, string message)
		{
			if (LogMessageRequested != null)
			{
				LogMessageRequested(this, new LogMessageEventArgs(level, message));
			}
		}

		private void RaiseInputRequested(Input input, bool throwIfNoHandler = true)
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
		}

		private void RaiseMessageBoxRequested(MessageBox mb, bool throwIfNoHandler = true)
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
		}

		private void RaisePlayMediaRequested(Media media, bool throwIfNoHandler = true)
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
		}

		private void RaiseScreenRequested(ScreenType kind, UIObject obj, bool throwIfNoHandler = true)
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
		}

		private void RaiseInventoryChanged(Thing obj, Thing fromContainer, Thing toContainer)
		{
			if (InventoryChanged != null)
			{
				InventoryChanged(this, new InventoryChangedEventArgs(obj, fromContainer, toContainer));
			}
		}

		private void RaiseAttributeChanged(Table obj, string propName)
		{
			if (AttributeChanged != null)
			{
				AttributeChanged(this, new AttributeChangedEventArgs(obj, propName));
			}
		}

		private void RaiseCommandChanged(Command command)
		{
			if (CommandChanged != null)
			{
				CommandChanged(this, new ObjectEventArgs<Command>(command));
			}
		}

		private void RaiseCartridgeCompleted(Cartridge cartridge)
		{
			if (CartridgeCompleted != null)
			{
				CartridgeCompleted(this, new CartridgeEventArgs(cartridge));
			}
		}

		private void RaiseSaveRequested(Cartridge cartridge, bool throwIfNoHandler = true)
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
		}

		private void RaiseZoneStateChanged(List<Zone> list)
		{
			if (ZoneStateChanged != null)
			{
				ZoneStateChanged(this, new ZoneStateChangedEventArgs(list));
			}
		}

		private void RaiseShowStatusTextRequested(string text, bool throwIfNoHandler = true)
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
		}

		#endregion
    }

	#region Helper classes

	/// <summary>
	/// Event arguments for a wherigo object of a certain type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ObjectEventArgs<T> : EventArgs where T : class
	{
		/// <summary>
		/// Gets the wherigo object that this event is associated to.
		/// </summary>
		public T Object { get; private set; }

		internal ObjectEventArgs(T obj)
		{
			Object = obj;
		}
	}

	/// <summary>
	/// Event arguments for a change in an attribute of a Wherigo object.
	/// </summary>
	public class AttributeChangedEventArgs : ObjectEventArgs<Table>
	{
		/// <summary>
		/// Gets the name of the attribute that changed.
		/// </summary>
		public string PropertyName { get; private set; }

		internal AttributeChangedEventArgs(Table obj, string prop)
			: base(obj)
		{
			PropertyName = prop;
		}
	}

	/// <summary>
	/// Event arguments for a change in the cartridge entity.
	/// </summary>
	public class CartridgeEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the cartridge entity that changed.
		/// </summary>
		public Cartridge Cartridge { get; private set; }

		internal CartridgeEventArgs(Cartridge cart)
		{
			Cartridge = cart;
		}
	}

	/// <summary>
	/// Event arguments for a change in the inventory of a container.
	/// </summary>
	public class InventoryChangedEventArgs : ObjectEventArgs<Thing>
	{
		/// <summary>
		/// Gets the old container for the object, or null if there are none.
		/// </summary>
		public Thing OldContainer { get; private set; }

		/// <summary>
		/// Gets the new container for the object, or null if there are none.
		/// </summary>
		public Thing NewContainer { get; private set; }

		internal InventoryChangedEventArgs(Thing obj, Thing fromContainer, Thing toContainer)
			: base(obj)
		{
			OldContainer = fromContainer;
			NewContainer = toContainer;
		}
	}

	/// <summary>
	/// Event arguments for a message from the Wherigo engine to be logged.
	/// </summary>
	public class LogMessageEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the level of logging of the message.
		/// </summary>
		public LogLevel Level { get; private set; }

		/// <summary>
		/// Gets the message.
		/// </summary>
		public string Message { get; private set; }

		internal LogMessageEventArgs(LogLevel level, string message)
		{
			Level = level;
			Message = message;
		}
	}

	/// <summary>
	/// Event arguments for a special command requested by the Wherigo engine.
	/// </summary>
	public class NotifyOSEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the command that is requested.
		/// </summary>
		public string Command { get; private set; }

		internal NotifyOSEventArgs(string c)
		{
			Command = c;
		}
	}

	/// <summary>
	/// Event arguments for a message box.
	/// </summary>
	public class MessageBoxEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the message box descriptor.
		/// </summary>
		public MessageBox Descriptor { get; private set; }

		internal MessageBoxEventArgs(MessageBox descriptor)
		{
			Descriptor = descriptor;
		}
	}

	/// <summary>
	/// Event arguments for a screen.
	/// </summary>
	public class ScreenEventArgs : ObjectEventArgs<UIObject>
	{
		/// <summary>
		/// Gets the kind of screen.
		/// </summary>
		public ScreenType Screen { get; private set; }

		internal ScreenEventArgs(ScreenType kind, UIObject obj)
			: base(obj)
		{
			Screen = kind;
		}
	}

	/// <summary>
	/// Class for event arguments of ShowStatusTextEvent.
	/// </summary>
	public class StatusTextEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the status text associated with this event.
		/// </summary>
		public string Text;

		internal StatusTextEventArgs(string text)
		{
			Text = text;
		}
	}

	/// <summary>
	/// Class for event arguments of SynchronizeEvent.
	/// </summary>
	public class SynchronizeEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the action to execute in the UI thread.
		/// </summary>
		public Action Tick { get; private set; }

		internal SynchronizeEventArgs(Action tick)
		{
			Tick = tick;
		}
	}

	/// <summary>
	/// Class for thread parameters.
	/// </summary>
	public class ThreadParams
	{
		public LuaTable Obj;
		public string Func;
		public object[] Parameter;
		public bool Running;

		internal ThreadParams(LuaTable obj,string func,object[] parameter)
		{
			Obj = obj;
			Func = func;
			Parameter = parameter;
		}

	}

	/// <summary>
	/// Class for event arguments of ZoneStateChangedEvent.
	/// </summary>
	public class ZoneStateChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the list of zones currently available.
		/// </summary>
		public List<Zone> Zones { get; private set; }

		internal ZoneStateChangedEventArgs(List<Zone> z)
		{
			Zones = z;
		}
	}

	#endregion

}
