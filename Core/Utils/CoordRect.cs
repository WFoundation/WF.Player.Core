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

namespace WF.Player.Core.Utils
{
	/// <summary>
	/// Class for a rect from coordinates.
	/// </summary>
	public class CoordBounds
	{
		double left = 360;
		double top = -360;
		double right = 360;
		double bottom = 360;

		public CoordBounds()
		{
			left = 360;
			right = -360;
			top = -360;
			bottom = 360;
		}

		public CoordBounds(double l, double t, double r, double b)
		{
			if (l < r) {
				left = l;
				right = r;
			} else {
				left = r;
				right = l;
			}
			if (b < t) {
				top = t;
				bottom = b;
			} else {
				top = b;
				bottom = t;
			}
		}

		public CoordBounds (ZonePoint zp1, ZonePoint zp2) : this (zp1.Latitude, zp1.Longitude, zp2.Latitude, zp2.Longitude)
		{
		}

		public CoordBounds (ZonePoint zp) : this (zp, zp)
		{
		}

		#region Properties
		 
		public double Left {
			get { 
				return left; 
			}
			set { 
				if (left != value) {
					if (value < right) {
						left = value;
					} else {
						left = right;
						right = value;
					}
				}
			}
		}

		public double Right {
			get { 
				return right; 
			}
			set { 
				if (right != value) {
					if (value > left) {
						right = value;
					} else {
						right = left;
						left = value;
					}
				}
			}
		}

		public double Top {
			get { 
				return top; 
			}
			set { 
				if (top != value) {
					if (value > bottom) {
						top = value;
					} else {
						top = bottom;
						bottom = value;
					}
				}
			}
		}

		public double Bottom {
			get { 
				return bottom; 
			}
			set { 
				if (bottom != value) {
					if (value < top) {
						bottom = value;
					} else {
						bottom = top;
						bottom = value;
					}
				}
			}
		}

		#endregion

		#region Methods

		public void Add(double lat, double lon)
		{
			if (lat < left)
				left = lat;
			if (lat > right)
				right = lat;
			if (lon > top)
				top = lon;
			if (lon < bottom)
				bottom = lon;
		}

		public void Add(ZonePoint zp)
		{
			Add (zp.Latitude, zp.Longitude);
		}

		public void Add(CoordBounds bounds)
		{
			Add (bounds.Left, bounds.Top);
			Add (bounds.Right, bounds.Bottom);
		}

		#endregion
	}
}