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
using System.Text;
using System.Globalization;

namespace WF.Player.Core
{
	/// <summary>
	/// A defining point of a Zone.
	/// </summary>
	public class ZonePoint : WherigoObject
	{
		#region Constructor

		internal ZonePoint(WF.Player.Core.Data.IDataContainer data)
			: base(data)
		{
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the altitude in meters.
		/// </summary>
        /// <remarks>0 is the default if no altitude is specified.</remarks>
		public double Altitude 
        {
			get 
            {
				return DataContainer.GetDouble("altitude").GetValueOrDefault();
			}
		}

        /// <summary>
        /// Gets if this point is marked as invalid.
        /// </summary>
        public bool IsInvalid
        {
            get
            {
                double? alt = DataContainer.GetDouble("altitude");
                double? lat = DataContainer.GetDouble("latitude");
                double? lon = DataContainer.GetDouble("longitude");

                return alt.GetValueOrDefault() == 360 &&
                    lat.GetValueOrDefault() == 360 &&
                    lon.GetValueOrDefault() == 360;
            }
        }

		/// <summary>
		/// Gets the latitude in decimal degrees.
		/// </summary>
		public double Latitude 
        {
			get 
            {
                return DataContainer.GetDouble("latitude").Value;
			}
		}

		/// <summary>
		/// Gets the longitude in decimal degrees.
		/// </summary>
		public double Longitude 
        {
			get 
            {
                return DataContainer.GetDouble("longitude").Value;
			}
		}

		#endregion

        #region Coordinate Conversion

        /// <summary>
        /// Converts the value of the current ZonePoint object to
        /// its equivalent string representation in decimal degrees and
        /// using default formatting.
        /// </summary>
        /// <returns>A string representing the current coordinate in
        /// the WGS84 system, using decimal degrees and default
        /// formatting. E.g. "-45.6781°, 2.38°"</returns>
        public override string ToString()
        {
            return ToString(GeoCoordinateUnit.DecimalDegrees, new GeoCoordinateFormat());
        }

        /// <summary>
        /// Converts the value of the current ZonePoint object to
        /// its equivalent string representation in the specified unit,
        /// using default formatting.
        /// </summary>
        /// <returns>A string representing the current coordinate in
        /// the WGS84 system, using the specified unit and the default
        /// formatting.</returns>
        public string ToString(GeoCoordinateUnit unit)
        {
            return ToString(unit, new GeoCoordinateFormat());
        }

        /// <summary>
        /// Converts the value of the current ZonePoint object to
        /// its equivalent string representation in the specified unit,
        /// using the specified formatting.
        /// </summary>
        /// <returns>A string representing the current coordinate in
        /// the WGS84 system, using the specified unit and formatting.</returns>
        public string ToString(GeoCoordinateUnit unit, GeoCoordinateFormat format)
        {
            // Creates a builder to make the main string.
            StringBuilder sBuilder = new StringBuilder();

            /// 1. Latitude component.
            double lat = DataContainer.GetDouble("latitude").Value;
            bool isDmOrDms = unit == GeoCoordinateUnit.DegreesMinutes
                || unit == GeoCoordinateUnit.DegreesMinutesSeconds;

            // DM and DMS output cardinal directions, others don't.
            if (isDmOrDms)
            {
                sBuilder.Append(lat >= 0 ? format.NorthSymbol : format.SouthSymbol);
                sBuilder.Append(format.CardinalPointSeparator);
            }
            
            // Computes and adds the main latitude component.
            sBuilder.Append(GetConvertedCoordinate(lat, 2, unit, format));

            // Adds the eventual separator.
            sBuilder.Append(format.ComponentSeparator);

            /// 2. Longitude component.
            double lon = DataContainer.GetDouble("longitude").Value;

            // DM and DMS output cardinal directions, others don't.
            if (isDmOrDms)
            {
                sBuilder.Append(lon >= 0 ? format.EastSymbol : format.WestSymbol);
                sBuilder.Append(format.CardinalPointSeparator);
            }

            // Computes and adds the main latitude component.
            sBuilder.Append(GetConvertedCoordinate(lon, 3, unit, format));

            // Returns the coordinates.
            return sBuilder.ToString();
        }

        private string GetConvertedCoordinate(double value, int maxIntPartDigits, GeoCoordinateUnit unit, GeoCoordinateFormat format)
        {
            // DD : DD.DDD°
            // DM : DD° MM.MMM'
            // DMS : DD° MM' SS.SSS"

            StringBuilder coordBuilder = new StringBuilder(); // Builder for the final coordinates.
            
            bool isDmOrDms = unit == GeoCoordinateUnit.DegreesMinutes
                || unit == GeoCoordinateUnit.DegreesMinutesSeconds;
            
            string fmtProtectString = "'{0}'";

            /// 1. Creates the two format strings for decimal and floats.
            StringBuilder formatBuilder = new StringBuilder();

            // Heading zeroes:
            // All digits except the last one are optional if TrimIntegralZeroes is true.
            formatBuilder.Append(format.TrimIntegralZeroes ? '#' : '0', maxIntPartDigits - 1); // e.g. ##

            // The last digit of the integral part is non-optional.
            formatBuilder.Append('0'); // e.g. ##0

            // This so far gives the first format string.
            string fmtTruncatedInteger = formatBuilder.ToString(); // e.g. ##0

            // Adds the precision digits.
            if (format.PrecisionDigits > 0)
            {
                formatBuilder.Append('.'); // Floating part separator.
                formatBuilder.Append('0'); // First digit is non-optional. // e.g. ##0.0

                if (format.PrecisionDigits > 1)
                {
                    formatBuilder.Append(
                        format.TrimPrecisionZeroes ? '#' : '0', 
                        format.PrecisionDigits - 1); // Additional optional digits. // e.g. ##0.0###
                }
            }

            // This so far gives the second format string.
            string fmtFloatingNumber = formatBuilder.ToString();

            // Let's remove the first character if maxIntPartDigits is 3.
            // This makes two format strings for subcomponents whose integral
            // parts have only 2 digits.
            int startDigit = maxIntPartDigits - 2;
            string fmtFloatingNumberMax2 = fmtFloatingNumber.Substring(startDigit); // e.g. #0.0###
            string fmtTruncatedIntegerMax2 = fmtTruncatedInteger.Substring(startDigit); // e.g. #0

            /// 2. First subcomponent format: (D)DD° or (D)DD.DDD°
            formatBuilder.Clear();
            if (isDmOrDms)
            {
                // DM and DMS: Only uses the integral part.
                formatBuilder.Append(fmtTruncatedInteger);
            }
            else if (unit == GeoCoordinateUnit.DecimalDegrees)
            {
                // DD: Uses the whole number format.
                formatBuilder.Append(fmtFloatingNumber); // e.g. ##0.0###
            }

            // Computes and appends the first subcomponent.
            double degrees = isDmOrDms ? Math.Abs(value) : value;
            coordBuilder.Append(degrees.ToString(formatBuilder.ToString(), format.NumberFormat));

            // Adds the symbols.
            if (format.ShowSymbols)
            {
		        coordBuilder.Append(format.DegreesSymbol);
            }

            // Adds separators.
            if (isDmOrDms)
            {
                // DM and DMS: Adds a subcomponent separator.
                coordBuilder.Append(format.SubComponentSeparator);
                // e.g. ##0°,
            }

            // DD can return now.
            if (unit == GeoCoordinateUnit.DecimalDegrees)
            {
                return coordBuilder.ToString();
            }

            /// 3. Second subcomponent: MM' or MM.MMM'
            formatBuilder.Clear();
            if (isDmOrDms)
            {
                // DMS uses the integral format, DM the whole format.
                formatBuilder.Append(unit == GeoCoordinateUnit.DegreesMinutesSeconds 
                    ? fmtTruncatedIntegerMax2 
                    : fmtFloatingNumberMax2);
            }

            // Computes and appends the second subcomponent.
            double minutes = (Math.Abs(value) * 60) % 60;
            coordBuilder.Append(minutes.ToString(formatBuilder.ToString(), format.NumberFormat));

            // Adds symbol and separator.
            if (isDmOrDms)
            {
                // Adds the symbol.
                if (format.ShowSymbols)
                {
                    coordBuilder.Append(format.MinutesSymbol);
                }

                // DMS: Adds subcomponent separator.
                if (unit == GeoCoordinateUnit.DegreesMinutesSeconds)
                {
                    coordBuilder.Append(format.SubComponentSeparator);
                }
            }

            // DM can return now.
            if (unit == GeoCoordinateUnit.DegreesMinutes)
            {
                return coordBuilder.ToString();
            }

            /// 4. Third subcomponent : SS.SSS"
            formatBuilder.Clear();
            if (unit == GeoCoordinateUnit.DegreesMinutesSeconds)
            {
                // DMS: Uses the whole format.
                formatBuilder.Append(fmtFloatingNumberMax2);
            }

            // Computes and appends the third subcomponent.
            double seconds = (Math.Abs(value) * 3600) % 60;
            coordBuilder.Append(seconds.ToString(formatBuilder.ToString(), format.NumberFormat));

            // Adds symbol.
            if (unit == GeoCoordinateUnit.DegreesMinutesSeconds)
            {
                // DMS: Adds the symbol.
                if (format.ShowSymbols)
                {
                    coordBuilder.Append(format.SecondsSymbol);
                }
            }

            // DMS can return now.
            if (unit == GeoCoordinateUnit.DegreesMinutesSeconds)
            {
                return coordBuilder.ToString();
            }
            
            // Makes sure we're not doing something unexpected.
            throw new InvalidOperationException("Unexpected unit: " + unit.ToString());
           
        }

        //private void AppendLiteralInFormat(StringBuilder formatBuilder, string literal)
        //{
        //    // Escapes known format characters.
        //    var escLiteral = literal
        //        .Replace("#", @"\#")
        //        .Replace("0", @"\0")
        //        .Replace(".", @"\.")
        //        .Replace(",", @"\,")
        //        .Replace("%", @"\%")
        //        .Replace("‰", @"\‰")
        //        .Replace("E", @"\E")
        //        .Replace("+", @"\+")
        //        .Replace("-", @"\-")
        //        .Replace(@"\", @"\\");

        //    // Prepares the format.
        //    string format = 

        //    // Appends the string.
        //    formatBuilder.AppendFormat("'{0}'", escLiteral);
        //}

        #endregion
	}

