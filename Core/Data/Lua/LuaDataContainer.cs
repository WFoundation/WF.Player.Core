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
using System.Collections;
using System.Collections.Generic;
using Eluant;
using System.Linq;

namespace WF.Player.Core.Data.Lua
{
	/// <summary>
	/// A Lua implementation of a data container wrapping a Lua table.
	/// </summary>
    internal class LuaDataContainer : IDataContainer
	{
        #region Nested Classes

        private class Enumerator : IEnumerator, IDictionaryEnumerator
        {
            private LuaDataContainer _parent;
            private IDictionaryEnumerator _baseEnumerator;

            private DictionaryEntry _current;
            private bool _isCurrentValid;

            internal Enumerator(LuaDataContainer luaDataContainer)
            {
                this._parent = luaDataContainer;
                _baseEnumerator = _parent._luaState.SafeGetEnumerator(_parent._selfLuaTable);
            }

            #region IEnumerator
            public object Current
            {
                get 
                {
                    CheckValid();

                    return _current.Value;
                }
            }

            public bool MoveNext()
            {
                if (!_baseEnumerator.MoveNext())
                {
                    _isCurrentValid = false;
                    return false;
                }

                // Updates the current values.
                object key = UnwrapOrNull(_baseEnumerator.Key);
                if (key == null)
                {
                    throw new InvalidOperationException("Enumerated key should not be null.");
                }
                object val = UnwrapOrNull(_baseEnumerator.Value);
                _current = new DictionaryEntry(key, val);
                _isCurrentValid = true;

                return true;
            }

            public void Reset()
            {
                _baseEnumerator.Reset();
                _isCurrentValid = false;
            } 
            #endregion

            #region IDictionaryEnumerator
            public DictionaryEntry Entry
            {
                get 
                {
                    CheckValid();
                    
                    return _current; 
                }
            }

            public object Key
            {
                get 
                {
                    CheckValid();

                    return _current.Key; 
                }
            }

            public object Value
            {
                get 
                {
                    CheckValid();

                    return _current.Value; 
                }
            } 
            #endregion

            private object UnwrapOrNull(object nativeObject)
            {
                return nativeObject is LuaValue 
                    ? _parent._dataFactory.GetValueFromNativeValue((LuaValue)nativeObject)
                    : nativeObject;
            }

