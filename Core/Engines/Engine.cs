///
/// WF.Player.Core - A Wherigo Player Core for different platforms.
/// Copyright (C) 2012-2014  Dirk Weltz <web@weltz-online.de>
/// Copyright (C) 2012-2014  Brice Clocher <contact@cybisoft.net>
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
				get { return _engine._luaExecQueue; }
			}

			public Character Player
			{
				get { return _engine._player; }
			}

			public Cartridge Cartridge
			{
				get { return _engine._cartridge; }
			}

			internal LuaDataFactoryHelper(Engine engine)
			{
				this._engine = engine;
			}
		}

		#endregion

		#region Fields

		private IPlatformHelper _platformHelper;

		private Cartridge _cartridge;
		private Character _player;

		private double _lat = 0;
		private double _lon = 0;
		private double _alt = 0;
		private double _accuracy = 0;
		private bool _hasValidLocation = false;
		private bool _hasDirtyLocation = false;
		//private double heading = 0;

		private WherigoCollection<Thing> _visibleInventory;
		private WherigoCollection<Thing> _visibleObjects;
		private WherigoCollection<Zone> _activeVisibleZones;
		private WherigoCollection<Task> _activeVisibleTasks;

		private EngineGameState _gameState;
		private bool _isReady;
		private bool _isBusy;

		private ExecutionQueue _luaExecQueue;
		private ActionPump _uiDispatchPump;

		private WIGInternalImpl _wigInternal;

		private Dictionary<int, System.Threading.Timer> _timers;

		private LuaDataFactory _dataFactory;

		private GeoMathHelper _geoMathHelper;

		private object _syncRoot = new object();

		#endregion

		#region Constants

		private const int INTERNAL_TIMER_DURATION = 1000;

		public const string CORE_PLATFORM = "WF.Player.Core";
		public const string CORE_VERSION = "0.3.0";

		#endregion

		#region Events

		public event EventHandler<AttributeChangedEventArgs> AttributeChanged;
		public event EventHandler<WherigoEventArgs> CartridgeCompleted;
		public event EventHandler<CrashEventArgs> CartridgeCrashed;
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
			_platformHelper = platform;
			_timers = new Dictionary<int, System.Threading.Timer>();
			_dataFactory = new LuaDataFactory(new LuaDataFactoryHelper(this));
			_geoMathHelper = new GeoMathHelper(_dataFactory);

			// Create Wherigo environment
			_wigInternal = new WIGInternalImpl(this, _dataFactory);

			// Set definitions from Wherigo for ShowScreen
			LuaDataContainer wherigoTable = _dataFactory.GetContainerAt("Wherigo");
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
			LuaDataContainer env = _dataFactory.CreateContainerAt("Env");

			// Set defaults
			env["Ok"] = _platformHelper.Ok;
			env["EmptyYouSeeListText"] = _platformHelper.EmptyYouSeeListText;
			env["EmptyInventoryListText"] = _platformHelper.EmptyInventoryListText;
			env["EmptyTasksListText"] = _platformHelper.EmptyTasksListText;
			env["EmptyZonesListText"] = _platformHelper.EmptyZonesListText;
			env["EmptyTargetListText"] = _platformHelper.EmptyTargetListText;
			env["CartFolder"] = _platformHelper.CartridgeFolder;
			env["SyncFolder"] = _platformHelper.SavegameFolder;
			env["LogFolder"] = _platformHelper.LogFolder;
			env["PathSep"] = _platformHelper.PathSeparator;
			env["Downloaded"] = 0.0;
			env["Platform"] = String.Format("{0} ({1})", CORE_PLATFORM, _platformHelper.Platform);
			env["Device"] = _platformHelper.Device;
			env["DeviceID"] = _platformHelper.DeviceId;
			env["Version"] = String.Format("{0} ({1} {2})", _platformHelper.ClientVersion, CORE_PLATFORM, CORE_VERSION);

			// Creates job queues that runs in another thread.
			_luaExecQueue = new ExecutionQueue() { DefaultFallbackAction = HandleLuaExecQueueJobException };
			_uiDispatchPump = new ActionPump();

			// Sets some event handlers for the job queues.
			_luaExecQueue.IsBusyChanged += new EventHandler(HandleLuaExecQueueIsBusyChanged);
			_uiDispatchPump.IsBusyChanged += new EventHandler(HandleUIDispatchPumpIsBusyChanged);

            // Children classes can initialize now.
            InitInstanceOverride();

			// Sets the game state.
			GameState = EngineGameState.Uninitialized;
		}

        protected virtual void InitInstanceOverride()
        {
            
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

		protected virtual void DisposeOverride(bool disposeManagedResources)
		{

		}

		private void Dispose(
			bool disposeManagedResources, 
			EngineGameState? duringState = EngineGameState.Disposing, 
			EngineGameState? afterState = EngineGameState.Disposed)
		{
			// Returns if the engine is already disposed.
			lock (_syncRoot)
			{
				if (_gameState == EngineGameState.Disposed)
				{
					return;
				}
			}

			// State change?
			if (duringState.HasValue)
			{
				GameState = duringState.Value; 
			}

			// Safe lua goes into disposal mode.
			_dataFactory.LuaStateRethrowsExceptions = false;

			// Let's clean managed resources first, since some of them may still try
			// to access this instance's properties while this method is executing.
			if (disposeManagedResources)
			{
				// Bye bye timers.
				DisposeTimers();

				// Bye bye threads.

				// This is disposed before the dispatch pump, because it may still
				// be executing actions that will try to use the pump.
				if (_luaExecQueue != null)
				{
					_luaExecQueue.IsBusyChanged -= new EventHandler(HandleLuaExecQueueIsBusyChanged);
					_luaExecQueue.Dispose();
					lock (_syncRoot)
					{
						_luaExecQueue = null;
					}
				}

				// This is disposed before this instance's properties because it
				// may still be firing events that will try to access them.
				if (_uiDispatchPump != null)
				{
					_uiDispatchPump.IsBusyChanged -= new EventHandler(HandleUIDispatchPumpIsBusyChanged);
					_uiDispatchPump.Dispose();
					lock (_syncRoot)
					{
						_uiDispatchPump = null;
					}
				}

				// Disposes the data factory last.
				_dataFactory.Dispose();
			}

			// Now that resources that may rely on the following resources
			// are either disposed or purposely ignored, the rest of
			// this instance's resources can be disposed.
			lock(_syncRoot)
			{
				// Clears some members set by Init().
				if (_cartridge != null)
				{
					_cartridge.DataContainer = null;
					_cartridge = null;
					RaisePropertyChanged("Cartridge", false);
				}
				if (_player != null)
				{
					_player.DataContainer = null;
					_player = null;
					RaisePropertyChanged("Player", false);
				}
				_geoMathHelper = null;

				// Unhooks WIGInternal.
				if (_wigInternal != null)
				{
					_wigInternal = null;
				}

				// Clears the different lists.
				ActiveVisibleTasks = null;
				ActiveVisibleZones = null;
				VisibleInventory = null;
				VisibleObjects = null;
			}

			// Children can dispose things now.
			DisposeOverride(disposeManagedResources);

			// State change.
			if (afterState.HasValue)
			{
				GameState = afterState.Value;
			}
		}

		private void DisposeTimers()
		{
			AutoResetEvent waitHandle = new AutoResetEvent(false);

			foreach (var timer in _timers.Values.ToList())
			{
				timer.Dispose(waitHandle);
				waitHandle.WaitOne();
			}

			lock (_syncRoot)
			{
				_timers.Clear();
			}
		}

		#endregion

		#region Properties

		#region Public
		public double Altitude
		{
			get
			{
				lock (_syncRoot)
				{
					return _alt;
				}
			}
		}

		public double Accuracy
		{
			get
			{
				lock (_syncRoot)
				{
					return _accuracy;
				}
			}
		}

		/// <summary>
		/// Gets the bounds of all visible zones and items of this cartridge.
		/// </summary>
		/// <value>The bounds.</value>
		public CoordBounds Bounds
		{
			get
			{
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
				lock (_syncRoot)
				{
					return _cartridge;
				}
			}
		}

		//public double Heading
		//{
		//    get
		//    {
		//        lock (syncRoot)
		//        {
		//            return heading;
		//        }
		//    }
		//}

		public double Latitude
		{
			get
			{
				lock (_syncRoot)
				{
					return _lat;
				}
			}
		}

		public double Longitude
		{
			get
			{
				lock (_syncRoot)
				{
					return _lon;
				}
			}
		}

		public Character Player
		{
			get
			{
				lock (_syncRoot)
				{
					return _player;
				}
			}
		}

		public WherigoCollection<Task> ActiveVisibleTasks
		{
			get
			{
				lock (_syncRoot)
				{
					return _activeVisibleTasks ?? new WherigoCollection<Task>();
				}
			}

			private set
			{
				bool valueChanged = false;

				// Thread-safely sets the value.
				lock (_syncRoot)
				{
					if (_activeVisibleTasks != value)
					{
						_activeVisibleTasks = value;
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
				lock (_syncRoot)
				{
					return _activeVisibleZones ?? new WherigoCollection<Zone>();
				}
			}

			private set
			{
				bool valueChanged = false;

				// Thread-safely sets the value.
				lock (_syncRoot)
				{
					if (_activeVisibleZones != value)
					{
						_activeVisibleZones = value;
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
				lock (_syncRoot)
				{
					return _visibleInventory ?? new WherigoCollection<Thing>();
				}
			}

			private set
			{
				bool valueChanged = false;

				// Thread-safely sets the value.
				lock (_syncRoot)
				{
					if (_visibleInventory != value)
					{
						_visibleInventory = value;
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
				lock (_syncRoot)
				{
					return _visibleObjects ?? new WherigoCollection<Thing>();
				}
			}

			private set
			{
				bool valueChanged = false;

				// Thread-safely sets the value.
				lock (_syncRoot)
				{
					if (_visibleObjects != value)
					{
						_visibleObjects = value;
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
				lock (_syncRoot)
				{
					return _gameState;
				}
			}

			private set
			{
				EngineGameState gs;
				lock (_syncRoot)
				{
					gs = _gameState;
				}

				if (gs != value)
				{
					// Changes the game state.
					lock (_syncRoot)
					{
						_gameState = value;
					}

					// Checks if IsReady needs to be changed.
					bool newIsReady = value != EngineGameState.Uninitialized
						&& value != EngineGameState.Initializing
						&& value != EngineGameState.Disposed
						&& value != EngineGameState.Uninitializing
						&& value != EngineGameState.Disposing;
					if (newIsReady != IsReady)
					{
						// Changes IsReady.
						IsReady = newIsReady;
					}

					// This event is raised bypassing the ui dispatch pump because
					// it carries information that needs immediate processing by the UI,
					// even if lua processing has already started.
					RaisePropertyChangedExtended("GameState", gs, value, false);
				}
			}
		}

		public bool IsReady
		{
			get
			{
				lock (_syncRoot)
				{
					return _isReady;
				}
			}

			private set
			{
				bool ir;
				lock (_syncRoot)
				{
					ir = _isReady;
				}

				if (ir != value)
				{
					lock (_syncRoot)
					{
						_isReady = value;
					}

					// This event is raised bypassing the ui dispatch pump because
					// it carries information that needs immediate processing by the UI,
					// even if lua processing has already started.
					RaisePropertyChangedExtended("IsReady", ir, value, false);
				}
			}
		}

		public bool IsBusy
		{
			get
			{
				lock (_syncRoot)
				{
					return _isBusy;
				}
			}

			private set
			{
				bool ib;
				lock (_syncRoot)
				{
					ib = _isBusy;
				}

				if (ib != value)
				{
					lock (_syncRoot)
					{
						_isBusy = value;
					}

					// This event is raised bypassing the ui dispatch pump because
					// it carries information that needs immediate processing by the UI,
					// even if lua processing has already started.
					RaisePropertyChangedExtended("IsBusy", ib, value, false);
				}
			}
		}

		#endregion

		#region Internal

		/// <summary>
		/// Gets if the events that are related to property changes in 
		/// objects can be raised.
		/// </summary>
		private bool CanRaisePropertyEvent
		{
			get
			{
				EngineGameState state = GameState;

				return state != EngineGameState.Uninitialized
					&& state != EngineGameState.Uninitializing
					&& state != EngineGameState.Disposing
					&& state != EngineGameState.Disposed;
			}
		}

		#endregion

		#endregion

		#region Game Operations (Init, Start...)

		#region Init
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

			// Loads the cartridge code.
			CartridgeLoaders.Load(input, cartridge);

			// Performs the init.
			InitCore(cartridge);
		}

		/// <summary>
		/// Initializes this Engine with the data of a Cartridge that has been 
		/// previously loaded.
		/// </summary>
		/// <param name="cartridge">Cartridge object to init.</param>
		public void Init(Cartridge cartridge)
		{
			// Sanity checks.
			CheckStateIs(EngineGameState.Uninitialized, "The engine cannot be initialized in this state", true);
			if (!cartridge.IsLoaded)
				throw new InvalidOperationException("The cartridge is not loaded. Use Init(Stream, Cartridge) instead.");

			// State change.
			GameState = EngineGameState.Initializing;

			// Performs the init.
			InitCore(cartridge);
		}

		private void InitCore(Cartridge cart)
		{
			// Various sets.
			this._cartridge = cart;
			_dataFactory.GetContainerAt("Env")["CartFilename"] = _cartridge.Filename;

			// Set player relevant data
			_player = _dataFactory.GetWherigoObjectAt<Character>("Wherigo.Player");
			LuaDataContainer playerTable = (LuaDataContainer)_player.DataContainer;
			playerTable["CompletionCode"] = _cartridge.CompletionCode;
			playerTable["Name"] = _cartridge.Player;

			LuaDataContainer objLoc = playerTable.GetContainer("ObjectLocation");
			objLoc["latitude"] = _lat;
			objLoc["longitude"] = _lon;
			objLoc["altitude"] = _alt;

			// Now start Lua binary chunk
			byte[] luaBytes = _cartridge.Resources[0].Data;

			// TODO: Asynchronize below!

			// Runs the init code.
			LuaDataContainer cartridgeTable = _dataFactory.LoadProvider(luaBytes, _cartridge.Filename).FirstContainerOrDefault();
			playerTable["Cartridge"] = cartridgeTable;
			_cartridge.DataContainer = cartridgeTable;

			// State change.
			GameState = EngineGameState.Initialized;

			// Notifies of the property changes.
			RaisePropertyChanged("Cartridge", false);
			RaisePropertyChanged("Player", false);
		} 
		#endregion

		/// <summary>
		/// Resets the engine, unloading the current cartridge and restoring
		/// this instance to its original state.
		/// </summary>
		public void Reset()
		{
			// Sanity checks.
			CheckStateIs(EngineGameState.Initialized, "The engine is not in state Initialized.", true);

			// Silent dispose: Uninitializing during dispose, no state change after.
			Dispose(true, EngineGameState.Uninitializing, null);

			// Reinit instance.
			InitInstance(this._platformHelper);

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
			_luaExecQueue.BeginCallSelf(_cartridge, "Start");
			_luaExecQueue.WaitEmpty();

			// Refreshes the values.
			RefreshActiveVisibleTasksAsync();
			RefreshActiveVisibleZonesAsync();
			RefreshVisibleInventoryAsync();
			RefreshVisibleObjectsAsync();

			GameState = EngineGameState.Playing;

			// Now that the cartridge has started, process the location.
			ProcessLocationInternal();
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

			_luaExecQueue.BeginCallSelf(_cartridge, "Stop");
			_luaExecQueue.WaitEmpty();

			// The last one stops the sound ;)
			// Should be done immediately before the handler is gone
			RaiseStopSoundsRequested(false);

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

			new GWS(
				_cartridge,
				_player,
				_platformHelper,
				_dataFactory
			).Load(stream);

			_luaExecQueue.BeginCallSelf(_cartridge, "OnRestore");
			_luaExecQueue.WaitEmpty();

			// Refreshes the values.
			RefreshActiveVisibleTasksAsync();
			RefreshActiveVisibleZonesAsync();
			RefreshVisibleInventoryAsync();
			RefreshVisibleObjectsAsync();

			GameState = EngineGameState.Playing;

			// Now that the cartridge has started, process the location.
			ProcessLocationInternal();

			// Restarts the timers.
			RestartTimers();
		}

		/// <summary>
		/// Saves the game for the current cartridge.
		/// </summary>
		/// <param name="stream">Stream, where the cartridge is saved.</param>
		public void Save(Stream stream, string savename = null)
		{
			// Sanity checks.
			CheckStateIs(EngineGameState.Playing, "The engine is not playing.", true);

			// State change.
			GameState = EngineGameState.Saving;

			// Informs the cartridge that saving starts.
			_luaExecQueue.BeginCallSelf(_cartridge, "OnSync");
			_luaExecQueue.WaitEmpty();

			if (savename == null)
			{
				// Serialize all objects
				new GWS(
					_cartridge,
					_player,
					_platformHelper,
					_dataFactory
				).Save(stream);
			}
			else
			{
				// Serialize all objects
				new GWS(
					_cartridge,
					_player,
					_platformHelper,
					_dataFactory
				).Save(stream, savename);
			}

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
			_uiDispatchPump.IsPumping = false;
			_luaExecQueue.IsRunning = false;

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
			_uiDispatchPump.IsPumping = true;
			_luaExecQueue.IsRunning = true;

			// Restarts the timers.
			RestartTimers();

			// State change.
			GameState = EngineGameState.Playing;
		}

		public void FreeMemory()
		{
			_dataFactory.RunScript("collectgarbage(\"collect\")");
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
			return _dataFactory.GetWherigoObject<T>(id);
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
				wObj = _dataFactory.GetWherigoObject<T>(id) as T;
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

			lock (_syncRoot)
			{
				this._lat = lat;
				this._lon = lon;
				this._alt = alt;
				this._accuracy = accuracy;
				this._hasValidLocation = true;
			}

			ProcessLocationInternal();
		}

		private void ProcessLocationInternal()
		{
			// Checks if the location should be processed.
			bool shouldProcessLocation = false;
			lock (_syncRoot)
			{
				shouldProcessLocation =
					GameState == EngineGameState.Playing &&
					(this._hasValidLocation || this._hasDirtyLocation);
			}

			// If so, processes it.
			if (shouldProcessLocation)
			{
				//System.Diagnostics.Debug.WriteLine("Engine: Will process location: {0} {1} {2} {3}", lat, lon, alt, accuracy);
				_luaExecQueue.BeginCallSelfUnique(_player, "ProcessLocation", _lat, _lon, _alt, _accuracy);
			}

			// Marks the location clean or dirty depending on whether it was
			// processed this time or not.
			lock (_syncRoot)
			{
				this._hasDirtyLocation = !shouldProcessLocation;
			}
		}

		///// <summary>
		///// Refresh compass heading of device.
		///// </summary>
		///// <param name="heading">New heading in degrees.</param>
		//public void RefreshHeading(double heading)
		//{
		//    this.heading = heading;

		//    // TODO: Give it out to the lua engine?
		//}

		#endregion

		#region Utilities for Players

		public string CreateLogMessage(string message)
		{
			lock (_syncRoot)
			{
				return String.Format("{0:yyyyMMddhhmmss}|{1:+0.00000;-0.00000}|{2:+0.00000;-0.00000}|{3:+0.00000;-0.00000}|{4:+0.00000;-0.00000}|{5}", DateTime.Now.ToLocalTime(), _lat, _lon, _alt, _accuracy, message);
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
			if (!_luaExecQueue.IsSameThread)
			{
				_luaExecQueue.BeginAction(RefreshActiveVisibleTasksAsync);
				return;
			}

			if (_player == null)
				ActiveVisibleTasks = null;

			// This executes in the lua exec thread, so it's fine to block.
			ActiveVisibleTasks = _player.DataContainer.GetWherigoObjectListFromProvider<Task>("GetActiveVisibleTasks");
		}

		private void RefreshActiveVisibleZonesAsync()
		{
			// Sanity checks.
			if (!IsReady)
			{
				return;
			}

			// If we're not in the lua exec thread, be in it!
			if (!_luaExecQueue.IsSameThread)
			{
				_luaExecQueue.BeginAction(RefreshActiveVisibleZonesAsync);
				return;
			}

			if (_player == null)
				ActiveVisibleZones = null;

			// This executes in the lua exec thread, so it's fine to block.
			ActiveVisibleZones = _player.DataContainer.GetWherigoObjectListFromProvider<Zone>("GetActiveVisibleZones");
		}

		private void RefreshVisibleInventoryAsync()
		{
			// Sanity checks.
			if (!IsReady)
			{
				return;
			}

			// If we're not in the lua exec thread, be in it!
			if (!_luaExecQueue.IsSameThread)
			{
				_luaExecQueue.BeginAction(RefreshVisibleInventoryAsync);
				return;
			}

			if (_player == null)
				VisibleInventory = null;

			// This executes in the lua exec thread, so it's fine to block.
			VisibleInventory = _player.DataContainer.GetWherigoObjectListFromProvider<Thing>("GetVisibleInventory");
		}

		private void RefreshVisibleObjectsAsync()
		{
			// Sanity checks.
			if (!IsReady)
			{
				return;
			}

			// If we're not in the lua exec thread, be in it!
			if (!_luaExecQueue.IsSameThread)
			{
				_luaExecQueue.BeginAction(RefreshVisibleObjectsAsync);
				return;
			}

			if (_player == null)
				VisibleObjects = null;

			// This executes in the lua exec thread, so it's fine to block.
			VisibleObjects = _player.DataContainer.GetWherigoObjectListFromProvider<Thing>("GetVisibleObjects");
		}

		private void RefreshThingVectorFromPlayerAsync(Thing t)
		{
			// Sanity checks.
			if (!IsReady)
			{
				return;
			}

			// If we're not in the lua exec thread, be in it!
			if (!_luaExecQueue.IsSameThread)
			{
				_luaExecQueue.BeginAction(() => RefreshThingVectorFromPlayerAsync(t));
				return;
			}

			/// This below executes in the lua exec thread, so it's fine to block.

			// Gets more info about the thing.
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

			LocationVector ret;
			if (isZone)
			{
				ret = _geoMathHelper.VectorToZone(_player.ObjectLocation, (Zone)t);
			}
			else
			{
				ret = _geoMathHelper.VectorToPoint(_player.ObjectLocation, thingLoc);
			}

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
			if (_cartridge == null)
			{
                //System.Diagnostics.Debug.WriteLine("Engine: WARNING: HandleAttributeChanged called with null cartridge.");
				return;
			}

			if (obj == null)
			{
				System.Diagnostics.Debug.WriteLine("Engine: WARNING: HandleAttributeChanged called with null object.");
				return;
			}

			// Raises the NotifyPropertyChanged event if this is a Cartridge or UIObject.
			if (obj is Cartridge)
			{
				RaisePropertyChangedInObject(_cartridge, attribute);
			}
			else if (obj is UIObject)
			{
				RaisePropertyChangedInObject((UIObject)obj, attribute);
			}

			// Refreshes the zone in order to make it fire its events.
			if (obj is Zone && ("Active".Equals(attribute) || "Points".Equals(attribute)))
			{
				// If ProcessLocation is called during initialization, Lua crashes. 
				// But we still need the call to happen. That is why we defer the call 
				// to later, thanks to the execution queue.
				ProcessLocationInternal();
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
				_cartridge.Complete = true;

				// Raises the event.
				RaiseCartridgeCompleted(_cartridge);
			}
			else if ("sync".Equals(ls))
			{
				// Raises the event.
				RaiseSaveRequested(_cartridge, false);
			}

		}

		/// <summary>
		/// Event, which is called, if a command has changed.
		/// </summary>
		internal void HandleCommandChanged(Command command)
		{
			// Raises PropertyChanged on the command's owner.
			if (command.Owner != null)
			{
				RaisePropertyChangedInObject(command.Owner, "Commands");
				RaisePropertyChangedInObject(command.Owner, "ActiveCommands");
			}

			// TODO: Reciprocal commands need to raise PropertyChanged on the targets too.


			// Raises the event.
			RaiseCommandChanged(command);
		}

		/// <summary>
		/// Get an input from the user interface.
		/// </summary>
		/// <param name="input">Detail object for the input.</param>
		internal void HandleGetInput(Input input)
		{
			// Raise the event.
			RaiseInputRequested(input);
		}

		/// <summary>
		/// Event, which is called, if the inventory has changed.
		/// </summary>
		internal void HandleInventoryChanged(Thing obj, Thing from, Thing to)
		{
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
				RaiseSaveRequested(_cartridge, true);
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
				(retValue) => _luaExecQueue.BeginCall(provider, retValue)));
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
			UIObject obj = null;

			// Checks if an object is required.
			if (st == ScreenType.Details && idxObj > -1)
			{
				// Tries to get the object as an UIObject.
				obj = _dataFactory.GetWherigoObject<WherigoObject>(idxObj) as UIObject;

				// If the object is not a UIObject, discards the event because
				// it is not part of the Wherigo spec.
				if (obj == null)
				{
					return;
				}
			}
			
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
			bool leqIsBusy = _luaExecQueue.IsBusy;

			// Sets the UI dispatching action pump to be pumping when the lua exec queue
			// is not busy.
			_uiDispatchPump.IsPumping = !leqIsBusy;

			// The engine is busy if the lua execution queue or the ui dispatch pump are busy.
			IsBusy = leqIsBusy || _uiDispatchPump.IsBusy;
		}

		private void HandleUIDispatchPumpIsBusyChanged(object sender, EventArgs e)
		{
			// The engine is busy if the lua execution queue or the ui dispatch pump are busy.
			IsBusy = _luaExecQueue.IsBusy || _uiDispatchPump.IsBusy;
		}

		private void HandleLuaExecQueueJobException(Exception ex)
		{
			// An exception occurred while executing a job.
			// The engine can no longer play.
			RaiseCartridgeCrashed(ex, true);
		}

		#endregion

		#region Timers

		/// <summary>
		/// Starts an OS timer corresponding to a Wherigo ZTimer.
		/// </summary>
		internal void HandleTimerStarted(Timer timer)
		{
			// Starts a timer.
			CreateAndStartInternalTimer(timer.ObjIndex);

			// Call OnStart of this timer
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
			System.Threading.Timer timer = new System.Threading.Timer(InternalTimerTick, objIndex, Timeout.Infinite, INTERNAL_TIMER_DURATION);

			// Keeps track of the timer.
			// TODO: What happens if the timer is already in the dictionary?
			lock (_syncRoot)
			{
				if (!_timers.ContainsKey(objIndex))
					_timers.Add(objIndex, timer);
			}

			// Starts the timer, now that it is registered.
			timer.Change(INTERNAL_TIMER_DURATION, INTERNAL_TIMER_DURATION);

			return timer;
		}

		/// <summary>
		/// Stops an OS timer corresponding to a Wherigo ZTimer.
		/// </summary>
		internal void HandleTimerStopped(Timer timerEntity)
		{
			int objIndex = timerEntity.ObjIndex;

			// TODO: What happens if the timer is not in the dictionary?
			bool shouldRemove = false;
			lock (_syncRoot)
			{
				shouldRemove = _timers.ContainsKey(objIndex);
			}
			if (shouldRemove)
			{
				System.Threading.Timer timer = _timers[objIndex];

				timer.Dispose();
				lock (_syncRoot)
				{
					_timers.Remove(objIndex);
				}
			}

			// Call OnStop of this timer
			timerEntity.CallSelf("Stop");
		}

		/// <summary>
		/// Updates the ZTimer's attributes and checks if its Tick event should be called.
		/// </summary>
		/// <param name="source">ObjIndex of the timer that released the tick.</param>
		private void InternalTimerTick(object source)
		{
			int objIndex = (int)source;

			LuaDataContainer t = _dataFactory.GetContainer(objIndex);

			// Gets the ZTimer's properties.
			double elapsedRaw = t.GetDouble("Elapsed").GetValueOrDefault();
			double? remainingRaw = t.GetDouble("Remaining");
			if (remainingRaw == null)
			{
				remainingRaw = t.GetDouble("Duration").GetValueOrDefault();
			}

			double elapsed = elapsedRaw * INTERNAL_TIMER_DURATION;
			double remaining = remainingRaw.Value * INTERNAL_TIMER_DURATION;

			// Updates the ZTimer properties and considers if it should tick.
			elapsed += INTERNAL_TIMER_DURATION;
			remaining -= INTERNAL_TIMER_DURATION;

			bool shoudTimerTick = false;
			if (remaining <= 0.0d)
			{
				remaining = 0;

				shoudTimerTick = true;
			}

			t["Elapsed"] = elapsed / INTERNAL_TIMER_DURATION;
			t["Remaining"] = remaining / INTERNAL_TIMER_DURATION;

			// Call only, if timer still exists.
			// It could be, that function is called from thread, even if the timer didn't exists anymore.
			bool timerExists = false;
			lock (_syncRoot)
			{
				timerExists = _timers.ContainsKey(objIndex);
			}
			if (shoudTimerTick && timerExists)
			{
				// Disables and removes the current timer.
				System.Threading.Timer timer = _timers[objIndex];
				timer.Dispose();
				lock (_syncRoot)
				{
					_timers.Remove(objIndex);
				}

				// Call OnTick of this timer
				_luaExecQueue.BeginCallSelf(t, "Tick");
			}
		}

		/// <summary>
		/// Restarts all timers of the current cartridge that are marked
		/// to be restarted.
		/// </summary>
		private void RestartTimers()
		{
			// Gets the active timers.
			var timers = _player.DataContainer.GetWherigoObjectListFromProvider<Timer>("GetActiveTimers");

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
			// Raises this event in the UI thread, using the pump.
			RaisePropertyChanged(propName, true);
		}

		/// <summary>
		/// Asynchronously invokes an action to be run in the UI thread.
		/// </summary>
		/// <param name="action"></param>
		private void BeginInvokeInUIThread(Action action, bool usePump = false)
		{
			// Throws an exception if there is no handler for this.
			if (!_platformHelper.CanDispatchOnUIThread)
				throw new InvalidOperationException("Unable to dispatch on UI Thread. Make sure to construct Engine with a IPlatformHelper that implements DispatchOnUIThread() and has CanDispatchOnUIThread return true.");

			// The pump cannot be used when the engine is resetting or disposing.
			// In those cases, a direct UI dispatch is forced.
			if (usePump)
			{
				bool isNotReadyForPump = false;
				EngineGameState currentGameState;
				lock (_syncRoot)
				{
					currentGameState = _gameState;
					isNotReadyForPump = !_isReady && _gameState != EngineGameState.Initializing;
				}

				if (isNotReadyForPump)
				{
                    //System.Diagnostics.Debug.WriteLine("Engine: WARNING: Cannot use UI dispatch pump in state {0}, forced direct.",  currentGameState.ToString());
					usePump = false;
				}
			}

			if (usePump)
			{
				// Adds a sync request action to the action pump.
				_uiDispatchPump.AcceptAction(new Action(() => _platformHelper.BeginDispatchOnUIThread(action)));
			}
			else
			{
				// Invokes the event right away.
				_platformHelper.BeginDispatchOnUIThread(action);
			}
		}

		private void RaisePropertyChangedInObject(UIObject obj, string propName)
		{
			BeginInvokeInUIThread(() =>
			{
				if (CanRaisePropertyEvent)
				{
					obj.NotifyPropertyChanged(propName);
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("Engine: WARNING: Ignored RaisePropertyChangedInObject because engine is not in a legal state. ({0})", GameState);
				}
			}, true);
		}

		private void RaisePropertyChangedInObject(Cartridge obj, string propName)
		{
			BeginInvokeInUIThread(() =>
			{
				if (CanRaisePropertyEvent)
				{
					obj.NotifyPropertyChanged(propName);
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("Engine: WARNING: Ignored RaisePropertyChangedInObject because engine is not in a legal state. ({0})", GameState);
				}
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

		private void RaisePropertyChangedExtended<T>(string propName, T oldValue, T newValue, bool usePump = true)
		{
			BeginInvokeInUIThread(() =>
			{
				PropertyChangedExtendedEventArgs<T> e = new PropertyChangedExtendedEventArgs<T>(propName, oldValue, newValue);

				if (PropertyChanged != null)
				{
					PropertyChanged(this, e);
				}
			}, usePump);
		}

		private void RaiseLogMessageRequested(LogLevel level, string message)
		{
			BeginInvokeInUIThread(() =>
			{
				if (LogMessageRequested != null)
				{
					LogMessageRequested(this, new LogMessageEventArgs(_cartridge, level, message));
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

				InputRequested(this, new ObjectEventArgs<Input>(_cartridge, input));
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

				ShowMessageBoxRequested(this, new MessageBoxEventArgs(_cartridge, mb));
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

				PlayMediaRequested(this, new ObjectEventArgs<Media>(_cartridge, media));
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

				ShowScreenRequested(this, new ScreenEventArgs(_cartridge, kind, obj));
			});
		}

		private void RaiseInventoryChanged(Thing obj, Thing fromContainer, Thing toContainer)
		{
			BeginInvokeInUIThread(() =>
			{
				if (InventoryChanged != null)
				{
					InventoryChanged(this, new InventoryChangedEventArgs(_cartridge, obj, fromContainer, toContainer));
				}
			});
		}

		private void RaiseAttributeChanged(WherigoObject obj, string propName)
		{
			BeginInvokeInUIThread(() =>
			{
				if (CanRaisePropertyEvent)
				{
					if (AttributeChanged != null)
					{
						AttributeChanged(this, new AttributeChangedEventArgs(_cartridge, obj, propName));
					}
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("Engine: WARNING: Ignored RaiseAttributeChanged because engine is not in a legal state. ({0})", GameState);
				}
			}, true);
		}

		private void RaiseCommandChanged(Command command)
		{
			BeginInvokeInUIThread(() =>
			{
				if (CommandChanged != null)
				{
					CommandChanged(this, new ObjectEventArgs<Command>(_cartridge, command));
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

		private void RaiseCartridgeCrashed(Exception exception, bool throwIfNoHandler = true)
		{
			BeginInvokeInUIThread(() =>
			{
				if (CartridgeCrashed == null)
				{
					if (throwIfNoHandler)
					{
						throw new InvalidOperationException("No CartridgeCrashed handler has been found.");
					}
					else
					{
						return;
					}
				}

				CartridgeCrashed(this, new CrashEventArgs(Cartridge, exception));
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
					ZoneStateChanged(this, new ZoneStateChangedEventArgs(_cartridge, list));
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

				StopSoundsRequested(this, new WherigoEventArgs(_cartridge));
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

				PlayAlertRequested(this, new WherigoEventArgs(_cartridge));
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

				ShowStatusTextRequested(this, new StatusTextEventArgs(_cartridge, text));
			});
		}

		#endregion

		#region GameState Checks

		/// <summary>
		/// Checks if this Engine's internal state is ready to provide access to its Lua resources.
		/// </summary>
		protected void CheckStateForLuaAccess()
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

				case EngineGameState.Disposing:
					throw new InvalidOperationException("The engine is disposing.");

				default:
					return;
			}
		}

		/// <summary>
		/// Checks if this Engine's internal state is ready to perform a state-changing game operation
		/// such as Start, Stop or Resume.
		/// </summary>
		protected void CheckStateForConcurrentGameOperation()
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
		protected void CheckStateIs(EngineGameState target, string exMessage, bool exShowsDetails = false)
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
		protected void CheckStateIsNot(EngineGameState target, string exMessage, bool exShowsDetails = false)
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
		Disposing,
		Disposed
	}

	/// <summary>
	/// Event args for a change in property with specified values.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class PropertyChangedExtendedEventArgs<T> : PropertyChangedEventArgs
	{
		public virtual T OldValue { get; private set; }
		public virtual T NewValue { get; private set; }

		public PropertyChangedExtendedEventArgs(string propertyName, T oldValue, T newValue)
			: base(propertyName)
		{
			OldValue = oldValue;
			NewValue = newValue;
		}
	}

	#endregion

}