    /// <summary>
    /// Describes parameters that have an influence on the formatting of a 
    /// geographic coordinate.
    /// </summary>
    public class GeoCoordinateFormat
    {
        #region Properties
        /// <summary>
        /// Gets or sets the string that separates a cardinal point and
        /// its following component.
        /// </summary>
        public string CardinalPointSeparator { get; set; }

        /// <summary>
        /// Gets or sets the string that separates the latitude
        /// and longitude parts of the coordinate.
        /// </summary>
        public string ComponentSeparator { get; set; }

        /// <summary>
        /// Gets or sets the symbol of degrees.
        /// </summary>
        public string DegreesSymbol { get; set; }

        /// <summary>
        /// Gets or sets the symbol of the East cardinal point.
        /// </summary>
        public string EastSymbol { get; set; }

        /// <summary>
        /// Gets or sets the NumberFormatInfo used to format numeric
        /// values.
        /// </summary>
        public NumberFormatInfo NumberFormat { get; set; }

        /// <summary>
        /// Gets or sets the symbol of arcminutes.
        /// </summary>
        public string MinutesSymbol { get; set; }

        /// <summary>
        /// Gets or sets the symbol of the North cardinal point.
        /// </summary>
        public string NorthSymbol { get; set; }

        /// <summary>
        /// Gets or sets how many precision digits the floating
        /// parts of the coordinate should have.
        /// </summary>
        public int PrecisionDigits { get; set; }

