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
	public class Distance : Table
	{

		#region Constructor

		public Distance (Engine e, LuaTable t) : base (e, t)
		{
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the value of the distance.
		/// </summary>
		/// <value>The value.</value>
		public double Value {
			get {
				return GetDouble ("value");
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the value of the distance with given unit.
		/// </summary>
		/// <value>The value.</value>
		public double ValueAs(string unit)
		{
			double value = GetDouble ("value");

			switch (unit.ToLower ()) {
				case "km":
					value = value / 1000.0;
					break;
				case "kilometers":
					value = value / 1000.0;
					break;
				case "kilometres":
					value = value / 1000.0;
					break;
				case "mi":
					value = value / 1609.344;
					break;
				case "miles":
					value = value / 1609.344;
					break;
				case "feet":
					value = value * 3.2808399;
					break;
				case "ft":
					value = value * 3.2808399;
					break;
				case "nauticalmiles":
					value = value / 1852.216;
					break;
				case "nm":
					value = value / 1852.216;
					break;
			}

			return value;
		}

		#endregion

	}

}

