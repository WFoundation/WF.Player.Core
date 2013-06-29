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


namespace WF.Player.Core
{

	#region Delegate definitions

	// Delegate for callback functions
	public delegate void CallbackFunction(string retValue);

	// Delegate for timer events
	public delegate void SyncronizeTick(object source);

	#endregion

    /// <summary>
    /// Engine handling all things for the cartridge, including loading, saving and other things.
    /// </summary>
    #if MONOTOUCH
	    [MonoTouch.Foundation.Preserve(AllMembers=true)]
    #endif
    public class Engine
    {

        #region Private variables

		private Cartridge cartridge;
		private double lat = 0;
		private double lon = 0;
		private double alt = 0;
		private double accuracy = 0;
		private double heading = 0;
        private Lua luaState;
        private Wherigo wherigo;
        private LuaTable player;
        private IUserInterface ui;
		private Dictionary<int, Timer> timers = new Dictionary<int,Timer> ();
		private Dictionary<int,UIObject> uiObjects = new Dictionary<int, UIObject> ();
		private object[] threadResult;

        #endregion

        #region Engine version

        public string CorePlatform = "WF.Player.Core";
        public string CoreVersion = "0.1.0";

        #endregion

        #region Constants

        // Definitions for ShowScreen
		public int MAINSCREEN;
		public int LOCATIONSCREEN;
		public int ITEMSCREEN;
		public int INVENTORYSCREEN;
		public int TASKSCREEN;
		public int DETAILSCREEN;
		public int DIALOGSCREEN;

        // Definitions for LogMessage
		public int LOGDEBUG;
		public int LOGCARTRIDGE;
		public int LOGINFO;
		public int LOGWARNING;
		public int LOGERROR;

        #endregion

        #region Constructor

        public Engine(IUserInterface ui)
        {
            this.luaState = new Lua();
            this.ui = ui;

            // Create Wherigo environment
            wherigo = new Wherigo(this, luaState, ui);

            // Register events
            wherigo.OnTimerStarted += TimerStarted;
            wherigo.OnTimerStopped += TimerStopped;
			wherigo.OnCartridgeChanged += CartridgeChanged;
			wherigo.OnZoneStateChanged += ZoneStateChanged;
			wherigo.OnInventoryChanged += InventoryChanged;
			wherigo.OnAttributeChanged += AttributeChanged;
			wherigo.OnCommandChanged += CommandChanged;

			// Get definitions from Wherigo for ShowScreen
			MAINSCREEN = Convert.ToInt32 ((double)luaState["Wherigo.MAINSCREEN"]);
			LOCATIONSCREEN = Convert.ToInt32 ((double)luaState["Wherigo.LOCATIONSCREEN"]);
			ITEMSCREEN = Convert.ToInt32 ((double)luaState["Wherigo.ITEMSCREEN"]);
			INVENTORYSCREEN = Convert.ToInt32 ((double)luaState["Wherigo.INVENTORYSCREEN"]);
			TASKSCREEN = Convert.ToInt32 ((double)luaState["Wherigo.TASKSCREEN"]);
			DETAILSCREEN = Convert.ToInt32 ((double)luaState["Wherigo.DETAILSCREEN"]);
			DIALOGSCREEN = Convert.ToInt32 ((double)luaState["Wherigo.DIALOGSCREEN"]);

            // Get definitions from Wherigo for LogMessage
            LOGDEBUG = Convert.ToInt32((double)luaState["Wherigo.LOGDEBUG"]);
			LOGCARTRIDGE = Convert.ToInt32 ((double)luaState["Wherigo.LOGCARTRIDGE"]);
			LOGINFO = Convert.ToInt32 ((double)luaState["Wherigo.LOGINFO"]);
			LOGWARNING = Convert.ToInt32 ((double)luaState["Wherigo.LOGWARNING"]);
			LOGERROR = Convert.ToInt32 ((double)luaState["Wherigo.LOGERROR"]);

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
            env["Device"] = ui.GetDevice();
            env["DeviceID"] = ui.GetDeviceId();
            env["Version"] = ui.GetVersion() + " (" + CorePlatform + " " + CoreVersion + ")";
        }