        /// <summary>
        /// Gets or sets the string that separates two parts of
        /// the same component.
        /// </summary>
        /// <remarks>A component is the latitude or longitude part
        /// of the coordinate. A subcomponent is a part of a
        /// component that is suffixed with its own unit
        /// (e.g. 16' in N 45°16'10.628")</remarks>
        public string SubComponentSeparator { get; set; }

        /// <summary>
        /// Gets or sets if unit symbols are displayed in coordinates.
        /// </summary>
        /// <remarks>This parameter does not impact the displaying
        /// of cardinal points.</remarks>
        public bool ShowSymbols { get; set; }

        /// <summary>
        /// Gets or sets the symbol of arcseconds.
        /// </summary>
        public string SecondsSymbol { get; set; }

        /// <summary>
        /// Gets or sets the symbol of the South cardinal point.
        /// </summary>
        public string SouthSymbol { get; set; }

        /// <summary>
        /// Gets or sets if the trailing zeroes in precision digits
        /// should be trimmed.
        /// </summary>
        /// <remarks>If true, there may be effectively fewer precision
        /// digits than expected.</remarks>
        public bool TrimPrecisionZeroes { get; set; }

        /// <summary>
        /// Gets or sets if the heading zeroes in integral parts
        /// should be trimmed.
        /// </summary>
        public bool TrimIntegralZeroes { get; set; }

        /// <summary>
        /// Gets or sets the symbol of the West cardinal point.
        /// </summary>
        public string WestSymbol { get; set; } 
        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new GeoCoordinateFormat that formats coordinates
        /// using standard separators and the invariant culture's 
        /// NumberFormatInfo.
        /// </summary>
        public GeoCoordinateFormat()
        {
            DefaultInit(CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Constructs a new GeoCoordinateFormat that formats coordinates
        /// using standard separators and a specified NumberFormatInfo.
        /// </summary>
        /// <param name="numberFormat">A format provider for numbers.</param>
        public GeoCoordinateFormat(NumberFormatInfo numberFormat)
        {
            DefaultInit(numberFormat);
        }

        private void DefaultInit(NumberFormatInfo nfi)
        {
            this.CardinalPointSeparator = " ";
            this.ComponentSeparator = ", ";
            this.DegreesSymbol = "°";
            this.EastSymbol = "E";
            this.NumberFormat = nfi;
            this.MinutesSymbol = "'";
            this.NorthSymbol = "N";
            this.PrecisionDigits = 3;
            this.SecondsSymbol = "\"";
            this.ShowSymbols = true;
            this.SouthSymbol = "S";
            this.SubComponentSeparator = " ";
            this.TrimIntegralZeroes = true;
            this.TrimPrecisionZeroes = false;
            this.WestSymbol = "W";
        }

        #endregion
    }
}

