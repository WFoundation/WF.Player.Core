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

namespace WF.Player.Core
{
	public class LocationVector
	{
		#region Properties

		/// <summary>
		/// Gets the distance.
		/// </summary>
		/// <value>If null, the distance is not available for this vector.</value>
		public Distance Distance { get; private set; }

		/// <summary>
		/// Gets the value of the bearing, in degrees.
		/// </summary>
		/// <value>If null, the bearing is not available for this vector.</value>
		public double? Bearing { get; private set; }

		#endregion

		#region Constructors

		internal LocationVector(Distance dist, double? bearing)
		{
			Distance = dist;
			Bearing = bearing;
		}

		#endregion
	}
}