        #endregion

        #region Property

        public double Altitude { get { return alt; } }

        public double Accuracy { get { return accuracy; } }

        public Cartridge Cartridge { get { return cartridge; } }

        public double Heading { get { return heading; } }

        public double Latitude { get { return lat; } }
		
		public double Longitude { get { return lon; } }

        public LuaTable Player { get { return player; } }

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

			ui.NotifyOS ("StopSound");

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

        #region Wherigo Events

        /// <summary>
        /// Event, which is called, if the attribute of an object has changed.
        /// </summary>
        /// <param name="t">LuaTable for object, which attribute has changed.</param>
        /// <param name="s">String with the name of the attribute that has changed.</param>
		internal void AttributeChanged(LuaTable t, string s)
		{
			Table obj = GetTable(t);

			if (obj != null) {
				if (IsUIObject (obj))
					((UIObject)obj).NotifyPropertyChanged (s);

				ui.AttributeChanged (GetTable(t), s);

				if (cartridge.WIGTable != null && ((string)t ["ClassName"]).Equals ("Zone") && s.Equals ("Active"))
					RefreshLocation (lat, lon, alt, accuracy);
			} else {
				if (((string)t["ClassName"]).Equals ("ZCartridge")) {
					cartridge.NotifyPropertyChanged (s);
				}
			}
		}
		
        /// <summary>
        /// Event, which is called, if a command has changed.
        /// </summary>
        /// <param name="c">LuaTable for command, that has changed.</param>
		internal void CommandChanged(LuaTable ltCommand)
		{
			Command c = (Command)GetTable (ltCommand);

			if (c.Owner != null && IsUIObject (c.Owner))
				((UIObject)c.Owner).NotifyPropertyChanged ("Commands");

			// TODO: Reciprocal commands should also inform the targets.

			ui.CommandChanged(c);
		}

        /// <summary>
        /// Event, which is called, if the cartridge has changed.
        /// </summary>
        /// <param name="s">String with the name of the cartridge attribute, that has changed.</param>
		internal void CartridgeChanged(string s)
		{
			if (s.ToLower().Equals("complete"))
				cartridge.Complete = true;

			cartridge.NotifyPropertyChanged (s);

			ui.CartridgeChanged(s);
		}

		/// <summary>
		/// Event, which is called, if the inventory has changed.
		/// </summary>
		/// <param name="t">LuaTable for item/character object.</param>
		/// <param name="from">LuaTable for container, there the object was.</param>
		/// <param name="to">LuaTable for container, to which the object goes.</param>
		internal void InventoryChanged(LuaTable ltThing, LuaTable ltFrom, LuaTable ltTo)
		{
			Thing obj = (Thing)GetTable (ltThing);
			Thing from = (Thing)GetTable (ltFrom);
			Thing to = (Thing)GetTable (ltTo);

			if (obj != null)
				((UIObject)obj).NotifyPropertyChanged ("Container");
			if (from != null)
				((UIObject)from).NotifyPropertyChanged ("Inventory");
			if (to != null)
				((UIObject)obj).NotifyPropertyChanged ("Inventory");

			ui.InventoryChanged(obj, from, to);
		}

