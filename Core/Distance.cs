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
using System.Globalization;

namespace WF.Player.Core
{
	/// <summary>
	/// A distance between two objects of the game.
	/// </summary>
	public class Distance : WherigoObject, IComparable<double>, IComparable<Distance>
	{

		#region Constructor

		/// <summary>
		/// Creates a new Distance expressed in a specified unit.
		/// </summary>
		/// <param name="value">The value of the distance.</param>
		/// <param name="unit">The unit in which the value is expressed.</param>
		public Distance(double value, DistanceUnit unit)
			: base(new Data.Native.NativeDataContainer())
		{
			// Converts and stores the value in meters.
			double convertedValue = value / unit.GetConversionFactor();
			((Data.Native.NativeDataContainer)DataContainer)["value"] = convertedValue;
		}

		internal Distance(WF.Player.Core.Data.IDataContainer data)
			: base(data)
		{
		}

		#endregion

		#region Operators

		/// <summary>
		/// Determines if a distance is strictly smaller than a second.
		/// </summary>
		/// <param name="d1"></param>
		/// <param name="d2"></param>
		/// <returns>Returns true if the first Distance is strictly smaller than the second.</returns>
		public static bool operator <(Distance d1, Distance d2)
		{
			return d1.Value < d2.Value;
		}

		/// <summary>
		/// Determines if a distance is smaller or equal to a second.
		/// </summary>
		/// <param name="d1"></param>
		/// <param name="d2"></param>
		/// <returns>Returns true if the first Distance is smaller or equal to the second.</returns>
		public static bool operator <=(Distance d1, Distance d2)
		{
			return d1.Value <= d2.Value;
		}

		/// <summary>
		/// Determines if a distance is strictly greater than a second.
		/// </summary>
		/// <param name="d1"></param>
		/// <param name="d2"></param>
		/// <returns>Returns true if the first Distance is strictly greater than the second.</returns>
		public static bool operator >(Distance d1, Distance d2)
		{
			return d1.Value > d2.Value;
		}

		/// <summary>
		/// Determines if a distance is greater or equal to a second.
		/// </summary>
		/// <param name="d1"></param>
		/// <param name="d2"></param>
		/// <returns>Returns true if the first Distance is greater or equal to the second.</returns>
		public static bool operator >=(Distance d1, Distance d2)
		{
			return d1.Value >= d2.Value;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the raw value of the distance, in meters.
		/// </summary>
		public double Value {
			get {
                return DataContainer.GetDouble("value").Value;
			}
		}

		#endregion

		#region Values and Measures

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
		/// <param name="digitsOfPrecision">How many precision digits should the measure have. Should be 0 or greater.</param>
		/// <returns>The measure of the distance, i.e. is value and unit symbol.</returns>
		public string MeasureAs(DistanceUnit unit, int digitsOfPrecision)
		{
			return String.Format("{0:F"+ digitsOfPrecision +"} {1}", ValueAs(unit), unit.ToSymbol());
		}

		/// <summary>
		/// Gets the most meaningful measure of the distance.
		/// </summary>
		/// <param name="smallestUnit">Smallest unit displayable in the measure.</param>
		/// <param name="format">Optional format for the measure.</param>
		/// <returns>The measure of the distance, in <code>smallestUnit</code> or its supported
		/// factors.</returns>
		public string BestMeasureAs(DistanceUnit smallestUnit, DistanceFormat format = null)
		{
			switch (smallestUnit)
			{
				case DistanceUnit.Meters:
					return BestMeasureAs(
						DistanceUnit.Meters, 1,
						DistanceUnit.Meters, 0,
						DistanceUnit.Kilometers, 2);

				case DistanceUnit.Kilometers:
					return BestMeasureAs(
						DistanceUnit.Kilometers, 2,
						DistanceUnit.Kilometers, 2,
						DistanceUnit.Kilometers, 1);

				case DistanceUnit.Miles:
					return BestMeasureAs(
						DistanceUnit.Miles, 1,
						DistanceUnit.Miles, 1,
						DistanceUnit.Miles, 1);

				case DistanceUnit.Feet:
					return BestMeasureAs(
						DistanceUnit.Feet, 0,
						DistanceUnit.Feet, 0,
						DistanceUnit.Miles, 2);

				case DistanceUnit.NauticalMiles:
					return BestMeasureAs(
						DistanceUnit.NauticalMiles, 1,
						DistanceUnit.NauticalMiles, 1,
						DistanceUnit.NauticalMiles, 1);

				default:
					throw new NotImplementedException(smallestUnit.ToString() + " is not a supported unit.");
			}
		}

		private string BestMeasureAs(DistanceUnit smallUnit, int smallPrecision, DistanceUnit mediumUnit, int mediumPrecision, DistanceUnit bigUnit, int bigPrecision)
		{
			double v = ValueAs(smallUnit);

			if (v > 1000d)
			{
				return MeasureAs(bigUnit, bigPrecision);
			}
			else if (v > 100d)
			{
				return MeasureAs(mediumUnit, mediumPrecision);
			}
			else
			{
				return MeasureAs(smallUnit, smallPrecision);
			}
		}

		#endregion

        #region IComparable
        public int CompareTo(double other)
        {
            return Value.CompareTo(other);
        }

        public int CompareTo(Distance other)
        {
            return Value.CompareTo(other.Value);
        }
        #endregion
    }

	/// <summary>
	/// Describes parameters that have an influence on the formatting of a distance.
	/// </summary>
	public class DistanceFormat
	{
		#region Properties

		/// <summary>
		/// Gets or sets how many digits of precision should have a big distance.
		/// </summary>
		public int BigDistancePrecisionDigits { get; set; }

		/// <summary>
		/// Gets or sets how many digits of precision should have a medium distance.
		/// </summary>
		public int MediumDistancePrecisionDigits { get; set; }

		/// <summary>
		/// Gets or sets how many digits of precision should have a small distance.
		/// </summary>
		public int SmallDistancePrecisionDigits { get; set; }

		#endregion
	}

}

