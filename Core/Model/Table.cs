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

namespace WF.Player.Core
{

	public class Table
	{
		protected LuaTable wigTable;
		protected Engine engine;

		public Table (Engine e, LuaTable t)
		{
			engine = e;
			wigTable = t;
		}

		/// <summary>
		/// Underlaying LuaTable for this object.
		/// </summary>
		/// <value>The LuaTable.</value>
		public LuaTable WIGTable { get { return wigTable; } }

		#region Standard methods

		/// <summary>
		/// Gets the boolean from LuaTable for entry key as string.
		/// </summary>
		/// <returns>The boolean.</returns>
		/// <param name="key">Key as string for the entry.</param>
		protected bool GetBool(string key)
		{
			object value = wigTable [key];

			return (value is bool && value != null) ? (bool)value : false;
		}

		/// <summary>
		/// Gets the boolean from LuaTable for entry key as number.
		/// </summary>
		/// <returns>The boolean.</returns>
		/// <param name="key">Key as number for the entry.</param>
		protected bool GetBool(double key)
		{
			object value = wigTable [key];

            return (value is bool && value != null) ? (bool)value : false;
        }

		/// <summary>
		/// Gets the double from LuaTable for entry key as string.
		/// </summary>
		/// <returns>The double.</returns>
		/// <param name="key">Key as string for the entry.</param>
		protected double GetDouble(string key)
		{
			object num = wigTable [key];

			return (num is double && num != null) ? (double)num : 0;
		}

		/// <summary>
		/// Gets the double from LuaTable for entry key as number.
		/// </summary>
		/// <returns>The double.</returns>
		/// <param name="key">Key as number for the entry.</param>
		protected double GetDouble(double key)
		{
			object num = wigTable [key];

            return (num is double && num != null) ? (double)num : 0;
        }

		/// <summary>
		/// Gets the integer from LuaTable for entry key as string.
		/// </summary>
		/// <returns>The integer.</returns>
		/// <param name="key">Key as string for the entry.</param>
		protected int GetInt(string key)
		{
			object num = wigTable [key];

			return (num is double && num != null) ? Convert.ToInt32 ((double)num) : 0;
		}

		/// <summary>
		/// Gets the integer from LuaTable for entry key as number.
		/// </summary>
		/// <returns>The integer.</returns>
		/// <param name="key">Key as number for the entry.</param>
		protected int GetInt(double key)
		{
			object num = wigTable [key];

            return (num is double && num != null) ? Convert.ToInt32((double)num) : 0;
        }

		/// <summary>
		/// Gets the string from LuaTable for entry key as string.
		/// </summary>
		/// <returns>The string.</returns>
		/// <param name="key">Key as string for the entry.</param>
		protected string GetString(string key)
		{
			object obj = wigTable [key];

			return obj is string ? (string)obj : "";
		}

		/// <summary>
		/// Gets the string from LuaTable for entry key as number.
		/// </summary>
		/// <returns>The string.</returns>
		/// <param name="key">Key as number for the entry.</param>
		protected string GetString(double key)
		{
			object obj = wigTable [key];

			return obj is string ? (string)obj : "";
		}

		/// <summary>
		/// Gets a Table object from a LuaTable.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		protected Table GetTable(LuaTable t)
		{
            if (t != null)
			    return engine.GetTable (t);
            else
                return null;
		}

		/// <summary>
		/// Gets a Table object from a field in the table.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		protected Table GetTable(string key)
		{
			return GetTable(wigTable[key] as LuaTable);
		}

		/// <summary>
		/// Gets a Table object from a field in the table.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		protected Table GetTable(double key)
		{
			return GetTable(wigTable[key] as LuaTable);
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
			return EnumUtils.ParseEnum<T>(GetString(key), defaultValue);
		}

		#endregion

	}

}