            private void CheckValid()
            {
                if (!_isCurrentValid)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        #endregion

        #region Fields

        private LuaDataFactory _dataFactory;
        private SafeLua _luaState;
        protected LuaTable _luaTable;
		private LuaTable _selfLuaTable; 

        #endregion

        #region Indexers

        /// <summary>
        /// Sets a field in the inner Lua table.
        /// </summary>
        /// <param name="key">A double, string or bool value.</param>
        /// <returns>A double, string, bool, LuaDataContainer or LuaDataProvider.</returns>
        internal object this[object key]
        {
            set
            {
                // Dewraps the value if its a container or provider.
                var v = _dataFactory.GetNativeValueFromValue(value);

                // Sets the field.
                _luaState.SafeSetField(_luaTable, key, v);
            }
        } 

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new LuaDataContainer wrapping a native lua table
        /// and using a safe lua state.
        /// </summary>
        /// <param name="table">Lua table to wrap.</param>
        /// <param name="luaState">Safe lua state to access the table.</param>
        /// <param name="factory"></param>
        internal LuaDataContainer(LuaTable table, SafeLua luaState, LuaDataFactory factory)
        {
            _luaState = luaState;
            _luaTable = table;
            _dataFactory = factory;

			// If the table is a proxy, the inner table needs to be recovered
			// in order to enable enumerating, because proxy tables handled by 
			// the Wherigo Lua engine cannot be enumerated.
			_selfLuaTable = _luaState.SafeGetFieldInMetatable<LuaTable>(_luaTable, "_self") 
				?? _luaTable;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a lua function for a delegate and binds a field of this container
        /// with the function.
        /// </summary>
        /// <param name="key">Key of the field to bind.</param>
        /// <param name="d">.Net delegate to bind the field to.</param>
        internal void BindWithFunction(string key, Delegate d)
        {
            // Creates a LuaFunction for the delegate.
            LuaFunction lf = _luaState.SafeProtectFunctionFromGC(_luaState.SafeCreateFunction(d));

            // Sets the field with the function.
            _luaState.SafeSetField(_luaTable, key, lf);
        }

		/// <summary>
		/// Calls a function from this container for a particular key and 
		/// with particular arguments.
		/// </summary>
		/// <param name="key">The key to find the function at.</param>
		/// <param name="parameters">Optional parameters to pass to the function.
		/// The native lua table will be added as first parameter before calling
		/// the function.</param>
		/// <returns>The first data container that is returned by the function,
		/// or null if none were.</returns>
        internal LuaDataContainer CallSelf(string key, params object[] parameters)
        {
             // Gets the function at the key.
			LuaFunction lf = _luaState.SafeGetField<LuaFunction>(_luaTable, key);

			// Checks if the function exists.
			if (lf == null)
			{
				throw new InvalidOperationException("The function " + key + " does not exist.");
			}

			// Gets a provider for the function and calls it.
			LuaDataProvider provider = _dataFactory.GetProvider(lf, this, false);
			return provider.FirstContainerOrDefault(parameters);
        }

        /// <summary>
        /// Gets the provider for a key.
        /// </summary>
        /// <param name="key">Key in this container.</param>
        /// <param name="isSelf">True to specify that the provider is a 
        /// self-provider that should execute by passing this data
        /// container as a first parameter.</param>
        /// <returns>The data provider, or null if it was not found.</returns>
        internal LuaDataProvider GetProvider(string key, bool isSelf)
        {
            // Gets the lua function.
            LuaFunction lf = _luaState.SafeGetField<LuaFunction>(_luaTable, key);

            // Returns the provider.
            return lf == null ? null : _dataFactory.GetProvider(lf, isSelf ? this : null, false);
        }

        /// <summary>
        /// Gets the container for a key.
        /// </summary>
        /// <param name="key">Key in this container.</param>
        /// <returns>The container, or null if it was not found.</returns>
        internal LuaDataContainer GetContainer(string key)
        {
            // Gets the lua table.
            LuaTable lt = _luaState.SafeGetField<LuaTable>(_luaTable, key);

            // Returns the container.
            return lt == null ? null : _dataFactory.GetContainer(lt);
        }

        /// <summary>
        /// Gets the container for a key.
        /// </summary>
        /// <param name="key">Key in this container.</param>
        /// <returns></returns>
        internal LuaDataContainer GetContainer(int key)
        {
            // Gets the lua table.
            LuaTable lt = _luaState.SafeGetField<LuaTable>(_luaTable, key);

            // Returns the container.
            return lt == null ? null : _dataFactory.GetContainer(lt);
        }

        /// <summary>
        /// Gets an enumerator of this container that can enumerate
        /// through keys and values.
        /// </summary>
        /// <returns></returns>
        internal IDictionaryEnumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion

        #region IDataContainer
        public int Count
        {
            get { return _luaState.SafeCount(_selfLuaTable); }
        }

        public bool? GetBool(string key)
        {
            return _luaState.SafeGetField<bool?>(_luaTable, key);
        }

        IDataContainer IDataContainer.GetContainer(string key)
        {
            return this.GetContainer(key);
        }

        public double? GetDouble(string key)
        {
            return _luaState.SafeGetField<double?>(_luaTable, key);
        }

        public E? GetEnum<E>(string key, E? defaultValue = null) where E : struct
        {
            // Gets the string value for the key.
            string field = _luaState.SafeGetField<string>(_luaTable, key);

            // Returns the enum value or default.
            E? enumValue;
            Utils.Utils.TryParseEnum<E>(field, defaultValue, out enumValue);
            return enumValue;
        }

        public int? GetInt(string key)
        {
            // Gets a double number.
            double? d = _luaState.SafeGetField<double?>(_luaTable, key);

            // Returns null or the converted int.
            return d.HasValue ? Convert.ToInt32(d.Value) : new Nullable<int>();
        }

        public IEnumerable<T> GetList<T>(string key)
        {
            // Gets the table for the key.
            LuaTable lt = _luaState.SafeGetField<LuaTable>(_luaTable, key);

            // Returns default right away if the table wasn't found.
            if (lt == null)
            {
                return new List<T>();
            }

            // Returns the list.
            return _luaState.SafeGetList<T>(lt);
        }

        public IEnumerable<T> GetListFromProvider<T>(string key, params object[] parameters)
        {
            // Gets the provider for the key.
            LuaFunction lf = _luaState.SafeGetField<LuaFunction>(_luaTable, key);

            // Returns default right away if the function wasn't found.
            if (lf == null)
                return new List<T>();

            // Gets the provider and its first inner list.
            IDataContainer dc = _dataFactory.GetProvider(lf).FirstContainerOrDefault(parameters);

            // Returns default right away if the container wasn't found.
            if (dc == null)
                return new List<T>();

            // Makes and returns the list.
            return dc.OfType<T>().ToList();
        }

        public IDataProvider GetProvider(string key)
        {
            // Gets the lua function.
            LuaFunction lf = _luaState.SafeGetField<LuaFunction>(_luaTable, key);

            // Returns the self-provider.
            return lf == null ? null : _dataFactory.GetProvider(lf, self: this);
        }

        public string GetString(string key)
        {
			return _luaState.SafeGetField<string>(_luaTable, key);
        }

		public byte[] GetByteArray(object key)
		{
			return _luaState.SafeGetField<byte[]>(_luaTable, key);
		}

        public W GetWherigoObject<W>(string key) where W : WherigoObject
        {
            // Gets the table for the key.
            LuaTable lt = _luaState.SafeGetField<LuaTable>(_luaTable, key);

            // Returns right away if the table is null.
            if (lt == null)
            {
                return null;
            }

            // Returns the wherigo object, or null.
            try
            {
                return _dataFactory.GetWherigoObject<W>(lt);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public WherigoCollection<W> GetWherigoObjectList<W>(string key) where W : WherigoObject
        {
            // Gets the table for the key.
            LuaTable lt = _luaState.SafeGetField<LuaTable>(_luaTable, key);

            // Returns right away if the table is null.
            if (lt == null)
            {
                return new WherigoCollection<W>();
            }

            // Returns the list of entities.
            return _dataFactory.GetWherigoObjectList<W>(lt);
        }

        public WherigoCollection<W> GetWherigoObjectListFromProvider<W>(string key, params object[] parameters) where W : WherigoObject
        {
            // Gets the provider at the key.
            LuaFunction lf = _luaState.SafeGetField<LuaFunction>(_luaTable, key);
            if (lf == null)
            {
                return new WherigoCollection<W>();
            }

            // Gets the self, unprotected, provider.
            LuaDataProvider ldp = _dataFactory.GetProvider(lf, this, false);

            // Executes and gets the first container.
            LuaDataContainer ldc = ldp.FirstContainerOrDefault(parameters);

            // Gets the list for this 
            return _dataFactory.GetWherigoObjectList<W>(ldc);

        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        } 
        #endregion
    }
}
