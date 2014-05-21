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
using Eluant;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using WF.Player.Core.Threading;
using System.Linq;
using WF.Player.Core.Utils;

namespace WF.Player.Core.Data.Lua
{
	/// <summary>
	/// A Lua implementation of a data factory.
	/// </summary>
    internal class LuaDataFactory : IDataFactory, IDisposable
    {
        #region Nested Classes

        private class FriendLuaDataContainer : LuaDataContainer
        {
#if DEBUG
			internal IEnumerable<object> KeyValuePairs
			{
				get
				{
					List<object> list = new List<object>();

					var e = GetEnumerator();
					while (e.MoveNext())
					{
						list.Add(e.Entry);
					}

					return list;
				}
			}
#endif
			
			internal LuaTable Table
            {
                get
                {
                    return _luaTable;
                }
            }

            internal FriendLuaDataContainer(LuaTable table, SafeLua lua, LuaDataFactory factory)
                : base(table, lua, factory)
            {
				
            }
        }

        private class FriendLuaDataProvider : LuaDataProvider
        {
            internal LuaFunction Function
            {
                get
                {
                    return _luaFunction;
                }
            }

            internal FriendLuaDataProvider(LuaFunction func, SafeLua lua, LuaDataFactory factory, LuaTable self)
                : base(func, lua, factory, self)
            {

            }
        }

        private class Node
        {
            internal FriendLuaDataContainer Container { get; set; }

            internal WherigoObject Object { get; set; }
        }

        /// <summary>
        /// A helper used by this factory to access external resources.
        /// </summary>
        internal interface IHelper
        {
            /// <summary>
            /// Gets an ExecutionQueue which can be used to sequentialize actions.
            /// </summary>
            ExecutionQueue LuaExecutionQueue { get; }

            /// <summary>
            /// Gets the player entity of the current cartridge.
            /// </summary>
            Character Player { get; }

            /// <summary>
            /// Gets the cartridge entity of the current game.
            /// </summary>
            Cartridge Cartridge { get; }
        }

        #endregion
        
        #region Fields

        private SafeLua _luaState;

        private object _syncRoot = new object();

        private Dictionary<int, Node> _wEntities = new Dictionary<int,Node>();

        private IHelper _helper;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether the underlying Lua state rethrows
        /// exception that happens at Lua's level or handle them
        /// and return default values instead.
        /// </summary>
        internal bool LuaStateRethrowsExceptions
        {
            get
            {
                return _luaState.RethrowsExceptions;
            }

            set
            {
                _luaState.RethrowsExceptions = value;
            }
        }

        #endregion

        #region Constructors

        internal LuaDataFactory(IHelper helper)
        {
            _luaState = new SafeLua()
            {
                RethrowsExceptions = true,
                RethrowsDisposedLuaExceptions = false
            };
            _helper = helper;
        }

        #endregion

		#region Destructors and IDisposable

		~LuaDataFactory()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);

