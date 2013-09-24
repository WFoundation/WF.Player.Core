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
using System.Globalization;

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
		/// Gets the raw value of the distance, in meters.
		/// </summary>
		public double Value {
			get {
				return GetDouble ("value");
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the value of the distance with a given unit.
		/// </summary>
		/// <param name="unit">The unit to query for.</param>
		/// <returns>The double value of the distance.</returns>
		public double ValueAs(DistanceUnit unit)
		{
			return Value * unit.GetConversionFactor();
		}

		/// <summary>
		/// Gets the measure of the distance for a given unit. 
		/// </summary>
		/// <param name="unit">The unit to query for.</param>
		/// <returns>The measure of the distance, i.e. is value and unit symbol.
		/// The decimal precision of the value is using the default setting of the current culture.</returns>
		public string MeasureAs(DistanceUnit unit)
		{
			return MeasureAs(unit, CultureInfo.CurrentCulture.NumberFormat.NumberDecimalDigits);
		}

		/// <summary>
		/// Gets the measure of the distance for a given unit and a given precision.
		/// </summary>
		/// <param name="unit">The unit to query for.</param>
		/// <param name="decimalDigits">How many digits should the measure have. Should be 0 or greater.</param>
		/// <returns>The measure of the distance, i.e. is value and unit symbol.</returns>
		public string MeasureAs(DistanceUnit unit, int decimalDigits)
		{
			return String.Format("{0:F"+ decimalDigits +"} {1}", ValueAs(unit), unit.ToSymbol());
		}

		/// <summary>
		/// Gets the most meaningful measure of the distance.
		/// </summary>
		/// <param name="smallestUnit">Smallest unit displayable in the measure.</param>
		/// <returns>The measure of the distance, in <code>smallestUnit</code> or its supported
		/// factors.</returns>
		public string BestMeasureAs(DistanceUnit smallestUnit)
		{
			if (!smallestUnit.Equals(DistanceUnit.Meters) && !smallestUnit.Equals(DistanceUnit.Kilometers))
			{
				throw new NotImplementedException("smallest Unit must be Meters or Kilometers.");
			}

			double v = Value;
			if (v > 1000d)
			{
				return MeasureAs(DistanceUnit.Kilometers, 2);
			}
			else if (v > 100d)
			{
				return MeasureAs(DistanceUnit.Meters, 0);
			}
			else
			{
				return MeasureAs(DistanceUnit.Meters, 1);
			}
		}

		#endregion

	}

}

