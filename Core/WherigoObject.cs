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
using WF.Player.Core.Engines;
using WF.Player.Core.Lua;

namespace WF.Player.Core
{
	public class WherigoObject
	{
		#region Members

		protected LuaTable wigTable;
		protected Engine engine; 

		#endregion

		#region Properties

		/// <summary>
		/// Underlying LuaTable for this object.
		/// </summary>
		/// <value>The LuaTable.</value>
		internal LuaTable WIGTable { get { return wigTable; } }

		#endregion

		#region Constructor

		internal WherigoObject(Engine e, LuaTable t)
		{
			engine = e;
			wigTable = t == null ? null : (LuaTable)t.CopyReference();
		} 

		#endregion

		#region Table Field Getters

		/// <summary>
		/// Gets the boolean from LuaTable for entry key as string.
		/// </summary>
		/// <returns>The boolean.</returns>
		/// <param name="key">Key as string for the entry.</param>
		protected bool GetBool(string key)
		{
			return engine.SafeLuaState.SafeGetField<bool>(wigTable, key);
		}

		/// <summary>
		/// Gets the boolean from LuaTable for entry key as number.
		/// </summary>
		/// <returns>The boolean.</returns>
		/// <param name="key">Key as number for the entry.</param>
		protected bool GetBool(double key)
		{
			return engine.SafeLuaState.SafeGetField<bool>(wigTable, key);
        }

		/// <summary>
		/// Gets the double from LuaTable for entry key as string.
		/// </summary>
		/// <returns>The double.</returns>
		/// <param name="key">Key as string for the entry.</param>
		protected double GetDouble(string key)
		{
			return engine.SafeLuaState.SafeGetField<double>(wigTable, key);
		}

		/// <summary>
		/// Gets the double from LuaTable for entry key as number.
		/// </summary>
		/// <returns>The double.</returns>
		/// <param name="key">Key as number for the entry.</param>
		protected double GetDouble(double key)
		{
			return engine.SafeLuaState.SafeGetField<double>(wigTable, key);
        }

		/// <summary>
		/// Gets the integer from LuaTable for entry key as string.
		/// </summary>
		/// <returns>The integer.</returns>
		/// <param name="key">Key as string for the entry.</param>
		protected int GetInt(string key)
		{
			return Convert.ToInt32(engine.SafeLuaState.SafeGetField<double>(wigTable, key));
		}

		/// <summary>
		/// Gets the integer from LuaTable for entry key as number.
		/// </summary>
		/// <returns>The integer.</returns>
		/// <param name="key">Key as number for the entry.</param>
		protected int GetInt(double key)
		{
			return Convert.ToInt32(engine.SafeLuaState.SafeGetField<double>(wigTable, key));
        }

		/// <summary>
		/// Gets the string from LuaTable for entry key as string.
		/// </summary>
		/// <returns>The string.</returns>
		/// <param name="key">Key as string for the entry.</param>
		protected string GetString(string key)
		{
			return engine.SafeLuaState.SafeGetField<string>(wigTable, key);
		}

		/// <summary>
		/// Gets the string from LuaTable for entry key as number.
		/// </summary>
		/// <returns>The string.</returns>
		/// <param name="key">Key as number for the entry.</param>
		protected string GetString(double key)
		{
			return engine.SafeLuaState.SafeGetField<string>(wigTable, key);
		}

		/// <summary>
		/// Gets a Table object from a LuaTable.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		protected WherigoObject GetTable(LuaTable t)
		{
			return t != null ? engine.GetTable(t) : null;
		}

		/// <summary>
		/// Gets a Table object from a field in the table.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		protected WherigoObject GetTable(string key)
		{
			return GetTable(GetLuaTable(key));
		}

		/// <summary>
		/// Gets a Table object from a field in the table.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		protected WherigoObject GetTable(double key)
		{
			return GetTable(GetLuaTable(key));
		}

		/// <summary>
		/// Gets a LuaFunction from a field in the table.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		protected LuaFunction GetLuaFunc(string key)
		{
			return engine.SafeLuaState.SafeGetField<LuaFunction>(wigTable, key);
		}