			// Requests the GC to not finalize this object (best practice).
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposeManagedResources)
		{
			if (disposeManagedResources)
			{
				// Disposes every registered instance of wherigo entities.
				lock (_syncRoot)
				{
					foreach (var w in _wEntities)
					{
						w.Value.Container.Table.Dispose();
					}

					// TODO: Keep track of all GC-protected, non-indexed entities
					// and dispose them here?

					// Clears the entities dictionary.
					_wEntities.Clear();
				}

				// Good bye, Lua state!
				_luaState.Dispose();
			}
		}

		#endregion

        #region Methods

        /// <summary>
        /// Creates and returns a new container.
        /// </summary>
        /// <returns></returns>
        internal LuaDataContainer CreateContainer()
        {
            // Creates a LuaTable, unprotected from garbage collection.
            LuaTable lt = _luaState.SafeCreateTable();

            // Returns the container.
            return CreateContainerCore(lt);
        }

        /// <summary>
        /// Creates a new container and sets a global path with it.
        /// </summary>
        /// <remarks>
        /// If an object already exists at <paramref name="path"/>, it is
        /// replaced with the new container.
        /// </remarks>
        /// <param name="path">The global path to create the container at.</param>
        /// <returns>The created container.</returns>
        internal LuaDataContainer CreateContainerAt(string path)
        {
            // Creates the container.
            LuaTable native = _luaState.SafeCreateTable();
            LuaDataContainer ldc = CreateContainerCore(native, true);

            // Sets the global path.
            _luaState.SafeSetGlobal(path, native);

            // Returns the container.
            return ldc;
        }

        /// <summary>
        /// Gets a container for a Lua table.
        /// </summary>
        /// <param name="luaTable">The non-null Lua table to wrap.</param>
        /// <returns>A non-null data container.</returns>
        internal LuaDataContainer GetContainer(LuaTable luaTable)
        {
            return GetContainerCore(luaTable);
        }

		/// <summary>
		/// Gets the LuaDataContainer which has a particular object index.
		/// </summary>
		/// <param name="objIndex">The object index to look for. Must be 0 or greater.</param>
		/// <returns>The LuaDataContainer of the wherigo object with this index.</returns>
		/// <exception cref="ArgumentException"><paramref name="objIndex"/> is smaller than 0.</exception>
        internal LuaDataContainer GetContainer(int objIndex)
        {
            // Gets the Wherigo object for this index and performs sanity checks.
			WherigoObject wo = GetWherigoObjectCore(objIndex);

			// Returns the container.
			return (LuaDataContainer)wo.DataContainer;
        }

        /// <summary>
        /// Gets the Lua container at a specific global path.
        /// </summary>
        /// <param name="path">The global path, dot-separated, 
        /// where the container is.</param>
        /// <returns>The container at the path.</returns>
		/// <exception cref="InvalidOperationException">No container was found
		/// at the path.</exception>
        internal LuaDataContainer GetContainerAt(string path)
        {
            // Gets the lua table.
            LuaTable lt = _luaState.SafeGetGlobal<LuaTable>(path);

			// Checks that the container has been found.
			if (lt == null)
			{
				throw new InvalidOperationException(String.Format("The container at path {0} has not been found.", path));
			}

            // Returns the container or null.
            return GetContainerCore(lt);
        }

		/// <summary>
		/// Gets the native lua table for a Wherigo object.
		/// </summary>
		/// <param name="obj">WherigoObject to get the table of.</param>
		/// <returns>The lua table for the object.</returns>
		/// <exception cref="InvalidOperationException">The object has not
		/// been created by this factory, or has no data container.</exception>
        internal LuaTable GetNativeContainer(WherigoObject obj)
        {
			// Checks if the data container exists.
			if (obj.DataContainer == null)
			{
				throw new InvalidOperationException("The object has no data container.");
			}
			
			// Returns the native table.
			return GetNativeContainerCore(obj.DataContainer);
        }

        /// <summary>
        /// Gets a provider for a lua function.
        /// </summary>
        /// <param name="func">Function to wrap.</param>
        /// <param name="self">If non-null, the provider will pass the underlying
        /// lua table as a first parameter during execution.</param>
        /// <param name="protectFromGC">If true, the provider is protected from garbage
        /// collection.</param>
        /// <returns>The LuaDataProvider for the function.</returns>
        internal LuaDataProvider GetProvider(LuaFunction func, LuaDataContainer self = null, bool protectFromGC = false)
        {
            return CreateProviderCore(func, self == null ? null : GetNativeContainerCore(self), protectFromGC);
        }

        /// <summary>
        /// Gets the provider at a global path.
        /// </summary>
        /// <param name="path">Global path to query.</param>
        /// <returns>The provider at the path.</returns>
        /// <exception cref="InvalidOperationException">No provider was found
        /// at the path.</exception>
        internal LuaDataProvider GetProviderAt(string path)
        {
            // Gets the lua table.
            LuaFunction lf = _luaState.SafeGetGlobal<LuaFunction>(path);

            // Checks that the container has been found.
            if (lf == null)
            {
                throw new InvalidOperationException(String.Format("The provider at path {0} has not been found.", path));
            }

            // Returns the container or null.
            return CreateProviderCore(lf);
        }

        /// <summary>
        /// Gets the untyped WherigoObject that corresponds to a Wherigo
        /// entity in a Lua table.
        /// </summary>
        /// <param name="obj">LuaTable containing a Wherigo entity.</param>
        /// <returns>The WherigoObject corresponding to the table entity.
		/// Null is returned if and only if <paramref name="obj"/> is
		/// null.</returns>
        /// <exception cref="InvalidOperationException">The table does not
        /// contain a Wherigo entity.</exception>
        internal WherigoObject GetWherigoObject(LuaTable obj)
        {
            return GetWherigoObjectCore(obj, allowsNullTable: true);
        }

        /// <summary>
        /// Gets the WherigoObject of a certain type that corresponds to a
        /// Wherigo entity in a Lua table.
        /// </summary>
        /// <typeparam name="W">Type of the WherigoObject to get.</typeparam>
        /// <param name="luaTable">The table containing a Wherigo entity.</param>
        /// <returns>The typed object if it matches the entity in the table.
		/// Null is returned if and only if <paramref name="obj"/> is
		/// null.</returns>
        /// <exception cref="InvalidOperationException">The table does not
        /// contain a Wherigo entity, or it does but the entity is not of 
        /// type <typeparamref name="W"/>.</exception>
        internal W GetWherigoObject<W>(LuaTable luaTable) where W : WherigoObject
        {
            return GetWherigoObjectCore(luaTable, allowsNullTable: true, typeToCompare: typeof(W)) as W;
        }

        /// <summary>
        /// Loads and immediately runs the Wherigo Lua engine.
        /// </summary>
        internal void LoadAndRunEngine()
        {
            // Loads the engine bytecode.
            byte[] binChunk;
            using (BinaryReader bw = new BinaryReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("WF.Player.Core.Resources.Wherigo.luac")))
            {
                binChunk = bw.ReadBytes((int)bw.BaseStream.Length);
            }

            // Runs the bytecode.
            _luaState.SafeDoString(binChunk, "Wherigo Engine");
        }

        /// <summary>
        /// Loads a data provider from lua bytecode.
        /// </summary>
        /// <param name="luaBytes">The lua bytecode to load.</param>
        /// <param name="name">Name of the bytecode chunk.</param>
        /// <returns>A LuaDataProvider that contains the loader provider.</returns>
        internal LuaDataProvider LoadProvider(byte[] luaBytes, string name)
        {
            // Loads the lua function.
            LuaFunction lf = _luaState.SafeLoadString(luaBytes, name);

            // Wraps it and returns the provider.
            return CreateProviderCore(lf);
        }

		/// <summary>
        /// Executes a Lua script.
        /// </summary>
        /// <param name="luaScript">The script to execute.</param>
        internal void RunScript(string luaScript)
        {
            _luaState.SafeDoString(luaScript);
        }

        /// <summary>
        /// Sets a global path with the native table associated with a
        /// container.
        /// </summary>
        /// <param name="path">Path in the global table to set.</param>
        /// <param name="container">Container associated to the Lua table to set.</param>
        internal void SetContainerAt(string path, LuaDataContainer container)
        {
            // Gets the LuaTable for the container.
            LuaTable lt = GetNativeContainerCore(container);

            // Sets the global path to this table.
            _luaState.SafeSetGlobal(path, lt);
        }

        /// <summary>
        /// Gets a .Net value from a native Lua value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
		/// <param name="protectFromGC">If true and the value to convert is a table of function,
		/// protects the wrapper from being garbage collected.</param>
        /// <returns>An object of type double, string, bool, LuaDataContainer,
        /// LuaDataProvider or null if the value was lua's nil.</returns>
        internal object GetValueFromNativeValue(LuaValue value, bool protectFromGC = false)
        {
            // Gets the .Net value.
            object o = _luaState.SafeDewrap(value);

            // Wraps the objects if they are still Lua types.
            if (o is LuaTable)
            {
                o = GetContainerCore((LuaTable)o, protectFromGC);
            }
            else if (o is LuaFunction)
            {
                o = CreateProviderCore((LuaFunction)o, protectFromGC: protectFromGC);
            }

            return o;
        }

        /// <summary>
        /// Gets a native Lua value from a .Net value or a container or provider.
        /// </summary>
        /// <param name="value">The value to convert. It is either one of the
        /// values supported by Lua, or a LuaDataContainer or LuaDataProvider.</param>
        /// <returns>A natively supported Lua value.</returns>
        internal LuaValue GetNativeValueFromValue(object value)
        {            
            // Unwraps the data layer types and other special types.
            if (value is WherigoObject)
            {
                value = GetNativeContainerCore(((WherigoObject)value).DataContainer);
            }
            else if (value is LuaDataContainer)
            {
                value = GetNativeContainerCore((LuaDataContainer)value);
            }
            else if (value is LuaDataProvider)
            {
                value = GetNativeProviderCore((LuaDataProvider)value);
            }
            else if (value is DistanceUnit)
            {
                // Distance units are converted to a value that the Wherigo
                // Engine is capable of understanding: the unit value.
                value = ((DistanceUnit)value).ToSymbol();
            }
            else if (value != null && value.GetType().IsEnum)
            {
                // Enums are converted to strings.
                value = value.ToString();
            }

            // Returns the native lua value.
            return _luaState.SafeWrap(value);
        }

        /// <summary>
        /// Gets a Wherigo object located at a global path.
        /// </summary>
        /// <typeparam name="W">Type of the Wherigo object to get.</typeparam>
        /// <param name="path">Global path to the table of the object.</param>
        /// <returns>The Wherigo object.</returns>
        /// <exception cref="InvalidCastException">The Wherigo object is not of the
        /// expected type.</exception>
        internal W GetWherigoObjectAt<W>(string path) where W : WherigoObject
        {
            // Gets the table at the global path.
            LuaTable lt = _luaState.SafeGetGlobal<LuaTable>(path);

			// Checks if the table exists.
			if (lt == null)
			{
				throw new InvalidOperationException(String.Format("The table at {0} does not exist.", path));
			}

            // Returns the object.
			return GetWherigoObjectCore(lt, typeToCompare: typeof(W)) as W;
        }

		/// <summary>
		/// Gets a list of Wherigo objects of a certain type from a list of
		/// Wherigo entities in a Lua table.
		/// </summary>
		/// <typeparam name="W">The type of Wherigo objects to get.</typeparam>
		/// <param name="luaTable">The table containing entities.</param>
		/// <returns>A list of entities of type <typeparamref name="W"/>, eventually
		/// empty.</returns>
        internal WherigoCollection<W> GetWherigoObjectList<W>(LuaTable luaTable) where W : WherigoObject
        {
			// Creates an empty list to store the objects.
			List<W> list = new List<W>();

			// Enumerates through all values of the lua table that are lua tables.
			// For each of them, tries to convert to a W.
			var e = _luaState.SafeGetEnumerator(luaTable);
			while (e.MoveNext())
			{
				if (e.Value is LuaTable)
				{
					// Tries to get a wherigo object for this entity.
					WherigoObject wo = GetWherigoObjectCore((LuaTable)e.Value, dontFailIfNotWigEntity: true);

					// If the object is a W, adds it to the list.
					if (wo != null && wo is W)
					{
						list.Add((W)wo);
					}
				}
			}

			// Returns the list.
			return new WherigoCollection<W>(list);
        }

        #endregion 

        #region IDataFactory

        public W CreateWherigoObject<W>(params object[] arguments) where W : WherigoObject
        {
            // Gets the classname for the needed type.
			string classname = GetWherigoClassname(typeof(W));

			// Creates the object.
			WherigoObject wo = CreateWherigoObjectCore(classname, typeof(W), arguments);

			// Returns the object.
			return (W)wo;
        }

        public WherigoObject CreateWherigoObject(string wClassname, params object[] arguments)
        {
			// Creates the object.
			return CreateWherigoObjectCore(wClassname, null, arguments);
        }

        IDataContainer IDataFactory.GetContainer(int objIndex)
        {
			return GetContainer(objIndex);
        }

        public W GetWherigoObject<W>(IDataContainer data) where W : WherigoObject
        {
            return GetWherigoObjectCore(GetNativeContainerCore(data), typeToCompare: typeof(W)) as W;
        }

        public W GetWherigoObject<W>(int objIndex) where W : WherigoObject
        {
            // Gets the object.
			WherigoObject wo = GetWherigoObjectCore(objIndex, dontFailIfBadArg: true);

			// Checks if the wherigo object is of the right type.
			if (wo != null && !(wo is W))
			{
				throw new InvalidOperationException(String.Format("The object with index {0} is known to be of type {1}, not {2} as requested.", objIndex, wo.GetType().FullName, typeof(W).FullName));
			}

			// Returns the object, cast to the proper type.
			return (W)wo;
        }

        public WherigoCollection<W> GetWherigoObjectList<W>(IDataContainer data) where W : WherigoObject
        {
            // Gets the list of non-null W objects from this list.
            List<W> wlist = new List<W>();
            if (data != null)
            {
                wlist.AddRange(data
                .OfType<LuaDataContainer>()
                .Select(ldc => GetWherigoObjectCore(GetNativeContainerCore(ldc), dontFailIfNotWigEntity: true))
                .Where(wo => wo is W)
                .Cast<W>()
                .ToList());
            }

            // Returns a collection for the list.
            return new WherigoCollection<W>(wlist);
        }

        #endregion

        #region Management of Containers, Providers and WherigoObjects
        private FriendLuaDataContainer CreateContainerCore(LuaTable native, bool protectFromGC = false)
        {
            // Gets a protected version of the table if needed.
            if (protectFromGC)
            {
                native = _luaState.SafeProtectTableFromGC(native);
            }

            // Creates a new container.
            FriendLuaDataContainer ldc = new FriendLuaDataContainer(native, _luaState, this);

            // Returns the container.
            return ldc;
        }

        private FriendLuaDataProvider CreateProviderCore(LuaFunction native, LuaTable self = null, bool protectFromGC = false)
        {
            // Protects the function from garbage collection if needed.
            if (protectFromGC)
            {
                native = _luaState.SafeProtectFunctionFromGC(native);
            }

            // Creates a new provider.
            FriendLuaDataProvider ldp = new FriendLuaDataProvider(native, _luaState, this, self);

            // Returns the provider.
            return ldp;
        }

        private LuaDataContainer GetContainerCore(LuaTable obj, bool protectFromGC = false)
        {
            // Does this have a ClassName?
            // Yes -> Returns the Wherigo object's data container.
            //        (This allows the registration process to take place.)
            // No -> Creates and returns the container.

            LuaDataContainer ret;

            string cn = _luaState.SafeGetField<string>(obj, "ClassName");
            if (cn != null)
            {
                ret = (LuaDataContainer)GetWherigoObjectCore(obj, forceProtectFromGC: protectFromGC).DataContainer;
            }
            else
            {
                ret = CreateContainerCore(obj, protectFromGC);
            }

            return ret;
        }

        private LuaTable GetNativeContainerCore(IDataContainer container)
        {
            // Makes the container friendly :)
            FriendLuaDataContainer fc = container as FriendLuaDataContainer;

            // Sanity check.
            if (fc == null)
            {
                throw new InvalidOperationException("The container has not been created by this instance of LuaDataFactory.");
            }

            // Returns the container.
            return fc.Table;
        }

        private LuaFunction GetNativeProviderCore(IDataProvider provider)
        {
            // Makes the container friendly :)
            FriendLuaDataProvider fd = provider as FriendLuaDataProvider;

            // Sanity check.
            if (fd == null)
            {
                throw new InvalidOperationException("The provider has not been created by this instance of LuaDataFactory.");
            }

            // Returns the container.
            return fd.Function;
        }

        private WherigoObject GetWherigoObjectCore(
            LuaTable obj,
            bool dontFailIfNotWigEntity = false,
            bool forceProtectFromGC = false,
            bool allowsNullTable = false,
            Type typeToCompare = null)
        {
            // Sanity check.
            if (obj == null)
            {
                if (!allowsNullTable)
                {
                    throw new ArgumentNullException("Null table argument is not allowed.");
                }

                return null;
            }

            // Gets the class of the entity.
            string cn = _luaState.SafeGetField<string>(obj, "ClassName");
            if (cn == null)
            {
                if (dontFailIfNotWigEntity)
                {
                    return null;
                }

                throw new InvalidOperationException("obj has no ClassName string property.");
            }

            // Does the entity have an ObjIndex?
            // YES -> it should be in the AllZObjects table, so retrieve or make it.
            // NO -> make it anyway.
            WherigoObject ret = null;
            double? oiRaw = _luaState.SafeGetField<double?>(obj, "ObjIndex");
            if (oiRaw == null)
            {
                // Creates a container for the table.
                // It is not protected from GC by default.
                LuaDataContainer ldc = CreateContainerCore(obj, forceProtectFromGC);

                // Immediately wraps the table into its corresponding class.
                if ("ZonePoint" == cn)
                    ret = new ZonePoint(ldc);

                else if ("ZCommand" == cn || "ZReciprocalCommand" == cn)
                    ret = new Command(
                        ldc,
                        MakeCommandCalcTargetObjectsInstance(ldc),
                        MakeCommandExecuteCommandInstance(ldc)
                        );

                else if ("Distance" == cn)
                    ret = new Distance(ldc);
            }
            else
            {
                // Tries to get the object from the cache if it is not
                // the player or cartridge object.
                int oi = (int)oiRaw.Value;
                bool isPlayer = oi < 0;
                bool isCartridge = oi == 0;
                Node node;
                if (!isPlayer && !isCartridge)
                {
                    bool hasValue;
                    lock (_syncRoot)
                    {
                        hasValue = _wEntities.TryGetValue(oi, out node);
                    }
                    // The object is known, returns it.
                    if (hasValue)
                    {
                        ret = node.Object;

                        // Double check the classname.
                        string cachedCn = ret.DataContainer.GetString("ClassName");
                        if (cn != cachedCn)
                        {
                            throw new InvalidOperationException(String.Format("The object with id {0} is known to have class {1}, but class {2} was requested.", oi, cachedCn ?? "<null>", cn ?? "<null>"));
                        }
                    }
                }

                // The object is not known, make it and create a node for it.
                if (ret == null)
                {
                    // Creates a GC-protected container for the table.
                    FriendLuaDataContainer ldc = CreateContainerCore(obj, true);

                    // Creates the object.
                    if ("ZInput" == cn)
                        ret = new Input(
                            ldc,
                            MakeInputRunOnGetInputInstance(ldc)
                            );

                    else if ("ZTimer" == cn)
                        ret = new Timer(ldc);

                    else if ("ZCharacter" == cn)
                    {
                        if (isPlayer && _helper.Player != null)
                        {
                            ret = _helper.Player;
                        }
                        else
                        {
                            ret = new Character(
                                ldc,
                                MakeUIObjectRunOnClickInstance(ldc)
                                );
                        }
                    }
                    else if ("ZItem" == cn)
                        ret = new Item(
                            ldc,
                            MakeUIObjectRunOnClickInstance(ldc)
                            );

                    else if ("ZTask" == cn)
                        ret = new Task(
                            ldc,
                            MakeUIObjectRunOnClickInstance(ldc)
                            );

                    else if ("Zone" == cn)
                        ret = new Zone(
                            ldc,
                            MakeUIObjectRunOnClickInstance(ldc)
                            );
                    else if ("ZMedia" == cn)
                    {
                        // Gets the ZMedia from the Cartridge which has the same Id.
                        Media media = _helper.Cartridge.Resources.Single(m => m.MediaId == oi);

                        // Injects the data container with metadata about the media.
                        media.DataContainer = ldc;

                        // The returned object is the media.
                        ret = media;
                    }
                    else if ("ZCartridge" == cn)
                    {
                        // Sanity checks if the Cartridge GUIDs match.
                        string baseId = _helper.Cartridge.Guid;
                        string reqId = ldc.GetString("Id");
                        if (baseId != reqId)
                        {
                            //throw new InvalidOperationException(String.Format("Requested Cartridge with id {0}, but only knows Cartridge with id {1}.", reqId, baseId));
                            System.Diagnostics.Debug.WriteLine("LuaDataFactory: WARNING: " + String.Format("Requested Cartridge with id {0}, but only knows Cartridge with id {1}.", reqId, baseId));
                        }

                        // Returns the cartridge object.
                        ret = _helper.Cartridge;

                        // Binds the cartridge container if the cartridge is unbound.
                        if (ret.DataContainer == null)
                        {
                            ret.DataContainer = ldc;
                        }
                    }
                    else
                        throw new InvalidOperationException("obj has an unknown classname: " + cn);

                    // Creates a node and registers it. Cartridge and player are not registered.
                    if (!isPlayer && !isCartridge)
                    {
                        node = new Node()
                        {
                            Container = ldc,
                            Object = ret
                        };
                        lock (_syncRoot)
                        {
                            _wEntities.Add(oi, node);
                        }
                    }
                }
            }

            // Final sanity checks.
            if (ret == null)
            {
                throw new InvalidOperationException("Returned value was not computed.");
            }
            if (typeToCompare != null && !typeToCompare.IsAssignableFrom(ret.GetType()))
            {
                throw new InvalidOperationException(String.Format("The wherigo object is known to have type {0}, not {1} as requested.", ret.GetType().FullName, typeToCompare.FullName));
            }

            return ret;
        }

        private WherigoObject GetWherigoObjectCore(int objIndex, bool dontFailIfBadArg = false, bool dontFailIfNotFound = false)
        {
            // Sanity check: indexes that are less than 1 are invalid and
            // return null.
            if (objIndex < 0)
            {
                if (dontFailIfBadArg)
                {
                    System.Diagnostics.Debug.WriteLine(String.Format("LuaDataFactory: WARNING: Requested WherigoObject with invalid ObjIndex {0}, returned null.", objIndex));
                    return null;
                }

                throw new ArgumentException("Object index must be 0 or greater, got " + objIndex);
            }

            // Gets the registered wherigo object with the index.
            // If not found, try to get it from the AllZObjects table.
            WherigoObject wo;
            Node node;
            bool hasValue;
            lock (_syncRoot)
            {
                hasValue = _wEntities.TryGetValue(objIndex, out node);
            }
            if (hasValue)
            {
                // The object is found: give it back.
                wo = node.Object;
            }
            else
            {
                // The object isn't found: look in AllZObjects.
                if (!_helper.Cartridge.IsBound)
                {
                    throw new InvalidOperationException("Cannot look up in AllZObjects, Cartridge is unbound.");
                }

                // Gets the AllZObjects table and queries for the field at objIndex.
                LuaTable allZObjs = GetNativeContainerCore(_helper.Cartridge.DataContainer);
                LuaTable zObj = _luaState.SafeGetField<LuaTable>(allZObjs, objIndex);

                if (zObj == null)
                {
                    string message = String.Format("The object with key {0} was not found in AllZObjects.", objIndex);

                    if (dontFailIfNotFound)
                    {
                        System.Diagnostics.Debug.WriteLine("LuaDataFactory: WARNING: " + message);
                        return null;
                    }

                    throw new KeyNotFoundException(message);
                }

                // The object exists, converts it to a WherigoObject.
                wo = GetWherigoObjectCore(zObj);
            }

            return wo;
        }

        private WherigoObject CreateWherigoObjectCore(string classname, Type typeToCompare, object[] arguments)
        {            
            // Gets the table for the class to get.
            LuaTable classLt = _luaState.SafeGetGlobal<LuaTable>("Wherigo." + classname);

            // Gets the newInstance function of the class.
            string newInstanceFuncName = "Wherigo.newInstance";
            LuaFunction newInstanceLf = _luaState.SafeGetGlobal<LuaFunction>(newInstanceFuncName);

            // Checks that the function is valid.
            if (newInstanceLf == null)
            {
                throw new InvalidOperationException("No " + newInstanceFuncName + " function could be found.");
            }

            // Conforms the arguments.
            List<LuaValue> conformedArguments = new List<LuaValue>();
            conformedArguments.Add(classLt); // Self comes first.
            if (IsWherigoClassnameZObject(classname))
            {
                // The ZObject constructor requires the current cartridge object as only parameter.
                // Therefore, let's discard the arguments and only supply the cartridge table.
                conformedArguments.Add(GetNativeContainerCore(_helper.Cartridge.DataContainer));
            }
            else
            {
                // Non-ZObject constructors take their parameters in a row.
                // All supplied arguments are wrapped.
                arguments = arguments ?? new object[] { };
                conformedArguments.AddRange(arguments.Select(o => GetNativeValueFromValue(o)));
            }

            // Calls the function to create a new instance.
            LuaTable wlt;
            try
            {
                IList<object> ret = _luaState.SafeCallRaw(newInstanceLf, conformedArguments.ToArray());
                wlt = ret.FirstOrDefault() as LuaTable;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("An exception occured while constructing an instance of " + classname, e);
            }

            // Checks if the object is valid.
            if (wlt == null)
            {
                throw new InvalidOperationException("An object of class " + classname + " could not be constructed.");
            }
            
            // Returns the wrapped wherigo object.
            //System.Diagnostics.Debug.WriteLine("LuaDataFactory: Creating an instance of " + classname);
            return GetWherigoObjectCore(wlt, typeToCompare: typeToCompare);
        }

        private bool IsWherigoClassnameZObject(string classname)
        {
            return "ZCartridge".Equals(classname) || "Zone".Equals(classname)
                || "ZCharacter".Equals(classname) || "ZItem".Equals(classname)
                || "ZTask".Equals(classname) || "ZInput".Equals(classname)
                || "ZMedia".Equals(classname) || "ZTimer".Equals(classname)
                //|| "ZCommand".Equals(classname) || "ZReciprocalCommand".Equals(classname)
                || "ZObject".Equals(classname);
        }

        private string GetWherigoClassname(Type type)
        {
            // Gets the short name of the type.
            string name = type.Name;

            // First, gets rid of the classnames that are the same.
            if ("ZonePoint".Equals(name) || "Distance".Equals(name) || "Zone".Equals(name))
            {
                return name;
            }

            // Second, checks for classnames that are Z-prefixed.
            if ("Command".Equals(name) || "Input".Equals(name) ||
                "Timer".Equals(name) || "Character".Equals(name) ||
                "Item".Equals(name) || "Task".Equals(name) ||
                "Media".Equals(name) || "Cartridge".Equals(name))
            {
                return "Z" + name;
            }

            // ZReciprocalCommands do not have equivalents in the C# model,
            // therefore they are not mentionned here.

            // Finally, WherigoObject should give ZObject as a classname.
            if (type == typeof(WherigoObject))
            {
                return "ZObject";
            }

            // Other types are not supported.
            throw new InvalidOperationException(String.Format("Type {0} is not a Wherigo type.", type.FullName));

        } 
        #endregion

        #region Data Model Delegates Implementation
        private UIObject.RunOnClick MakeUIObjectRunOnClickInstance(LuaDataContainer ldc)
        {
            return new UIObject.RunOnClick(() => _helper.LuaExecutionQueue.BeginCallSelf(ldc, "OnClick"));
        }

        private Input.RunOnGetInput MakeInputRunOnGetInputInstance(LuaDataContainer ldc)
        {
			// Defines an action that can queue giving a result to the relevant input entity.
			Action<string, ExecutionQueue.FallbackAction> onGetInput = new Action<string,ExecutionQueue.FallbackAction>(
				(s, fa) => _helper.LuaExecutionQueue.BeginCallSelf(ldc, "OnGetInput", fa, s)
					);

			// Defines a function that can create a fallback action in case giving a null
			// result to the relevant input entity raises an exception.
			// In that case, another try is done using an empty string instead.
			// No fallback action is given if the input answer is not null.
			Func<string, ExecutionQueue.FallbackAction> getFallback = new Func<string,ExecutionQueue.FallbackAction>(
				s => {
					if (s == null) 
					{
						return new ExecutionQueue.FallbackAction(
							ex => onGetInput("", null)
						);
					} 
					else
					{
						return null;
					}
				});

			// Returns an action that queues giving a result to this input, allowing
			// to do it again if the first attempt has been made with a null string
			// and failed.
			return new Input.RunOnGetInput(s => onGetInput(s, getFallback(s)));
        }

        private Command.ExecuteCommand MakeCommandExecuteCommandInstance(LuaDataContainer ldc)
        {
            return new Command.ExecuteCommand(t => _helper.LuaExecutionQueue.BeginCallSelf(ldc, "exec", t));
        }

        private Command.CalcTargetObjects MakeCommandCalcTargetObjectsInstance(LuaDataContainer ldc)
        {
            return new Command.CalcTargetObjects(() => ldc.GetWherigoObjectListFromProvider<Thing>(
                "CalcTargetObjects",
                _helper.Cartridge,
                _helper.Player));
        } 
        #endregion
    }
}
