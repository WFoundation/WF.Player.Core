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
using System.Collections;
using Eluant;
using WF.Player.Core.Utils;
using System.Linq;

namespace WF.Player.Core.Data.Lua
{
	/// <summary>
	/// A wrapper around a Lua state that provides with various thread-safe utilities.
	/// </summary>
	internal class SafeLua : IDisposable
	{
		#region Nested Classes

		/// <summary>
		/// A thread-safe wrapper around a IDictionaryEnumerator bound to a lua state.
		/// </summary>
        private class SafeDictionaryEnumerator : IDictionaryEnumerator
		{
            #region Fields
            private IEnumerator<KeyValuePair<LuaValue, LuaValue>> _baseEnumerator;
            private SafeLua _parent;
            private LuaRuntime _luaState;
            #endregion

			public SafeDictionaryEnumerator(
                IEnumerator<KeyValuePair<LuaValue, LuaValue>> e,
                SafeLua parent)
			{
				this._baseEnumerator = e;
                this._parent = parent;
				this._luaState = _parent._luaState;
			}

			public DictionaryEntry Entry
			{
				get
				{
					lock (_luaState)
					{
						KeyValuePair<LuaValue,LuaValue> kv = (KeyValuePair<LuaValue,LuaValue>) _baseEnumerator.Current;
						var k = Dewrap(kv.Key);
						var v= Dewrap(kv.Value);

                        return new DictionaryEntry(k, v);
					}
				}
			}

			public object Key
			{
				get
				{
					lock (_luaState)
					{
                        LuaValue key = _baseEnumerator.Current.Key;

                        return Dewrap(key);
					}
				}
			}

            public object Value
			{
				get
				{
					lock (_luaState)
					{
                        LuaValue val = _baseEnumerator.Current.Value;

                        return Dewrap(val);
					}
				}
			}

            object IEnumerator.Current
			{
				get 
				{
					lock (_luaState)
					{
                        KeyValuePair<LuaValue, LuaValue> current = _baseEnumerator.Current;

                        return new KeyValuePair<object, object>(
                            Dewrap(current.Key),
                            Dewrap(current.Value)
                            );
					}
				}
			}

			public bool MoveNext()
			{
				lock (_luaState)
				{
					return _baseEnumerator.MoveNext();
				}
			}

			public void Reset()
			{
				lock (_luaState)
				{
					_baseEnumerator.Reset();
				}
			}

            private object Dewrap(LuaValue luaValue)
            {
                return _parent.Dewrap(luaValue);
            }
		}

		#endregion
		
		#region Fields

		/// <summary>
		/// The underlying lua state, used both for lua operation and for locking lua operations.
		/// </summary>
		private LuaRuntime _luaState;

		/// <summary>
		/// A sync root used for locking on this instance's internal operations.
		/// </summary>
		private object _syncRoot = new object();

		private bool _rethrowsExceptions = true;
		private bool _rethrowsDisposedLuaExceptions = true;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs a SafeLua using a new underlying lua state.
		/// </summary>
		public SafeLua()
		{
			_luaState = new LuaRuntime();
		}

		/// <summary>
		/// Constructs a thread-safe wrapper around a lua state.
		/// </summary>
		/// <remarks>Warning: this class cannot provide full thread-safety if the
		/// lua state <paramref name="lua"/> is used by other classes.</remarks>
		/// <param name="lua"></param>
		public SafeLua(LuaRuntime lua)
		{
			_luaState = lua;
		} 

		#endregion

		#region Disposers

