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

		#region Special methods

		#endregion

		#region Standard methods

		/// <summary>
		/// Gets the boolean from LuaTable for entry key as string.
		/// </summary>
		/// <returns>The boolean.</returns>
		/// <param name="key">Key as string for the entry.</param>
		public bool GetBool(string key)
		{
			object value = wigTable [key];

			return value == null ? false : (bool)value;
		}

		/// <summary>
		/// Gets the boolean from LuaTable for entry key as number.
		/// </summary>
		/// <returns>The boolean.</returns>
		/// <param name="key">Key as number for the entry.</param>
		public bool GetBool(double key)
		{
			object value = wigTable [key];

			return value == null ? false : (bool)value;
		}

		/// <summary>
		/// Gets the double from LuaTable for entry key as string.
		/// </summary>
		/// <returns>The double.</returns>
		/// <param name="key">Key as string for the entry.</param>
		public double GetDouble(string key)
		{
			object num = wigTable [key];

			return num == null ? 0 : (double)num;
		}

		/// <summary>
		/// Gets the double from LuaTable for entry key as number.
		/// </summary>
		/// <returns>The double.</returns>
		/// <param name="key">Key as number for the entry.</param>
		public double GetDouble(double key)
		{
			object num = wigTable [key];

			return num == null ? 0 : (double)num;
		}

		/// <summary>
		/// Gets the integer from LuaTable for entry key as string.
		/// </summary>
		/// <returns>The integer.</returns>
		/// <param name="key">Key as string for the entry.</param>
		public int GetInt(string key)
		{
			object num = wigTable [key];

			return num == null ? 0 : Convert.ToInt32 ((double)num);
		}

		/// <summary>
		/// Gets the integer from LuaTable for entry key as number.
		/// </summary>
		/// <returns>The integer.</returns>
		/// <param name="key">Key as number for the entry.</param>
		public int GetInt(double key)
		{
			object num = wigTable [key];

			return num == null ? 0 : Convert.ToInt32 ((double)num);
		}

		/// <summary>
		/// Gets the string from LuaTable for entry key as string.
		/// </summary>
		/// <returns>The string.</returns>
		/// <param name="key">Key as string for the entry.</param>
		public string GetString(string key)
		{
			object obj = wigTable [key];

			return obj is string ? (string)obj : "";
		}

		/// <summary>
		/// Gets the string from LuaTable for entry key as number.
		/// </summary>
		/// <returns>The string.</returns>
		/// <param name="key">Key as number for the entry.</param>
		public string GetString(double key)
		{
			object obj = wigTable [key];

			return obj is string ? (string)obj : "";
		}

		public Table GetTable(LuaTable t)
		{
			return engine.GetTable (t);
		}

		#endregion

	}

}

