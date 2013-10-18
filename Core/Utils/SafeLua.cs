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
using NLua;
using System.Collections.Generic;
using System.Collections;

namespace WF.Player.Core.Utils
{
	/// <summary>
	/// A wrapper around a Lua state that provides with various thread-safe utilities.
	/// </summary>
	internal class SafeLua
	{
		#region Nested Classes

		/// <summary>
		/// A thread-safe wrapper around a IDictionaryEnumerator bound to a lua state.
		/// </summary>
		private class SafeDictionaryEnumerator : IDictionaryEnumerator
		{
			private IDictionaryEnumerator baseEnumerator;
			private Lua luaState;

			public SafeDictionaryEnumerator(IDictionaryEnumerator e, Lua luaState)
			{
				this.baseEnumerator = e;
				this.luaState = luaState;
			}

			public DictionaryEntry Entry
			{
				get
				{
					object key, value;

					lock (luaState)
					{
						key = baseEnumerator.Entry.Key;
						value = baseEnumerator.Entry.Value;
					}

					return new DictionaryEntry(key, value);
				}
			}

			public object Key
			{
				get
				{
					lock (luaState)
					{
						return baseEnumerator.Key;
					}
				}
			}

			public object Value
			{
				get
				{
					lock (luaState)
					{
						return baseEnumerator.Value;
					}
				}
			}

			public object Current
			{
				get
				{
					lock (luaState)
					{
						return baseEnumerator.Current;
					}
				}
			}

			public bool MoveNext()
			{
				lock (luaState)
				{
					return baseEnumerator.MoveNext();
				}
			}

			public void Reset()
			{
				lock (luaState)
				{
					baseEnumerator.Reset();
				}
			}
		}

		#endregion
		
		#region Members

		/// <summary>
		/// The underlying lua state, used both for lua operation and for locking.
		/// </summary>
		private Lua luaState; 

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs a thread-safe wrapper around a lua state.
		/// </summary>
		/// <param name="lua"></param>
		public SafeLua(Lua lua)
		{
			luaState = lua;
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
			lock (luaState)
			{
				o = table[key];
			}

			return (o != null && o is T) ? (T)o : default(T);
		}

		/// <summary>
		/// Calls a function contained by a table and that takes as first parameter the table
		/// itself.
		/// </summary>
		/// <param name="table">Table that contains the function.</param>
		/// <param name="func">Name of the function to call.</param>
		/// <param name="parameters">Parameters for the function. The table will be added
		/// as first parameter.</param>
		/// <returns>The inner LuaTable contained in the result, or null.</returns>
		public LuaTable SafeCallSelf(LuaTable table, string func, params object[] parameters)
		{
			lock (luaState)
			{
				return table.CallSelf(func, parameters);
			}
		}

		/// <summary>
		/// Calls a function.
		/// </summary>
		/// <param name="func">Function to call.</param>
		/// <param name="parameters">Parameters to pass the function.</param>
		public object[] SafeCallRaw(LuaFunction func, params object[] parameters)
		{
			lock (luaState)
			{
				return func.Call(parameters);
			}
		}

		/// <summary>
		/// Calls a function.
		/// </summary>
		/// <param name="func">Name of the function (in the lua state) to call.</param>
		/// <param name="parameters">Parameters to pass the function.</param>
		public object[] SafeCallRaw(string funcName, params object[] parameters)
		{
			LuaFunction lf;
			
			lock (luaState)
			{
				lf = luaState.GetFunction(funcName);
			}

			if (lf == null)
			{
				throw new InvalidOperationException("Function " + funcName + " has not been found.");
			}

			lock (luaState)
			{
				return lf.Call(parameters);
			}
		}

		/// <summary>
		/// Processes a Lua chunk.
		/// </summary>
		/// <param name="chunk">Chunk to process.</param>
		/// <param name="chunkName">Name of the chunk.</param>
		/// <returns>The result of the lua state's DoString method.</returns>
		public object[] SafeDoString(string chunk, string chunkName)
		{
			lock (luaState)
			{
				return luaState.DoString(chunk, chunkName);
			}
		}

		/// <summary>
		/// Processes a Lua chunk.
		/// </summary>
		/// <param name="chunk">Chunk to process.</param>
		/// <param name="chunkName">Name of the chunk.</param>
		/// <returns>The result of the lua state's DoString method.</returns>
		public object[] SafeDoString(byte[] chunk, string chunkName)
		{
			lock (luaState)
			{
				return luaState.DoString(chunk, chunkName);
			}
		}

		/// <summary>
		/// Counts how many fields a LuaTable has.
		/// </summary>
		/// <param name="luaTable">Table whose fields to count.</param>
		/// <returns>How many fields the table has.</returns>
		public int SafeCount(LuaTable luaTable)
		{
			lock (luaState)
			{
				return luaTable.Keys.Count;
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
			lock (luaState)
			{
				table[key] = value;
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
			lock (luaState)
			{
				return luaState.LoadString(chunk, chunkName);
			}
		}

		/// <summary>
		/// Creates an empty table.
		/// </summary>
		/// <returns>A new empty table.</returns>
		public LuaTable SafeEmptyTable()
		{
			lock (luaState)
			{
				return luaState.EmptyTable();
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
		public T SafeGetInnerField<T>(LuaTable luaTable, object firstKey, object secondKey, params object[] otherKeys)
		{
			if (luaTable == null || firstKey == null || secondKey == null)
			{
				throw new ArgumentNullException();
			}

			// Gets the inner fields for the first and second keys.
			LuaTable lastTable;
			object current;
			lock (luaState)
			{
				lastTable = luaTable[firstKey] as LuaTable;
			}
			if (lastTable == null)
			{
				throw new InvalidOperationException("The field at " + firstKey + " is not a LuaTable.");
			}
			lock (luaState)
			{
				current = lastTable[secondKey];
			}

			// Return now?
			if (otherKeys == null || otherKeys.Length == 0)
			{
				if (!(current is T))
				{
					throw new InvalidOperationException("The field at " + secondKey + " is not a " + typeof(T).FullName);
				}

				return (T)current;
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
				lock (luaState)
				{
					current = lastTable[e.Current];
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
			if (current != null && !(current is T))
			{
				throw new InvalidOperationException("The field at " + e.Current + " is not a " + typeof(T).FullName);
			}

			// Returns the value.
			return current == null ? default(T) : (T)current;
		}

		/// <summary>
		/// Gets the dictionary enumerator for a table.
		/// </summary>
		/// <param name="table">The table to get an enumerator of.</param>
		/// <returns>A thread-safe enumerator on the table.</returns>
		public IDictionaryEnumerator SafeGetEnumerator(LuaTable table)
		{
			IDictionaryEnumerator e;

			lock (luaState)
			{
				e = table.GetEnumerator();
			}

			return new SafeDictionaryEnumerator(e, luaState);
		}

		#endregion


	}
}