		~SafeLua()
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
				// Bye bye lua state.
				if (_luaState != null)
				{
					lock (_luaState)
					{
						_luaState.Dispose();
					}
				}
			}
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets if the methods of this SafeLua rethrow exceptions when they occur,
		/// or if they return default values instead.
		/// </summary>
		public bool RethrowsExceptions
		{
			get
			{
				lock (_syncRoot)
				{
					return _rethrowsExceptions;
				}
			}

			set
			{
				lock (_syncRoot)
				{
					_rethrowsExceptions = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets if when rethrowing exceptions, this instance should
		/// also rethrow exceptions that are due to a disposed lua state.
		/// </summary>
		public bool RethrowsDisposedLuaExceptions
		{
			get
			{
				lock (_syncRoot)
				{
					return _rethrowsDisposedLuaExceptions;
				}
			}

			set
			{
				lock (_syncRoot)
				{
					_rethrowsDisposedLuaExceptions = value;
				}
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Gets a list of objects from the contents of a table.
		/// </summary>
		/// <typeparam name="T">Type of the entities to get from the table.</typeparam>
		/// <param name="table">The table to convert.</param>
		/// <returns>A list of all the entities from the table that could be converted
		/// to the type <typeparamref name="T"/>.</returns>
		public List<T> SafeGetList<T>(LuaTable table)
		{
			if (table == null)
				return null;

			List<T> result = new List<T>();

			try
			{
                var t = SafeGetEnumerator(table);
                
                while (t.MoveNext())
				{
					if (t.Value != null && t.Value is T)
						result.Add((T)t.Value);
				}
			}
			catch (Exception e)
			{
				return HandleException(e, null) as List<T>;
			}

			return result;
		}

		/// <summary>
		/// Gets the field of a table, for a particular key, and of a particular type.
		/// </summary>
		/// <typeparam name="T">Type of the entity.</typeparam>
		/// <param name="table">The table to query.</param>
		/// <param name="key">The key to query.</param>
		/// <returns>The object of type <typeparamref name="T"/> if it was
		/// found and could be converted, the default for the type otherwise.</returns>
        public T SafeGetField<T>(LuaTable table, object key)
		{
			object o;
			lock (_luaState)
			{
				try
				{
					o = Dewrap(table[Wrap(key)]);
				}
				catch (Exception e)
				{
					o = HandleException(e, null);
				}
			}

			return (o != null && o is T) ? (T)o : default(T);
		}

		/// <summary>
		/// Gets the field of a table's metatable, for a particular key, and of a particular type.
		/// </summary>
		/// <typeparam name="T">Type of the field value to get.</typeparam>
		/// <param name="table">The table whose metatable to query.</param>
		/// <param name="key">The key to query.</param>
		/// <returns>If <paramref name="table"/> has a metatable, and this
		/// metatable has a value of type <typeparamref name="T"/> for
		/// <paramref name="key"/>, then this value is returned. Otherwise
		/// the default for <typeparamref name="T"/> is returned, namely
		/// but not only if <paramref name="table"/> doesn't have a
		/// metatable.</returns>
		public T SafeGetFieldInMetatable<T>(LuaTable table, object key)
		{
			object o = null;
			lock (_luaState)
			{
				try
				{
					LuaTable mt = table.Metatable;

					if (mt != null)
					{
						o = Dewrap(mt[Wrap(key)]);
					}
				}
				catch (Exception e)
				{
					o = HandleException(e, null);
				}
			}

			return (o != null && o is T) ? (T)o : default(T);
		}

        ///// <summary>
        ///// Calls a function contained by a table and that takes as first parameter the table
        ///// itself.
        ///// </summary>
        ///// <param name="table">Table that contains the function.</param>
        ///// <param name="func">Name of the function to call.</param>
        ///// <param name="parameters">Parameters for the function. The table will be added
        ///// as first parameter.</param>
        ///// <returns>The returned list of results, or null.</returns>
        //public IList<object> SafeCallSelf(LuaTable table, string func, params object[] parameters)
        //{
        //    // Checks if the function exists.
        //    LuaFunction funcInternal;
        //    lock (_luaState)
        //    {
        //        funcInternal = table[func] as LuaFunction; 
        //    }
        //    if (func == null)
        //    {
        //        throw new Exception(func + " is not a LuaFunction.");
        //    }

        //    // Conforms parameters: the array must be non-null, and its first parameter, self.
        //    parameters = (parameters ?? new object[] { }).ConcatBefore(table);

        //    // Calls the function and returns its result.
        //    LuaVararg ret;
        //    lock (_luaState)
        //    {
        //        try
        //        {
        //            ret = funcInternal.Call(Wrap(parameters));
        //        }
        //        catch (Exception e)
        //        {
        //            ret = HandleException<LuaVararg>(e, null);
        //        }
        //    }
        //    return Dewrap(ret);
        //}

		/// <summary>
		/// Calls a function.
		/// </summary>
		/// <param name="func">Function to call.</param>
		/// <param name="parameters">Parameters to pass the function.</param>
		/// <returns>The raw array returned by the function, or null if the function
		/// could not be called.</returns>
        public IList<object> SafeCallRaw(LuaFunction func, params object[] parameters)
		{
			lock (_luaState)
			{
				try
				{
					return Dewrap(func.Call(Wrap(parameters)));
				}
				catch (Exception e)
				{
                    return HandleException<IList<object>>(e, new List<object>());
				}
			}
		}

		/// <summary>
		/// Calls a function.
		/// </summary>
        /// <param name="funcName">Name of the function (in the lua state) to call.</param>
		/// <param name="parameters">Parameters to pass the function.</param>
        public IList<object> SafeCallRaw(string funcName, params object[] parameters)
		{
			LuaFunction lf;
			
			lock (_luaState)
			{
                try
                {
                    lf = _luaState.Globals[funcName] as LuaFunction;
                }
                catch (Exception ex)
                {
                    return HandleException<IList<object>>(ex, new List<object>());
                }
			}

			if (lf == null)
			{
				throw new InvalidOperationException("Function " + funcName + " has not been found.");
			}

			lock (_luaState)
			{
				try
				{
					return Dewrap(lf.Call(Wrap(parameters)));
				}
				catch (Exception e)
				{
                    return HandleException<IList<object>>(e, new List<object>());
				}
			}
		}

		/// <summary>
		/// Processes a Lua chunk.
		/// </summary>
		/// <param name="chunk">Chunk to process.</param>
		/// <param name="chunkName">Name of the chunk.</param>
		/// <returns>The result of the lua state's DoString method.</returns>
        public IList<object> SafeDoString(string chunk, string chunkName = "")
		{
			lock (_luaState)
			{
				try
				{
					return Dewrap(_luaState.DoString(chunk, chunkName));
				}
				catch (Exception e)
				{
					return HandleException(e, null) as IList<object>;
				}
			}
		}

		/// <summary>
		/// Processes a Lua chunk.
		/// </summary>
		/// <param name="chunk">Chunk to process.</param>
		/// <param name="chunkName">Name of the chunk.</param>
		/// <returns>The result of the lua state's DoString method.</returns>
        public IList<object> SafeDoString(byte[] chunk, string chunkName)
		{
			lock (_luaState)
			{
				try
				{
					return Dewrap(_luaState.DoString(chunk, chunkName));
				}
				catch (Exception e)
				{
					return HandleException(e, null) as IList<object>;
				}
			}
		}

		/// <summary>
		/// Counts how many fields a LuaTable has.
		/// </summary>
		/// <param name="luaTable">Table whose fields to count.</param>
		/// <returns>How many fields the table has.</returns>
		public int SafeCount(LuaTable luaTable)
		{
			lock (_luaState)
			{
				try
				{
					return luaTable.Keys.Count;
				}
				catch (Exception e)
				{
					return (int)HandleException(e, 0);
				}
			}
		}

		/// <summary>
		/// Sets a field of a table to a particular value.
		/// </summary>
		/// <param name="table">Table to query.</param>
		/// <param name="key">Key of the field to set.</param>
		/// <param name="value">Value to set the field to.</param>
        public void SafeSetField(LuaTable table, object key, object value)
		{
			lock (_luaState)
			{
				try
				{
					table[Wrap(key)] = Wrap(value);
				}
				catch (Exception e)
				{
					HandleException(e);
				}
			}
		}

		/// <summary>
		/// Loads a lua chunk into a lua function.
		/// </summary>
		/// <param name="chunk">Chunk to load.</param>
		/// <param name="chunkName">Name of the chunk.</param>
		/// <returns>LuaFunction that corresponds to the loaded chunk.</returns>
		public LuaFunction SafeLoadString(byte[] chunk, string chunkName)
		{
			lock (_luaState)
			{
				try
				{
					return _luaState.CompileString(chunk, chunkName);
				}
				catch (Exception e)
				{
					return HandleException(e, null) as LuaFunction;
				}
			}
		}

		/// <summary>
		/// Creates an empty table.
		/// </summary>
		/// <returns>A new empty table.</returns>
		public LuaTable SafeCreateTable()
		{
			lock (_luaState)
			{
				try
				{
					return (LuaTable)_luaState.CreateTable();
				}
				catch (Exception e)
				{
					return HandleException(e, null) as LuaTable;
				}
			}
		}

		/// <summary>
		/// Gets a nested field inside a recursive construct of tables.
		/// </summary>
		/// <remarks>
		/// Each key passed as an argument to this method must correspond to a value of type LuaTable in the
		/// previous LuaTable, except for the last key, that must correspond to a value of type <typeparamref name="T"/>
		/// in the previous LuaTable.
		/// </remarks>
		/// <typeparam name="T">Type of the value to get.</typeparam>
		/// <param name="luaTable">The outermost LuaTable, that contains others.</param>
		/// <param name="firstKey">Key of the field of <paramref name="luaTable"/> that contains the first lua table.</param>
		/// <param name="secondKey">Key of a field that corresponds to either a LuaTable or a <typeparamref name="T"/>.</param>
		/// <param name="otherKeys">Optional keys of fields that correspond to LuaTables, except for the last key,
		/// that must correspond to a <typeparamref name="T"/>.</param>
		/// <returns>A valid <typeparamref name="T"/> or null.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="luaTable"/>, <paramref name="firstKey"/>
		/// or <paramref name="secondKey"/> is null.</exception>
        /// <exception cref="InvalidOperationException">A field was found to have a value of a different
        /// type than expected. This is thrown when the field at one of the intermediary keys has a value
        /// which is not a LuaTable, or when the final field has a value that is not a <typeparamref name="T"/>.
        /// </exception>
        public T SafeGetInnerField<T>(LuaTable luaTable, object firstKey, object secondKey, params object[] otherKeys)
		{
			if (luaTable == null || firstKey == null || secondKey == null)
			{
				throw new ArgumentNullException();
			}

			// Gets the inner fields for the first and second keys.
			LuaTable lastTable;
			LuaValue current;
			lock (_luaState)
			{
				try
				{
					lastTable = luaTable[Wrap(firstKey)] as LuaTable;
				}
				catch (Exception ex)
				{
					return HandleException<T>(ex, default(T));
				}
			}
			if (lastTable == null)
			{
				throw new InvalidOperationException("The field at " + firstKey + " is not a LuaTable.");
			}
			lock (_luaState)
			{
				try
				{
					current = lastTable[Wrap(secondKey)];
				}
				catch (Exception ex)
				{
					return HandleException<T>(ex, default(T));
				}
			}

			// Return now?
			if (otherKeys == null || otherKeys.Length == 0)
			{
				if (!(current is T))
				{
					throw new InvalidOperationException("The field at " + secondKey + " is not a " + typeof(T).FullName);
				}

				return (T)Dewrap(current);
			}

			// Is current a table?
			if (!(current is LuaTable))
			{
				throw new InvalidOperationException("The field at " + secondKey + " is not a LuaTable.");
			}
			lastTable = (LuaTable)current;

			// Loops inside the nested tables.
			int keysLeft = otherKeys.Length;
			IEnumerator e = otherKeys.GetEnumerator();
			while (e.MoveNext())
			{
				keysLeft--;

				// Gets the next field.
				lock (_luaState)
				{
					try
					{
						current = lastTable[(LuaValue)e.Current];
					}
					catch (Exception ex)
					{
						return HandleException<T>(ex, default(T));
					}
				}

				// No more keys? Break!
				if (keysLeft == 0)
				{
					break;
				}

				// A LuaTable is expected now.
				if (!(current is LuaTable))
				{
					throw new InvalidOperationException("The field at " + e.Current + " is not a LuaTable.");
				}

				lastTable = (LuaTable)current;
			}

			// A T is expected now.
            object o = Dewrap(current);
            if (o != null && !(o is T))
			{
				throw new InvalidOperationException("The field at " + e.Current + " is not a " + typeof(T).FullName);
			}

			// Returns the value.
            return o == null ? default(T) : (T)o;
		}

        /// <summary>
        /// Sets a nested field inside a recursive construct of tables.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Each key passed as an argument to this method must correspond to a value of type LuaTable in the
        /// previous LuaTable, except for the last key, that must correspond to a value of type <typeparamref name="T"/>
        /// in the previous LuaTable.
        /// </para>
        /// 
        /// <para>
        /// Intermediate tables are automatically created if their associated key does not yield any
        /// other value than Nil.
        /// </para>
        /// </remarks>
        /// <param name="value">The value to set the field with.</param>
        /// <param name="luaTable">The outermost LuaTable, that contains others.</param>
        /// <param name="firstKey">Key of the field of <paramref name="luaTable"/> that contains the first lua table.</param>
        /// <param name="secondKey">Key of a field that corresponds to either a LuaTable or a <typeparamref name="T"/>.</param>
        /// <param name="otherKeys">Optional keys of fields that correspond to LuaTables, except for the last key,
        /// that must correspond to a <typeparamref name="T"/>.</param>
        /// <returns>A valid <typeparamref name="T"/> or null.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="luaTable"/>, <paramref name="firstKey"/>
        /// or <paramref name="secondKey"/> is null.</exception>
        /// <exception cref="InvalidOperationException">A field was found to have a value of a different
        /// type than expected. This is thrown when the field at one of the intermediary keys has a value
        /// which is not a LuaTable, or when the final field has a value that is not a <typeparamref name="T"/>.
        /// </exception>
        public void SafeSetInnerField(object value, LuaTable luaTable, object firstKey, object secondKey, params object[] otherKeys)
        {
            if (luaTable == null || firstKey == null || secondKey == null)
            {
                throw new ArgumentNullException();
            }

            // Gets the inner fields for the first and second keys.
            LuaTable lastTable;
            LuaValue current;
            lock (_luaState)
            {
                try
                {
                    lastTable = luaTable[Wrap(firstKey)] as LuaTable;
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                    return;
                }
            }
            if (lastTable == null)
            {
                throw new InvalidOperationException("The field at " + firstKey + " is not a LuaTable.");
            }

            // Sets the field now?
            lock (_luaState)
            {
                try
                {
                    if (otherKeys == null || otherKeys.Length == 0)
                    {
                        lastTable[Wrap(secondKey)] = Wrap(value);
                        return;
                    }
                    else
                    {
                        current = lastTable[Wrap(secondKey)];
                    }
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                    return;
                }
            }

            // Is current a table?
            if (!(current is LuaTable))
            {
                throw new InvalidOperationException("The field at " + secondKey + " is not a LuaTable.");
            }
            lastTable = (LuaTable)current;

            // Loops inside the nested tables.
            int keysLeft = otherKeys.Length;
            IEnumerator e = otherKeys.GetEnumerator();
            while (e.MoveNext() && keysLeft > 1)
            {
                keysLeft--;

                // Gets the next field.
                lock (_luaState)
                {
                    try
                    {
                        current = lastTable[Wrap(e.Current)];
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex);
                        return;
                    }
                }

                // A LuaTable is expected now.
                if (!(current is LuaTable))
                {
                    throw new InvalidOperationException("The field at " + e.Current + " is not a LuaTable.");
                }

                lastTable = (LuaTable)current;
            }

            // Setting the field now.
            lock (_luaState)
            {
                try
                {
                    lastTable[Wrap(e.Current)] = Wrap(value);
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                    return;
                }
            }
        }

		/// <summary>
		/// Gets the dictionary enumerator for a table.
		/// </summary>
		/// <param name="table">The table to get an enumerator of.</param>
		/// <returns>A thread-safe enumerator on the table.</returns>
		public IDictionaryEnumerator SafeGetEnumerator(LuaTable table)
		{
			IEnumerator<KeyValuePair<LuaValue,LuaValue>> e;

			lock (_luaState)
			{
				try
				{
					e = table.GetEnumerator();
				}
				catch (Exception ex)
				{
                    return HandleException(ex, null) as IDictionaryEnumerator;
				}
			}

            return (IDictionaryEnumerator)new SafeDictionaryEnumerator(e, this);
		}

        /// <summary>
        /// Gets a nested global value at a specific path.
        /// </summary>
        /// <remarks>
        /// This method looks up into the lua state's table of globals
        /// and navigates in the inner constructs of tables to retrieve
        /// a specific field. It is equivalent to calling
        /// <code>SafeGetInnerField()</code> on the Globals table
        /// with a splitted path.
        /// </remarks>
        /// <typeparam name="T">The type of the value to get.</typeparam>
        /// <param name="path">The full dot-separated path to the value.</param>
        /// <returns>The value, or null if it was not found.</returns>
        /// <exception cref="InvalidOperationException">An intermediary part of the path
        /// was found but is not a table.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/>is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty or contains only
        /// white spaces, or is not a valid path.</exception>
        public T SafeGetGlobal<T>(string path)
        {
            // Gets the conformed path.
            List<string> conformedPathParts;
            try
            {
                conformedPathParts = GetConformedPathInternal(path);
            }
            catch (Exception)
            {
                throw;
            }

            // Takes care of invalid paths.
            int pathPartsCounts = conformedPathParts.Count;
            if (pathPartsCounts == 0)
            {
                throw new ArgumentException("path has no valid components.");
            }

            // Takes care of non-nested paths.
            if (pathPartsCounts == 1)
            {
                // Gets the global.
                LuaValue lv = null;
                object ret = null;
                lock (_luaState)
                {
                    lv = _luaState.Globals[conformedPathParts[0]];
                }
                ret = Dewrap(lv);

                // Checks the type.
                if (!(ret is T))
                {
                    throw new InvalidOperationException(String.Format("The global value was found but has type {0}, not {1}.", lv.GetType().FullName, typeof(T).FullName));
                }

                // Returns
                return ret == null ? default(T) : (T)ret;
            }
            // Takes care of nested paths.
            else
            {
                try
                {
                    return SafeGetInnerField<T>(
                        _luaState.Globals,
                        conformedPathParts[0],
                        conformedPathParts[1],
                        conformedPathParts.GetRange(2, pathPartsCounts - 2).Cast<LuaValue>().ToArray()
                    );
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Sets a nested global value at a specific path.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method looks up into the lua state's table of globals
        /// and navigates in the inner constructs of tables to set
        /// a specific field. It is equivalent to calling
        /// <code>SafeGetInnerField()</code> on the Globals table
        /// with a splitted path.
        /// </para>
        /// <para>
        /// If an intermediary part of the path is not found, a corresponding table
        /// will be created.
        /// </para>
        /// </remarks>
        /// <param name="path">The full dot-separated path to the value.</param>
        /// <param name="value">The value to set the global to.</param>
        /// <exception cref="InvalidOperationException">An intermediary part of the path
        /// was found but is not a table.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/>is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty or contains only
        /// white spaces, or is not a valid path.</exception>
        public void SafeSetGlobal(string path, object value)
        {
            // Gets the conformed path.
            List<string> conformedPathParts;
            try
            {
                conformedPathParts = GetConformedPathInternal(path);
            }
            catch (Exception)
            {
                throw;
            }

            // Takes care of invalid paths.
            int pathPartsCounts = conformedPathParts.Count;
            if (pathPartsCounts == 0)
            {
                throw new ArgumentException("path has no valid components.");
            }

            // Takes care of non-nested paths.
            if (pathPartsCounts == 1)
            {
                // Gets the global.
                lock (_luaState)
                {
                    _luaState.Globals[conformedPathParts[0]] = Wrap(value);
                }
            }
            // Takes care of nested paths.
            else
            {
                try
                {
                    SafeSetInnerField(
                        value,
                        _luaState.Globals,
                        conformedPathParts[0],
                        conformedPathParts[1],
                        conformedPathParts.GetRange(2, pathPartsCounts - 2).Select(s => new LuaString(s)).ToArray()
                    );
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates a Lua function from a delegate.
        /// </summary>
        /// <param name="func">Delegate to wrap in a lua function.</param>
        /// <returns>A LuaFunction bound to the delegate.</returns>
        public LuaFunction SafeCreateFunction(Delegate func)
        {
            LuaFunction lf;

            lock (_luaState)
            {
                try
                {
                    lf = (LuaFunction)_luaState.CreateFunctionFromDelegate(func);
                }
                catch (Exception ex)
                {
                    lf = HandleException<LuaFunction>(ex, null);
                }
            }

            return lf;
        }

        /// <summary>
        /// Converts a native Lua value to a regular .Net type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>A .Net object, among string, double, bool, LuaTable 
        /// or LuaFunction. Null is returned if the value was lua's Nil.</returns>
        public object SafeDewrap(LuaValue value)
        {
            return Dewrap(value);
        }

        /// <summary>
        /// Converts a .Net value to a native Lua type.
        /// </summary>
        /// <param name="value">The value to convert, among string, double,
        /// bool, LuaTable or LuaFunction.</param>
        /// <returns>A LuaValue, or Nil if the object was null or not 
        /// supported.</returns>
        public LuaValue SafeWrap(object value)
        {
            return Wrap(value);
        }

        /// <summary>
        /// Gets a copy of LuaTable that is protected against garbage collection.
        /// </summary>
        /// <param name="native">Table to protect.</param>
        /// <returns>A protected copy of the table.</returns>
        public LuaTable SafeProtectTableFromGC(LuaTable table)
        {
            lock (_luaState)
            {
                try
                {
                    table = (LuaTable)table.CopyReference();
                }
                catch (Exception ex)
                {
                    table = HandleException<LuaTable>(ex, table);
                }
            }

            return table;
        }

        /// <summary>
        /// Gets a copy of LuaFunction that is protected against garbage collection.
        /// </summary>
        /// <param name="native">Function to protect.</param>
        /// <returns>A protected copy of the function.</returns>
        public LuaFunction SafeProtectFunctionFromGC(LuaFunction func)
        {
            lock (_luaState)
            {
                try
                {
                    func = (LuaFunction)func.CopyReference();
                }
                catch (Exception ex)
                {
                    func = HandleException<LuaFunction>(ex, func);
                }
            }

            return func;
        }

		#endregion

		#region Exception Tracking
		private object HandleException(Exception ex, object defValue = null)
		{
			return HandleException<object>(ex, defValue);
		}

		private T HandleException<T>(Exception ex, T defValue)
		{
			if (RethrowsExceptions)
			{
                Type exType = ex.GetType();
                if (exType.Equals(typeof(ObjectDisposedException)) && !RethrowsDisposedLuaExceptions)
				{
					// The exception is due to the Lua state being disposed.
					// Just emit a warning and ignore it.
					System.Diagnostics.Debug.WriteLine("SafeLua: Ignored {0} likely due to a disposed Lua state.", exType.Name);

					return defValue;
				}

				throw ex;
			}
			else
			{
				return defValue;
			}
		} 
		#endregion

        private List<string> GetConformedPathInternal(string path)
        {
            /// Sanity checks.
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            if (String.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("path is empty or whitespace");
            }

            /// Constructs a conformed list composed of the elements of the path.

            // Splits the path around dots.
            string[] pathParts = path.Split(new char[] { '.' }, StringSplitOptions.None);

            // Checks if all elements are not garbage and makes a list out of them.
            List<string> conformedPathParts = new List<string>();
            foreach (var pp in pathParts)
            {
                if (String.IsNullOrWhiteSpace(pp))
                {
                    throw new ArgumentException("path contains a subpath which is empty or whitespace");
                }

                conformedPathParts.Add(pp);
            }

            return conformedPathParts;
        }

        #region Wrap and Dewrap
        private object Dewrap(LuaValue luaValue)
        {
            object ret = null;

            lock (_luaState)
            {
                try
                {
                    if (luaValue is LuaTable || luaValue is LuaFunction)
                    {
                        ret = luaValue;
                    }
                    else if (luaValue is LuaNumber)
                    {
                        ret = luaValue.ToNumber();
                    }
                    else if (luaValue is LuaBoolean)
                    {
                        ret = luaValue.ToBoolean();
                    }
                    else if (luaValue is LuaString)
                    {
                        ret = luaValue.ToString();
                    }
                }
                catch (Exception ex)
                {
                    ret = HandleException(ex);
                }
            }
            // LuaNil is null, already assigned!
            // Other objects are wrapped to null.

            return ret;
        }

        private IList<object> Dewrap(IList<LuaValue> vararg)
        {
            List<object> lo = new List<object>();

            foreach (var item in vararg)
            {
                lo.Add(Dewrap(item));
            }

            return lo;
        }

        private LuaValue Wrap(object value)
        {
            LuaValue lv;

            lock (_luaState)
            {
                try
                {
                    // Asks Eluant to perform the conversion.
                    lv = _luaState.AsLuaValue(value, false);
                }
                catch (Exception ex)
                {
                    lv = HandleException<LuaValue>(ex, default(LuaValue));
                }
            }

            return lv;
        }

        private LuaValue[] Wrap(object[] array)
        {
            List<LuaValue> lvl = new List<LuaValue>();

            foreach (var item in array)
            {
                LuaValue wrappedItem;
                
                if (item is Array)
                {
                    // Creates a new table with the wrapped contents of the array.
                    lock (_luaState)
                    {
                        wrappedItem = _luaState.CreateTable(((Array)item).OfType<object>().Select(o => Wrap(o)));
                    }
                }
                else
                {
                    // Simply wraps the item.
                    wrappedItem = Wrap(item);
                }

                lvl.Add(wrappedItem);
            }

            return lvl.ToArray();
        } 
        #endregion
	}
}