		/// <summary>
		/// Gets a LuaFunction from a field in the table.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		protected LuaFunction GetLuaFunc(double key)
		{
			return engine.SafeLuaState.SafeGetField<LuaFunction>(wigTable, key);
		}
		
		/// <summary>
		/// Gets a LuaTable object from a field in the table.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		protected LuaTable GetLuaTable(string key)
		{
			return engine.SafeLuaState.SafeGetField<LuaTable>(wigTable, key);
		}

		/// <summary>
		/// Gets a LuaTable object from a field in the table.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		protected LuaTable GetLuaTable(double key)
		{
			return engine.SafeLuaState.SafeGetField<LuaTable>(wigTable, key);
		}

		/// <summary>
		/// Gets an Enum object from a field in the table.
		/// </summary>
		/// <typeparam name="T">Type of the enum to get.</typeparam>
		/// <param name="key">Key of the string name of the value in the table.</param>
		/// <param name="defaultValue">The default value for the enum. If null, an
		/// exception is thrown if no such value of the enum exists.</param>
		/// <returns></returns>
		protected T GetEnum<T>(string key, T? defaultValue = null) where T : struct, IConvertible
		{
			return Utils.Utils.ParseEnum<T>(GetString(key), defaultValue);
		}

		/// <summary>
		/// Gets a list of primitive objects from a field in the table that contains a table.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <returns></returns>
		protected List<T> GetList<T>(string key) where T : LuaValue
		{
			return engine.SafeLuaState.SafeGetList<T>(GetLuaTable(key));
		}

		/// <summary>
		/// Gets a list of primitive objects from a field in the table that contains a table.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <returns></returns>
		protected List<T> GetList<T>(double key) where T : LuaValue
		{
			return engine.SafeLuaState.SafeGetList<T>(GetLuaTable(key));
		}

		/// <summary>
		/// Gets a list of Table entities from a field in the table that contains itself
		/// a LuaTable.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <returns>A list of all entries of the table that could convert to 
		/// <paramref name="T"/>.</returns>
		protected List<T> GetTableList<T>(string key) where T : WherigoObject
		{
			return engine.GetTableListFromLuaTable<T>(GetLuaTable(key));
		}

		/// <summary>
		/// Gets a list of Table entities from a field in the table that contains itself
		/// a LuaTable.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <returns>A list of all entries of the table that could convert to 
		/// <paramref name="T"/>.</returns>
		protected List<T> GetTableList<T>(double key) where T : WherigoObject
		{
			return engine.GetTableListFromLuaTable<T>(GetLuaTable(key));
		}

		/// <summary>
		/// Gets a list of Table entities from a field in the table that contains a
		/// function that returns itself a LuaTable.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <returns>A list of all entries of the table that could convert to 
		/// <paramref name="T"/>.</returns>
		protected List<T> GetTableFuncList<T>(string key, params LuaValue[] parameters) where T : WherigoObject
		{
			return engine.GetTableListFromLuaTable<T>(engine.SafeLuaState.SafeCallSelf(wigTable, key, parameters));
		}

		/// <summary>
		/// Gets a Media from a field in the table that contains a table.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		protected Media GetMedia(string key)
		{
			LuaTable media = GetLuaTable("Media");

			if (media == null)
			{
				return null;
			}

			int objIndex = Convert.ToInt32((double)engine.SafeLuaState.SafeGetField<object>(media, "ObjIndex"));

			return engine.GetMedia(objIndex);
		}

		/// <summary>
		/// Gets a Media from a field in the table that contains a table.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		protected Media GetMedia(double key)
		{
			LuaTable media = GetLuaTable("Media");

			if (media == null)
			{
				return null;
			}

			int objIndex = Convert.ToInt32((double)engine.SafeLuaState.SafeGetField<object>(media, "ObjIndex"));

			return engine.GetMedia(objIndex);
		}

		#endregion

	}

}

