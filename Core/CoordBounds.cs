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
using System.Collections.Generic;
using System.Linq;

namespace WF.Player.Core
{
	/// <summary>
	/// Describes the rectangle with the smallest measure within which
    /// a certain amount of ZonePoints or geocoordinates lie.
	/// </summary>
	public class CoordBounds
	{
        #region Members

        double left = 360;
        double top = -360;
        double right = 360;
        double bottom = 360;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new empty CoordBounds with invalid values.
        /// </summary>
        public CoordBounds()
        {
            left = 360;
            right = -360;
            top = -360;
            bottom = 360;
        }

        /// <summary>
        /// Constructs a CoordBounds from left, top, right and
        /// bottom values.
        /// </summary>
        /// <param name="l">The left coordinate, in decimal degrees.</param>
        /// <param name="t">The top coordinate, in decimal degrees.</param>
        /// <param name="r">The right coordinate, in decimal degrees.</param>
        /// <param name="b">The bottom coordinate, in decimal degrees.</param>
        public CoordBounds(double l, double t, double r, double b)
        {
            if (l < r)
            {
                left = l;
                right = r;
            }
            else
            {
                left = r;
                right = l;
            }
            if (b < t)
            {
                top = t;
                bottom = b;
            }
            else
            {
                top = b;
                bottom = t;
            }
        }

        /// <summary>
        /// Constructs a CoordBounds that is the minimum bounding box
        /// of two ZonePoints.
        /// </summary>
        /// <param name="zp1"></param>
        /// <param name="zp2"></param>
        public CoordBounds(ZonePoint zp1, ZonePoint zp2)
            : this(zp1.Latitude, zp1.Longitude, zp2.Latitude, zp2.Longitude)
        {
        }

        /// <summary>
        /// Constructs a CoordBounds that is completely enclosing
        /// a single ZonePoint.
        /// </summary>
        /// <param name="zp"></param>
        public CoordBounds(ZonePoint zp)
            : this(zp, zp)
        {
        }

        /// <summary>
        /// Constructs a CoordBounds that is the minimum bounding box
        /// of several ZonePoints.
        /// </summary>
        /// <param name="zps"></param>
        public CoordBounds(IEnumerable<ZonePoint> zps)
            : this()
        {
            Inflate(zps);
        } 

        #endregion

		#region Properties
		 
        /// <summary>
        /// Gets or sets the left coordinate of the current bounds,
        /// in decimal degrees.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the right coordinate of the current bounds,
        /// in decimal degrees.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the top coordinate of the current bounds,
        /// in decimal degrees.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the bottom coordinate of the current bounds,
        /// in decimal degrees.
        /// </summary>
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

        /// <summary>
        /// Gets if the coordinates of the current bounds are valid.
        /// </summary>
        /// <value>True if and only if left is less than or equal to right
        /// and top is greater than or equal to bottom.</value>
        public bool IsValid
        {
            get
            {
                return left <= right && top >= bottom;
            }
        }

		#endregion

		#region Methods

        /// <summary>
        /// Enlarges these bounds so that a point defined by its latitude 
        /// and longitude can fit inside.
        /// </summary>
        /// <param name="lat">Latitude of the point in decimal degrees.</param>
        /// <param name="lon">Longitude of the point in decimal degrees.</param>
        public void Inflate(double lat, double lon)
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

        /// <summary>
        /// Enlarges these bounds so that a ZonePoint can fit inside.
        /// </summary>
        /// <param name="zp"></param>
        public void Inflate(ZonePoint zp)
		{
            Inflate(zp.Latitude, zp.Longitude);
		}

        /// <summary>
        /// Enlarges these bounds so that another bounds can fit inside.
        /// </summary>
        /// <param name="bounds"></param>
        public void Inflate(CoordBounds bounds)
		{
            Inflate(bounds.Left, bounds.Top);
            Inflate(bounds.Right, bounds.Bottom);
		}

        /// <summary>
        /// Enlarges these bounds so that all ZonePoints of an enumeration
        /// can fit inside.
        /// </summary>
        /// <param name="zps"></param>
        public void Inflate(IEnumerable<ZonePoint> zps)
        {
            foreach (ZonePoint zp in zps.Where(p => p != null))
            {
                Inflate(zp);
            }
        }

        /// <summary>
        /// Enlarges these bounds so that all other bounds of an enumeration
        /// can fit inside.
        /// </summary>
        /// <param name="boundsEnum"></param>
        public void Inflate(IEnumerable<CoordBounds> boundsEnum)
        {
            foreach (CoordBounds bounds in boundsEnum.Where(cb => cb != null))
            {
                Inflate(bounds);
            }
        }

		#endregion
	}
}