		/// <summary>
		/// Event, which is called, if the state of a zone has changed.
		/// </summary>
		/// <param name="z">LuaTable for zone object.</param>
		internal void ZoneStateChanged(LuaTable zones)
		{
			List<Zone> list = new List<Zone> ();

			var z = zones.GetEnumerator ();
			while(z.MoveNext())
			{
				Zone zone = (Zone)GetTable ((LuaTable)z.Value);
				if (zone != null)
					((UIObject)zone).NotifyPropertyChanged ("State");
				list.Add ((Zone)GetTable((LuaTable)z.Value));
			}

			ui.ZoneStateChanged(list);
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
            	ui.Syncronize(timerTick, source);
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
        /// <returns>Distance in meters.</returns>
		public double GetDistanceOf(Thing obj)
		{
			LuaTable dist = (LuaTable)Call (obj.WIGTable,"GetCurrentDistance",new object[] { obj.WIGTable, 0 })[0];
			return (double)dist["value"];
		}
		
        /// <summary>
        /// Get distance to object in meters as text.
        /// </summary>
        /// <param name="obj">Object for distance calculation.</param>
        /// <returns>Text representing the distance.</returns>
		public string GetDistanceTextOf(Thing obj)
		{
			double dist = GetDistanceOf (obj);
			if (dist >= 1000.0)
				return String.Format ("{0:0.0} km",dist/1000.0);
			else if (dist >= 100)
				return String.Format ("{0:0} m",dist);
			else
				return String.Format ("{0:0} m",dist);
		}

        /// <summary>
		/// Check, if the given LuaTable obj is a ZCartridge object.
		/// </summary>
		/// <returns><c>true</c> if obj is a ZCartridge object; otherwise, <c>false</c>.</returns>
		/// <param name="obj">LuaTable with object to check.</param>
		public bool IsCartridge(object obj)
		{
			return obj is Cartridge;
		}
		
		/// <summary>
		/// Check, if the given LuaTable obj is a ZCharacter object.
		/// </summary>
		/// <returns><c>true</c> if obj is a ZCharacter object; otherwise, <c>false</c>.</returns>
		/// <param name="obj">LuaTable with object to check.</param>
		public bool IsCharacter(object obj)
		{
			return obj is Character;
		}
		
		/// <summary>
		/// Check, if the given LuaTable obj is a Distance object.
		/// </summary>
		/// <returns><c>true</c> if obj is a Distance object; otherwise, <c>false</c>.</returns>
		/// <param name="obj">LuaTable with object to check.</param>
		public bool IsDistance(object obj)
		{
			return obj is Distance;
		}

		/// <summary>
		/// Check, if the given LuaTable obj is a ZItem object.
		/// </summary>
		/// <returns><c>true</c> if obj is a ZItem object; otherwise, <c>false</c>.</returns>
		/// <param name="obj">LuaTable with object to check.</param>
		public bool IsItem(object obj)
		{
			return obj is Item;
		}

		/// <summary>
		/// Check, if the given LuaTable obj is a ZTask object.
		/// </summary>
		/// <returns><c>true</c> if obj is a ZTask object; otherwise, <c>false</c>.</returns>
		/// <param name="obj">LuaTable with object to check.</param>
		public bool IsTask(object obj)
		{
			return obj is Task;
		}

		/// <summary>
		/// Check, if the given LuaTable obj is a Thing object.
		/// </summary>
		/// <returns><c>true</c> if obj is a Thing object; otherwise, <c>false</c>.</returns>
		/// <param name="obj">LuaTable with object to check.</param>
		public bool IsThing(object obj)
		{
			return obj is Thing;
		}

		/// <summary>
		/// Check, if the given LuaTable obj is a UIObject.
		/// </summary>
		/// <returns><c>true</c> if obj is a UIObject; otherwise, <c>false</c>.</returns>
		/// <param name="obj">LuaTable with object to check.</param>
		public bool IsUIObject(object obj)
		{
			return obj is UIObject;
		}

		/// <summary>
		/// Check, if the given LuaTable obj is a Zone object.
		/// </summary>
		/// <returns><c>true</c> if obj is a Zone object; otherwise, <c>false</c>.</returns>
		/// <param name="obj">LuaTable with object to check.</param>
		public bool IsZone(object obj)
		{
			return obj is Zone;
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
//            output.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, });
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
				// Don't save default values
//				if (entry.Value is bool && !(bool)entry.Value)
//					continue;
//				if (entry.Value is double && (double)entry.Value == 0)
//					continue;
//				if (entry.Value is string && ((string)entry.Value).Equals (""))
//					continue;

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

    }

	#region Helper class

	class ThreadParams
	{
		public LuaTable Obj;
		public string Func;
		public object[] Parameter;
		public bool Running;

		public ThreadParams(LuaTable obj,string func,object[] parameter)
		{
			Obj = obj;
			Func = func;
			Parameter = parameter;
		}

	}

	#endregion

}
