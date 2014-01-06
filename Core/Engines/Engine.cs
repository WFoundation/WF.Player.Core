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
using System.Threading;
using System.Collections;
using WF.Player.Core.Utils;
using WF.Player.Core.Threading;
using WF.Player.Core.Formats;
using WF.Player.Core.Data;
using WF.Player.Core.Data.Lua;

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
        #region Nested Classes

        private class LuaDataFactoryHelper : LuaDataFactory.IHelper
        {
            private Engine _engine;

            public ExecutionQueue LuaExecutionQueue
            {
                get { return _engine.luaExecQueue; }
            }

            public Character Player
            {
                get { return _engine.player; }
            }

            public Cartridge Cartridge
            {
                get { return _engine.cartridge; }
            }

            internal LuaDataFactoryHelper(Engine engine)
            {
                this._engine = engine;
            }
        }  

        #endregion

        #region Private variables

		private IPlatformHelper platformHelper;

		private Cartridge cartridge;
		private Character player;

		private double lat = 0;
		private double lon = 0;
		private double alt = 0;
		private double accuracy = 0;
		private double heading = 0;

        private WherigoCollection<Thing> visibleInventory;
        private WherigoCollection<Thing> visibleObjects;
        private WherigoCollection<Zone> activeVisibleZones;
        private WherigoCollection<Task> activeVisibleTasks;

		private EngineGameState gameState;
		private bool isReady;
		private bool isBusy;

		private ExecutionQueue luaExecQueue;
		private ActionPump uiDispatchPump;

        private WIGInternalImpl wherigo;

		private Dictionary<int, System.Threading.Timer> timers;

		private LuaDataFactory dataFactory;

		private object syncRoot = new object();

        #endregion

		#region Constants

		private const int internalTimerDuration = 1000;

        public static readonly string CorePlatform = "WF.Player.Core";
		public static readonly string CoreVersion = "0.3.0";

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

        //[DirectLuaUsage(ShouldBeRefactored=true)]
		private void InitInstance(IPlatformHelper platform)
		{
			if (platform == null)
				throw new ArgumentNullException("platform");
			
			// Base objects.
			platformHelper = platform;

			//luaState = new LuaRuntime();

			//safeLuaState = new Utils.SafeLua(luaState)
            //safeLuaState = new Utils.SafeLua()
            //{
            //    RethrowsExceptions = true,
            //    RethrowsDisposedLuaExceptions = false
            //};

			timers = new Dictionary<int, System.Threading.Timer>();

			//uiObjects = new Dictionary<int, UIObject>();
            dataFactory = new LuaDataFactory(new LuaDataFactoryHelper(this));

			// Create Wherigo environment
			wherigo = new WIGInternalImpl(this, dataFactory);

			// Set definitions from Wherigo for ShowScreen
            //LuaTable wherigoTable = safeLuaState.SafeGetGlobal<LuaTable>("Wherigo");
            LuaDataContainer wherigoTable = dataFactory.GetContainerAt("Wherigo");
            //safeLuaState.SafeSetField(wherigoTable, "MAINSCREEN", (int)ScreenType.Main);
            //safeLuaState.SafeSetField(wherigoTable, "LOCATIONSCREEN", (int)ScreenType.Locations);
            //safeLuaState.SafeSetField(wherigoTable, "ITEMSCREEN", (int)ScreenType.Items);
            //safeLuaState.SafeSetField(wherigoTable, "INVENTORYSCREEN", (int)ScreenType.Inventory);
            //safeLuaState.SafeSetField(wherigoTable, "TASKSCREEN", (int)ScreenType.Tasks);
            //safeLuaState.SafeSetField(wherigoTable, "DETAILSCREEN", (int)ScreenType.Details);
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
            //safeLuaState.SafeSetField(wherigoTable, "LOGDEBUG", (int)LogLevel.Debug);
            //safeLuaState.SafeSetField(wherigoTable, "LOGCARTRIDGE", (int)LogLevel.Cartridge);
            //safeLuaState.SafeSetField(wherigoTable, "LOGINFO", (int)LogLevel.Info);
            //safeLuaState.SafeSetField(wherigoTable, "LOGWARNING", (int)LogLevel.Warning);
            //safeLuaState.SafeSetField(wherigoTable, "LOGERROR", (int)LogLevel.Error);

			// Get information about the player
			// Create table for Env, ...
            ////luaState.Globals["Env"] = luaState.CreateTable();
            ////LuaTable env = (LuaTable)luaState.Globals["Env"];
            //LuaTable env = safeLuaState.SafeCreateTable();
            //safeLuaState.SafeSetGlobal("Env", env);
            LuaDataContainer env = dataFactory.CreateContainerAt("Env");

			// Set defaults
			env["Ok"] = platformHelper.Ok;
			env["EmptyYouSeeListText"] = platformHelper.EmptyYouSeeListText;
			env["EmptyInventoryListText"] = platformHelper.EmptyInventoryListText;
			env["EmptyTasksListText"] = platformHelper.EmptyTasksListText;
			env["EmptyZonesListText"] = platformHelper.EmptyZonesListText;
			env["EmptyTargetListText"] = platformHelper.EmptyTargetListText;
            env["CartFolder"] = platformHelper.CartridgeFolder;
            env["SyncFolder"] = platformHelper.SavegameFolder;
            env["LogFolder"] = platformHelper.LogFolder;
            env["PathSep"] = platformHelper.PathSeparator;
            env["Downloaded"] = 0.0;
            env["Platform"] = String.Format("{0} ({1})", CorePlatform, platformHelper.Platform);
            env["Device"] = platformHelper.Device;
            env["DeviceID"] = platformHelper.DeviceId;
            env["Version"] = String.Format("{0} ({1} {2})", platformHelper.ClientVersion, CorePlatform, CoreVersion);
            //safeLuaState.SafeSetField(env, "CartFolder", platformHelper.CartridgeFolder);
            //safeLuaState.SafeSetField(env, "SyncFolder", platformHelper.SavegameFolder);
            //safeLuaState.SafeSetField(env, "LogFolder", platformHelper.LogFolder);
            //safeLuaState.SafeSetField(env, "PathSep", platformHelper.PathSeparator);
            //safeLuaState.SafeSetField(env, "Downloaded", 0.0);
            //safeLuaState.SafeSetField(env, "Platform", String.Format("{0} ({1})", CorePlatform, platformHelper.Platform));
            //safeLuaState.SafeSetField(env, "Device", platformHelper.Device);
            //safeLuaState.SafeSetField(env, "DeviceID", platformHelper.DeviceId);
            //safeLuaState.SafeSetField(env, "Version", String.Format("{0} ({1} {2})", platformHelper.ClientVersion, CorePlatform, CoreVersion));

            // Creates job queues that runs in another thread.
            luaExecQueue = new ExecutionQueue();
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
				dataFactory.LuaStateRethrowsExceptions = false;
			 
				// Clears some members set by Init().
				if (cartridge != null)
				{
					cartridge.DataContainer = null;
					cartridge = null;
				}
				if (player != null)
				{
					player.DataContainer = null;
					player = null;
				}

				// Unhooks WIGInternal.
				if (wherigo != null)
				{
					wherigo = null;
				}

                // Clears the different lists.
                ActiveVisibleTasks = null;
                ActiveVisibleZones = null;
                VisibleInventory = null;
                VisibleObjects = null;
			}

			// Cleans managed resources in here.
			if (disposeManagedResources)
			{
				// Bye bye timers.
				DisposeTimers();

				// Bye bye UI objects.
				//foreach (UIObject uiObject in uiObjects.Values)
				//{
				//    // TODO: Check, if this is correct
				//    //					uiObject.WIGTable.Dispose(disposeManagedResources);
				//    uiObject.WIGTable.Dispose();
				//}
				//uiObjects.Clear();
				
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
                ////if (luaState != null)
                ////{
                ////    lock (luaState)
                ////    {
                ////        luaState.Dispose();
                ////    }
                ////    luaState = null;
                ////    safeLuaState = null;
                ////}
                //if (safeLuaState != null)
                //{
                //    safeLuaState.Dispose();
                //}
                dataFactory.Dispose();
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

		/// <summary>
		/// Gets the bounds of all visible zones and items of this cartridge.
		/// </summary>
		/// <value>The bounds.</value>
		public CoordBounds Bounds {
			get {
                CoordBounds result = new CoordBounds();

                // Adds all active visible zones' bounds.
                result.Inflate(ActiveVisibleZones.Select(z => z.Bounds));
                
                // Adds all visible objects' locations.
                result.Inflate(VisibleObjects.Select(t => t.ObjectLocation));

                // Only returns the bounds if at least a point
                // has inflated it.
                return result.IsValid ? result : null;
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
				lock (syncRoot)
				{
					return player;
				}
			} 
		}

		public WherigoCollection<Task> ActiveVisibleTasks
		{
			get
			{
				lock (syncRoot)
				{
                    return activeVisibleTasks ?? new WherigoCollection<Task>();
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

        public WherigoCollection<Zone> ActiveVisibleZones
		{
			get
			{
				lock (syncRoot)
				{
                    return activeVisibleZones ?? new WherigoCollection<Zone>();
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
                {
                    RaisePropertyChanged("ActiveVisibleZones");
                    RaisePropertyChanged("Bounds");
                }
			}
		}

        public WherigoCollection<Thing> VisibleInventory
		{
			get
			{
				lock (syncRoot)
				{
                    return visibleInventory ?? new WherigoCollection<Thing>();
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

        public WherigoCollection<Thing> VisibleObjects
		{
			get
			{
				lock (syncRoot)
				{
                    return visibleObjects ?? new WherigoCollection<Thing>();
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
                {
                    RaisePropertyChanged("VisibleObjects");
                    RaisePropertyChanged("Bounds");
                }
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

		//private LuaExecutionQueue LuaExecQueue { get { return luaExecQueue; } }

		//internal SafeLua SafeLuaState { get { return safeLuaState; } }

		//internal IDataFactory<LuaTable> DataFactory { get { return dataFactory; } }

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

			// State change.
			GameState = EngineGameState.Initializing;

			// Various sets.
			this.cartridge = cartridge;
            ////((LuaTable)luaState.Globals["Env"])["CartFilename"] = cartridge.Filename;
            //safeLuaState.SafeSetGlobal("Env.CartFilename", cartridge.Filename);
            dataFactory.GetContainerAt("Env")["CartFilename"] = cartridge.Filename;

			// Loads the cartridge code.
			FileFormats.Load(input, cartridge);

			// Set player relevant data
            ////playerTable = (LuaTable)((LuaTable)luaState.Globals["Wherigo"])["Player"];
            ////playerTable["CompletionCode"] = cartridge.CompletionCode;
            ////playerTable["Name"] = cartridge.Player;
            //playerTable = safeLuaState.SafeGetGlobal<LuaTable>("Wherigo.Player");
            //safeLuaState.SafeSetField(playerTable, "CompletionCode", cartridge.CompletionCode);
            //safeLuaState.SafeSetField(playerTable, "Name", cartridge.Player);
            player = dataFactory.GetWherigoObjectAt<Character>("Wherigo.Player");
            LuaDataContainer playerTable = (LuaDataContainer) player.DataContainer;
            playerTable["CompletionCode"] = cartridge.CompletionCode;
            playerTable["Name"] = cartridge.Player;

            ////LuaTable objLoc = (LuaTable)playerTable["ObjectLocation"];
            ////objLoc["latitude"] = lat;
            ////objLoc["longitude"] = lon;
            ////objLoc["altitude"] = alt;
            //LuaTable objLoc = safeLuaState.SafeGetField<LuaTable>(playerTable, "ObjectLocation");
            //safeLuaState.SafeSetField(objLoc, "latitude", lat);
            //safeLuaState.SafeSetField(objLoc, "longitude", lon);
            //safeLuaState.SafeSetField(objLoc, "altitude", alt);
            LuaDataContainer objLoc = playerTable.GetContainer("ObjectLocation");
            objLoc["latitude"] = lat;
            objLoc["longitude"] = lon;
            objLoc["altitude"] = alt;

            // Now start Lua binary chunk
            byte[] luaBytes = cartridge.Resources[0].Data;

            // TODO: Asynchronize below!

            // Runs the init code and stores the Cartridge container.
            ////cartridgeTable = (LuaTable)luaState.DoString(luaBytes, cartridge.Filename)[0];
            ////playerTable["Cartridge"] = cartridgeTable;
            //cartridgeTable = (LuaTable)safeLuaState.SafeDoString(luaBytes, Cartridge.Filename)[0];
            //safeLuaState.SafeSetField(playerTable, "Cartridge", cartridgeTable);
            //cartridge = dataFactory.GetWherigoObject<Cartridge>(cartridgeTable);

            // Runs the init code.
            LuaDataContainer cartridgeTable = dataFactory.LoadProvider(luaBytes, cartridge.Filename).FirstContainerOrDefault();
            playerTable["Cartridge"] = cartridgeTable;
            cartridge.DataContainer = cartridgeTable;

            // State change.
            GameState = EngineGameState.Initialized;
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
			luaExecQueue.BeginCallSelf(cartridge, "Start");
			luaExecQueue.WaitEmpty();

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
			CheckStateIsNot(EngineGameState.Initialized, "The engine is already stopped.");
			
			GameState = EngineGameState.Stopping;

			DisposeTimers();

			luaExecQueue.BeginCallSelf(cartridge, "Stop");
			luaExecQueue.WaitEmpty();

			// The last one stops the sound ;)
			// Should be done immediately before the handler is gone
			if (StopSoundsRequested != null)
				StopSoundsRequested(this, new WherigoEventArgs(cartridge));

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
			CheckStateIsNot(EngineGameState.Playing, "The engine is already playing.");
			
			GameState = EngineGameState.Restoring;

			new FileGWS(
				cartridge,
				player,
				platformHelper,
				dataFactory
			).Load(stream);

			luaExecQueue.BeginCallSelf(cartridge, "OnRestore");
			luaExecQueue.WaitEmpty();

			// Refreshes the values.
			RefreshActiveVisibleTasksAsync();
			RefreshActiveVisibleZonesAsync();
			RefreshVisibleInventoryAsync();
			RefreshVisibleObjectsAsync();

			GameState = EngineGameState.Playing;

			// Restarts the timers.
            RestartTimers();
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
			luaExecQueue.BeginCallSelf(cartridge, "OnSync");
			luaExecQueue.WaitEmpty();

            // Serialize all objects
            new FileGWS(
                cartridge,
                player,
                platformHelper,
                dataFactory
            ).Save(stream);

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
            RestartTimers();

			// State change.
			GameState = EngineGameState.Playing;
		}

		public void FreeMemory()
		{
            ////lock (luaState) {
            ////    luaState.DoString ("collectgarbage(\"collect\")");
            ////}
            //safeLuaState.SafeDoString("collectgarbage(\"collect\")");
            dataFactory.RunScript("collectgarbage(\"collect\")");
		}

		#endregion

        #region Model Queries

        /// <summary>
        /// Gets a Wherigo object that has a certain id.
        /// </summary>
        /// <typeparam name="T">Type of the object to expect, subclass of WherigoObject.</typeparam>
        /// <param name="id">Id of the object to get.</param>
        /// <returns>A wherigo object of the expected type.</returns>
        /// <exception cref="InvalidOperationException">No object with such Id exists, or the object is not of the
        /// required type.</exception>
        public T GetWherigoObject<T>(int id) where T : WherigoObject
        {
            return dataFactory.GetWherigoObject<T>(id);
        }

        /// <summary>
        /// Gets a Wherigo object that has a certain id.
        /// </summary>
        /// <typeparam name="T">Type of the object to expect, subclass of WherigoObject.</typeparam>
        /// <param name="id">Id of the object to get.</param>
        /// <param name="wObj">A wherigo object of the expected type, or null if it wasn't found or is not
        /// of the expected type.</param>
        /// <returns>True if the method returned, false otherwise.</returns>
        public bool TryGetWherigoObject<T>(int id, out T wObj) where T : WherigoObject
        {
            try
            {
                wObj = dataFactory.GetWherigoObject<T>(id) as T;
            }
            catch (Exception)
            {
                wObj = null;
            }

            return wObj != null;
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

			if (GameState == EngineGameState.Playing)
				luaExecQueue.BeginCallSelf(player, "ProcessLocation", lat, lon, alt, accuracy);
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
			if (!luaExecQueue.IsSameThread)
			{
				luaExecQueue.BeginAction(RefreshActiveVisibleTasksAsync);
				return;
			}

			if (player == null)
				ActiveVisibleTasks = null;

			// This executes in the lua exec thread, so it's fine to block.
			ActiveVisibleTasks = player.DataContainer.GetWherigoObjectListFromProvider<Task>("GetActiveVisibleTasks");
		}

		private void RefreshActiveVisibleZonesAsync()
		{
			// Sanity checks.
			if (!IsReady)
			{
				return;
			}

			// If we're not in the lua exec thread, be in it!
			if (!luaExecQueue.IsSameThread)
			{
				luaExecQueue.BeginAction(RefreshActiveVisibleZonesAsync);
				return;
			}

			if (player == null)
				ActiveVisibleZones = null;

			// This executes in the lua exec thread, so it's fine to block.
			ActiveVisibleZones = player.DataContainer.GetWherigoObjectListFromProvider<Zone>("GetActiveVisibleZones");
		}

		private void RefreshVisibleInventoryAsync()
		{
			// Sanity checks.
			if (!IsReady)
			{
				return;
			}

			// If we're not in the lua exec thread, be in it!
			if (!luaExecQueue.IsSameThread)
			{
				luaExecQueue.BeginAction(RefreshVisibleInventoryAsync);
				return;
			}

			if (player == null)
				VisibleInventory = null;

			// This executes in the lua exec thread, so it's fine to block.
			VisibleInventory = player.DataContainer.GetWherigoObjectListFromProvider<Thing>("GetVisibleInventory");
		}

		private void RefreshVisibleObjectsAsync()
		{
			// Sanity checks.
			if (!IsReady)
			{
				return;
			}

			// If we're not in the lua exec thread, be in it!
			if (!luaExecQueue.IsSameThread)
			{
				luaExecQueue.BeginAction(RefreshVisibleObjectsAsync);
				return;
			}

			if (player == null)
				VisibleObjects = null;

			// This executes in the lua exec thread, so it's fine to block.
			VisibleObjects = player.DataContainer.GetWherigoObjectListFromProvider<Thing>("GetVisibleObjects");
		}

		private void RefreshThingVectorFromPlayerAsync(Thing t)
		{
			// Sanity checks.
			if (!IsReady)
			{
				return;
			}

			// If we're not in the lua exec thread, be in it!
			if (!luaExecQueue.IsSameThread)
			{
				luaExecQueue.BeginAction(() => RefreshThingVectorFromPlayerAsync(t));
				return;
			}

			/// This below executes in the lua exec thread, so it's fine to block.

			// Gets more info about the thing.
			//LuaValue thingLoc;
			//lock (luaState)
			//{
			//    thingLoc = t.WIGTable["ObjectLocation"];
			//}
			ZonePoint thingLoc = t.ObjectLocation;
			bool isZone = t is Zone;

			if (!isZone && thingLoc == null)
			{
                // If the Thing is not a zone and has no location, consider it is close to the player.
                //t.VectorFromPlayer = new LocationVector(dataFactory.CreateWherigoObject<Distance>(), 0);
                t.VectorFromPlayer = null;
				
				RaisePropertyChangedInObject(t, "VectorFromPlayer");
				return;
			}

			//LuaVararg ret;
			//if (isZone)
			//{
			//    lock (luaState)
			//    {
			//        ret = wherigo.VectorToZone(playerTable["ObjectLocation"], t.WIGTable);
			//    }
			//}
			//else
			//{
			//    lock (luaState)
			//    {
			//        ret = wherigo.VectorToPoint(playerTable["ObjectLocation"], thingLoc);
			//    }
			//}
			LocationVector ret;
			if (isZone)
			{
				ret = wherigo.VectorToZone(player.ObjectLocation, (Zone)t);
			}
			else
			{
				ret = wherigo.VectorToPoint(player.ObjectLocation, thingLoc);
			}

			//if (!(ret [0] is LuaNil) && !(ret [1] is LuaNil)) {
			//    t.VectorFromPlayer = new LocationVector ((Distance)GetTable ((LuaTable)ret [0]), (double)ret [1].ToNumber ());
			//    RaisePropertyChangedInObject (t, "VectorFromPlayer");
			//}

			t.VectorFromPlayer = ret;
			RaisePropertyChangedInObject(t, "VectorFromPlayer");

			return;
		}

		#endregion

		#region WIGInternal Event Handlers

		/// <summary>
        /// Event, which is called, if the attribute of an object has changed.
        /// </summary>
        /// <param name="obj">Object, which attribute has changed.</param>
        /// <param name="attribute">String with the name of the attribute that has changed.</param>
		internal void HandleAttributeChanged(WherigoObject obj, string attribute)
		{
            if (cartridge == null)
            {
                System.Diagnostics.Debug.WriteLine("Engine: WARNING: HandleAttributeChanged called with null cartridge.");
                return;
            }

            if (obj == null)
            {
                System.Diagnostics.Debug.WriteLine("Engine: WARNING: HandleAttributeChanged called with null object.");
                return;
            }
            
            // Raises the NotifyPropertyChanged event if this is a Cartridge.
            if (obj is Cartridge)
            {
                RaisePropertyChangedInObject(cartridge, attribute);
            }

			// Raises the NotifyPropertyChanged event if this is a UIObject.
            else if (obj is UIObject)
            {
                RaisePropertyChangedInObject((UIObject)obj, attribute);
            }

			// Refreshes the zone in order to make it fire its events.
			else if (obj is Zone && "Active".Equals(attribute))
			{
                // If ProcessLocation is called during initialization, Lua crashes. 
                // But we still need the call to happen. That is why we defer the call 
                // to later, thanks to the execution queue.
				if (GameState == EngineGameState.Playing)
                    luaExecQueue.BeginCallSelf(player, "ProcessLocation", lat, lon, alt, accuracy);
			}

			// Checks if an engine property has changed.
			bool isAttributeVisibleOrActive = "Active".Equals(attribute) || "Visible".Equals(attribute);
            if (isAttributeVisibleOrActive)
            {
                if (obj is Task)
                {
                    // Recomputes active visible tasks and raises the property changed event.
                    RefreshActiveVisibleTasksAsync();
                }
                else if (obj is Zone)
                {
                    // Recomputes active visible zones and raises the property changed event.
                    RefreshActiveVisibleZonesAsync();
                }
                else if (obj is Thing)
                {
                    // Recomputes the visible objects and raises the property changed event.
                    RefreshVisibleObjectsAsync();
                    RefreshVisibleInventoryAsync();
                } 
            }

			// Raises the AttributeChanged event.
			RaiseAttributeChanged(obj, attribute);
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
		internal void HandleCommandChanged(Command command)
		{
			// Raises PropertyChanged on the command's owner.
			if (command.Owner != null)
                RaisePropertyChangedInObject(command.Owner, "Commands");

			// TODO: Reciprocal commands need to raise PropertyChanged on the targets too.


			// Raises the event.
            RaiseCommandChanged(command);
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
        internal void HandleInventoryChanged(Thing obj, Thing from, Thing to)
		{
            //Thing obj = dataFactory.GetWherigoObject<Thing>(ltThing);
            //Thing from = dataFactory.GetWherigoObject<Thing>(ltFrom);
            //Thing to = dataFactory.GetWherigoObject<Thing>(ltTo);

			// Raises the PropertyChanged events on the objects.
			if (obj != null)
				RaisePropertyChangedInObject((UIObject)obj, "Container");
			if (from != null)
				RaisePropertyChangedInObject((UIObject)from, "Inventory");
			if (to != null)
				RaisePropertyChangedInObject((UIObject)to, "Inventory");

			// Check for player inventory changes.
			if (Player.Equals(to) || Player.Equals(from))
			{
				// Recomputes the visible inventory and raises the property changed event.
				RefreshVisibleInventoryAsync();
			}

			// Check for visible objects changes.
			if (from is Zone || to is Zone)
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
		internal void HandleLogMessage(int level, string message)
		{
			// Raise the event.
			RaiseLogMessageRequested((LogLevel)Enum.ToObject(typeof(LogLevel), level), message);
		}

		/// <summary>
		/// Notifies the user interface about a special command, which is sent from Lua.
		/// </summary>
		/// <param name="command">Name of command.</param>
		internal void HandleNotifyOS(string command)
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
		internal void HandlePlayMedia(int type, Media mediaObj)
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
		/// <param name="provider">Callback function, which is called, if one of the buttons is pressed or the message is cancelled.</param>
		internal void HandleShowMessage(string text, Media media, string btn1Label, string btn2Label, IDataProvider provider)
		{
			// Raise the event.
            RaiseMessageBoxRequested(new MessageBox(
                text,
                media,
                btn1Label,
                btn2Label,
                (retValue) => luaExecQueue.BeginCall(provider, retValue)));
		}

		/// <summary>
		/// Shows the screen via the user interface.
		/// </summary>
		/// <param name="screen">Screen number to show.</param>
		/// <param name="idxObj">Index of the object to show.</param>
		internal void HandleShowScreen(int screen, int idxObj)
		{
			// Gets the event parameters.
			ScreenType st = (ScreenType)Enum.ToObject(typeof(ScreenType), screen);
			UIObject obj = st == ScreenType.Details && idxObj > -1 ? dataFactory.GetWherigoObject<UIObject>(idxObj) : null;

			// Raise the event.
			RaiseScreenRequested(st, obj);
		}

		/// <summary>
		/// Shows the status text via user interface.
		/// </summary>
		/// <param name="text">Text to show.</param>
		internal void HandleShowStatusText(string text)
		{
			// Raise the event.
			RaiseShowStatusTextRequested(text);
		}


		/// <summary>
		/// Event, which is called, if the state of a zone has changed.
		/// </summary>
		internal void HandleZoneStateChanged(IEnumerable<Zone> zones)
		{
			//List<Zone> list = new List<Zone>();

			//// Generates the list of zones.
			//IEnumerator<KeyValuePair<LuaValue,LuaValue>> z;
			//bool run = true;
			//lock (luaState)
			//{
			//    z = zones.GetEnumerator();
			//    run = z.MoveNext();
			//}
			//while (run)
			//{
			//    // Gets a zone from the table.
			//    Zone zone = (Zone)GetTable((LuaTable)z.Current.Value);

			//    // Performs notifications.
			//    if (zone != null)
			//    {
			//        RaisePropertyChangedInObject((UIObject)zone, "State");
			//        RefreshThingVectorFromPlayerAsync(zone);
			//    }

			//    // Adds the zone to the list.
			//    list.Add(zone);

			//    // Keep on running?
			//    lock (luaState)
			//    {
			//        run = z.MoveNext();
			//    }
			//}

			// Generates the list of zones.
			foreach (var zone in zones)
			{
				RaisePropertyChangedInObject(zone, "State");
				RefreshThingVectorFromPlayerAsync(zone);
			}

			// The list of zones and objects has changed.
			RefreshActiveVisibleZonesAsync();
			RefreshVisibleObjectsAsync();

			// Notifies all visible objects that their distances have changed.
			VisibleObjects.ToList().ForEach(t => RefreshThingVectorFromPlayerAsync(t));

			// Raise the event.
			RaiseZoneStateChanged(zones);
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
        internal void HandleTimerStarted(Timer timer)
        {
			
			// Gets the object index of the Timer that started.
			//int objIndex;
			//lock (luaState)
			//{
			//    objIndex = Convert.ToInt32((double)t["ObjIndex"].ToNumber()); 
			//}

			// Starts a timer.
			CreateAndStartInternalTimer(timer.ObjIndex);

			// Call OnStart of this timer
			//lock (luaState)
			//{
			//    t.CallSelf("Start");
			//}
            //timer.Call("Start");
            timer.CallSelf("Start");
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
        internal void HandleTimerStopped(Timer timerEntity)
        {
			//int objIndex;
			//lock (luaState)
			//{
			//    objIndex = Convert.ToInt32((double)t["ObjIndex"].ToNumber()); 
			//}
            //Timer timerEntity = dataFactory.GetWherigoObject<Timer>(t);
			int objIndex = timerEntity.ObjIndex;

			// TODO: What happens if the timer is not in the dictionary?
			bool shouldRemove = false;
			lock (syncRoot)
			{
				shouldRemove = timers.ContainsKey(objIndex);
			}
			if (shouldRemove) {
				System.Threading.Timer timer = timers[objIndex];

				timer.Dispose();
				lock (syncRoot)
				{
					timers.Remove(objIndex); 
				}
			}

			// Call OnStop of this timer
			//lock (luaState)
			//{
			//    t.CallSelf("Stop");
			//}
            //timerEntity.Call("Stop");
            timerEntity.CallSelf("Stop");
        }

        /// <summary>
        /// Updates the ZTimer's attributes and checks if its Tick event should be called.
        /// </summary>
        /// <param name="source">ObjIndex of the timer that released the tick.</param>
        private void InternalTimerTick(object source)
        {
			int objIndex = (int)source;

            ////LuaTable t = GetObject(objIndex).WIGTable;
            //LuaTable t = dataFactory.GetNativeContainer(objIndex);
            LuaDataContainer t = dataFactory.GetContainer(objIndex);

			// Gets the ZTimer's properties.
            //LuaValue elapsedRaw = safeLuaState.SafeGetField<LuaValue>(t, "Elapsed");
            //LuaValue remainingRaw = safeLuaState.SafeGetField<LuaValue>(t, "Remaining");
            ////lock (luaState)
            ////{
            ////    elapsedRaw = t["Elapsed"];
            ////    remainingRaw = t["Remaining"]; 
            ////}
            //if (elapsedRaw == null || elapsedRaw is LuaNil)
            //    elapsedRaw = 0.0d;
            double elapsedRaw = t.GetDouble("Elapsed").GetValueOrDefault();
            double? remainingRaw = t.GetDouble("Remaining");
            //if (remainingRaw == null || remainingRaw is LuaNil)
            if (remainingRaw == null)
			{
                ////lock (luaState)
                ////{
                ////    remainingRaw = t["Duration"];
                ////}
                //remainingRaw = safeLuaState.SafeGetField<LuaValue>(t, "Duration");
                remainingRaw = t.GetDouble("Duration").GetValueOrDefault();
			}

            //double elapsed = (double)(LuaNumber)elapsedRaw.ToNumber() * internalTimerDuration;
            //double remaining = (double)(LuaNumber)remainingRaw.ToNumber() * internalTimerDuration;
            double elapsed = elapsedRaw * internalTimerDuration;
            double remaining = remainingRaw.Value * internalTimerDuration;

			// Updates the ZTimer properties and considers if it should tick.
			elapsed += internalTimerDuration;
			remaining -= internalTimerDuration;

			bool shoudTimerTick = false;
			if (remaining <= 0.0d)
			{
				remaining = 0;

				shoudTimerTick = true;
			}

            ////lock (luaState)
            ////{
            ////    t["Elapsed"] = elapsed / internalTimerDuration;
            ////    t["Remaining"] = remaining / internalTimerDuration; 
            ////}
            //safeLuaState.SafeSetField(t, "Elapsed", elapsed / internalTimerDuration);
            //safeLuaState.SafeSetField(t, "Remaining", remaining / internalTimerDuration);
            t["Elapsed"] = elapsed / internalTimerDuration;
            t["Remaining"] = remaining / internalTimerDuration;

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
				luaExecQueue.BeginCallSelf(t, "Tick");
			}
        }

        /// <summary>
        /// Restarts all timers of the current cartridge that are marked
        /// to be restarted.
        /// </summary>
        private void RestartTimers()
        {
            // Gets the active timers.
            var timers = player.DataContainer.GetWherigoObjectListFromProvider<Timer>("GetActiveTimers");
            
            // Restart those whose Restart provider gives true.
            foreach (Timer t in timers.Where(tim => tim.DataContainer.GetProvider("Restart").FirstOrDefault<bool>()))
            {
                // Creates the internal timer.
                CreateAndStartInternalTimer(t.ObjIndex);
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

		private void RaiseZoneStateChanged(IEnumerable<Zone> list)
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
