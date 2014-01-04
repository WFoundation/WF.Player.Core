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
using WF.Player.Core.Utils;

namespace WF.Player.Core
{
	/// <summary>
	/// A Thing that has a location and can contain other Things.
	/// </summary>
	public class Zone : Thing
	{

		#region Constructor

		internal Zone(WF.Player.Core.Data.IDataContainer data, RunOnClick runOnClick)
			: base(data, runOnClick)
		{
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets a value indicating whether this <see cref="WF.Player.Core.Zone"/> is active.
		/// </summary>
		/// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
		public bool Active {
			get 
			{
                return DataContainer.GetBool("Active").Value;
			}
		}

		/// <summary>
		/// Gets the bounds of this zone.
		/// </summary>
		/// <value>The bounds.</value>
		public CoordBounds Bounds {
			get {
				var points = Points;

				if (points.Count == 0)
					return null;

				var result = new CoordBounds (points [0], points [0]);

				foreach (ZonePoint zp in points)
					result.Add (zp);

				return result;
			}
		}

		/// <summary>
		/// Gets the original point.
		/// </summary>
		/// <value>The original point as ZonePoint.</value>
        public ZonePoint OriginalPoint
        {
            get
            {
                return DataContainer.GetWherigoObject<ZonePoint>("OriginalPoint");
            }
        }

		/// <summary>
		/// Gets the point defining the zone.
		/// </summary>
		/// <value>The inventory.</value>
        public WherigoCollection<ZonePoint> Points
        {
			get 
			{
				return DataContainer.GetWherigoObjectList<ZonePoint>("Points");
			}
		}

		/// <summary>
		/// Gets the position state of the player for this zone.
		/// </summary>
		public PlayerZoneState State
		{
			get
			{
				return DataContainer.GetEnum<PlayerZoneState>("State").Value;
			}
		}

		#endregion

	}

}
