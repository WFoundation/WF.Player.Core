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
using System.Linq;
using System.Collections.Generic;
using WF.Player.Core.Data;

namespace WF.Player.Core.Utils
{
	/// <summary>
	/// A class that can perform useful mathematical operations over 
	/// geographic entities such as distances, points and zones.
	/// </summary>
	public class GeoMathHelper
	{
		#region Nested Classes

		private struct Point
		{
			public double Lat, Lon;

			public Point(double lat, double lon)
			{
				Lat = lat;
				Lon = lon;
			}

			public Point(ZonePoint point)
			{
				Lat = point.Latitude;
				Lon = point.Longitude;
			}

			public ZonePoint ToZonePoint(IDataFactory _dataFactory)
			{
				return _dataFactory.CreateWherigoObject<ZonePoint>(Lat, Lon);
			}

			public override string ToString()
			{
				return Lat + ", " + Lon;
			}
		}

		private struct Vector
		{
			public double? Distance;
			public double? Bearing;

			public Vector(LocationVector vector)
			{
				Distance = vector.Distance == null ? new Nullable<double>() : vector.Distance.Value;
				Bearing = vector.Bearing;
			}

			public Vector(double? dist, double? bear)
			{
				Distance = dist;
				Bearing = bear;
			}

			public LocationVector ToLocationVector(IDataFactory dataFactory)
			{
				return new LocationVector(
					Distance.HasValue ? dataFactory.CreateWherigoObject<Distance>(Distance.Value) : null,
					Bearing
					);
			}
		}

		private struct CalcInput
		{
			public Point MainPoint;

			public Point[] TargetZonePoints;
			public int TargetZonePointCount;

			public Point SegmentPoint1;
			public Point SegmentPoint2;

			public Point TargetPoint;

			public Vector TargetVector;

			public void SetTargetZone(Zone zone)
			{
				// Orders points by key.
				WherigoCollection<ZonePoint> zonePoints = zone.Points;
				if (zonePoints == null)
				{
					TargetZonePoints = new Point[] { };
					TargetZonePointCount = 0;
				}
				else
				{
					TargetZonePoints = zonePoints.Select(zp => new Point(zp)).ToArray();
					TargetZonePointCount = TargetZonePoints.Count();
				}
			}

		}

		#endregion

		#region Math Constants

		public const double LATITUDE_COEF = 110940.00000395167;
		public const double METER_COEF = 9.013881377e-6;
		public const double PI_180 = Math.PI / 180;
		public const double DEG_PI = 180 / Math.PI;
		public const double PI_2 = Math.PI / 2;
		public const double NMILES_COEF = 1852.216;
		public const double METER_NMI_COEF = 1d / NMILES_COEF;
		public const double EARTH_RADIUS = 6367449;

		#endregion

		#region Fields

		private IDataFactory _dataFactory;

		#endregion

		#region Constructors

		public GeoMathHelper()
		{
			_dataFactory = Data.Native.NativeDataFactory.Instance;
		}

		internal GeoMathHelper(IDataFactory dataFactory)
		{
			_dataFactory = dataFactory;
		}

		#endregion

		#region Data Model Math

		public bool IsPointInZone(ZonePoint point, Zone target)
		{
			// Computes the calculation input.
			CalcInput c = new CalcInput();
			c.MainPoint = new Point(point);
			c.SetTargetZone(target);

			return IsPointInZoneCore(c);
		}

		private bool IsPointInZoneCore(CalcInput c)
		{
			// http://alienryderflex.com/polygon/

			double lat = c.MainPoint.Lat;
			double lon = c.MainPoint.Lon;
			int count = c.TargetZonePointCount;

			int j = count - 1;
			bool oddNodes = false;

			Point iPt, jPt;
			double iLat, iLon, jLat, jLon;
			for (int i = 0; i < count; i++)
			{
				jPt = c.TargetZonePoints[j];
				jLat = jPt.Lat;
				jLon = jPt.Lon;
				iPt = c.TargetZonePoints[i];
				iLat = iPt.Lat;
				iLon = iPt.Lon;

				if ((iLat < lat && jLat >= lat || jLat < lat && iLat >= lat) && (iLon <= lon || jLon <= lon))
				{
					oddNodes ^= (iLon + (lat - iLat) / (jLat - iLat) * (jLon - iLon) < lon);
				}
				j = i;
			}

			return oddNodes;
		}

		public LocationVector VectorToZone(ZonePoint point, Zone zone)
		{
			// Computes the input.
			CalcInput c = new CalcInput();
			c.MainPoint = new Point(point);
			c.SetTargetZone(zone);

			// Performs the computation.
			Vector vector = VectorToZoneCore(c);

			// Returns the right object.
			return vector.ToLocationVector(_dataFactory);
		}

		private Vector VectorToZoneCore(CalcInput c)
		{
			// If the point is in the zone, the distance and bearing are 0.
			if (IsPointInZoneCore(c))
			{
				return new Vector(0d, 0d);
			}

			// If the zone doesn't have points, the distance and bearing are null.
			if (c.TargetZonePointCount == 0)
			{
				return new Vector();
			}

			// Computes the minimal distance to the Zone's edges.
			c.SegmentPoint1 = c.TargetZonePoints.Last();
			c.SegmentPoint2 = c.TargetZonePoints.First();
			Vector minVec = VectorToSegmentCore(c);
			double minDist = minVec.Distance.Value;

			for (int i = 0; i < c.TargetZonePointCount - 1; i++)
			{
				c.SegmentPoint1 = c.TargetZonePoints[i];
				c.SegmentPoint2 = c.TargetZonePoints[i + 1];
				Vector curr = VectorToSegmentCore(c);
				double currDist = curr.Distance.Value;

				if (currDist < minDist)
				{
					minVec = curr;
					minDist = currDist;
				}
			}

			return minVec;
		}

		public LocationVector VectorToSegment(ZonePoint point, ZonePoint segmentStartPoint, ZonePoint segmentEndPoint)
		{
			// Computes the input.
			CalcInput c = new CalcInput();
			c.MainPoint = new Point(point);
			c.SegmentPoint1 = new Point(segmentStartPoint);
			c.SegmentPoint2 = new Point(segmentEndPoint);

			// Performs the computation.
			Vector vector = VectorToSegmentCore(c);

			// Returns the right object.
			return vector.ToLocationVector(_dataFactory);
		}

		private Vector VectorToSegmentCore(CalcInput c)
		{
			// http://www.movable-type.co.uk/scripts/latlong.html#crossTrack
			
			Point mainPoint = c.MainPoint;
			Point firstLinePoint = c.SegmentPoint1;
			Point secondLinePoint = c.SegmentPoint2;

			c.MainPoint = firstLinePoint;
			c.TargetPoint = mainPoint;
			Vector d1 = VectorToPointCore(c);
			double b1 = d1.Bearing.GetValueOrDefault();
			double dd1 = PI_180 * MetersToNauticalMiles(d1.Distance.Value) / 60; // 1 nmi ~= 1'arc

			c.MainPoint = firstLinePoint;
			c.TargetPoint = secondLinePoint;
			Vector ds = VectorToPointCore(c);
			double bs = ds.Bearing.GetValueOrDefault();
			double dds = PI_180 * MetersToNauticalMiles(ds.Distance.Value) / 60; // 1 nmi ~= 1'arc

			var dist = Math.Asin(Math.Sin(dd1) * Math.Sin(PI_180 * (b1 - bs)));
			var dat = Math.Acos(Math.Cos(dd1) / Math.Cos(dist));

			c.MainPoint = mainPoint;
			if (dat <= 0)
			{
				c.TargetPoint = firstLinePoint;
				return VectorToPointCore(c);
			}
			else if (dat >= dds)
			{
				c.TargetPoint = secondLinePoint;
				return VectorToPointCore(c);
			}

			c.MainPoint = firstLinePoint;
			c.TargetVector = new Vector(NauticalMilesToMeters(dat * 60 / PI_180), bs);
			Point intersect = TranslatePointCore(c);

			c.MainPoint = mainPoint;
			c.TargetPoint = intersect;
			return VectorToPointCore(c);
		}

		public LocationVector VectorToPoint(ZonePoint point, ZonePoint target)
		{
			// Computes the input.
			CalcInput c = new CalcInput();
			c.MainPoint = new Point(point);
			c.TargetPoint = new Point(target);

			// Performs the computation.
			Vector vector = VectorToPointCore(c);

			// Returns the right object.
			return vector.ToLocationVector(_dataFactory);
		}

		private Vector VectorToPointCore(CalcInput c)
		{
			// http://www.movable-type.co.uk/scripts/latlong.html#ortho-dist
			
			double lat1 = c.MainPoint.Lat * PI_180;
			double lon1 = c.MainPoint.Lon * PI_180;
			double lat2 = c.TargetPoint.Lat * PI_180;
			double lon2 = c.TargetPoint.Lon * PI_180;

			double dLat = lat2 - lat1;
			double dLon = lon2 - lon1;

			double hvsLat = Math.Sin(dLat / 2);
			double hvsLon = Math.Sin(dLon / 2);

			double a = (hvsLat * hvsLat) + (hvsLon * hvsLon * Math.Cos(lat1) * Math.Cos(lat2));

			double cc = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

			double distance = EARTH_RADIUS * cc;

			// http://www.movable-type.co.uk/scripts/latlong.html#bearing

			double bearing = Math.Atan2(
				Math.Sin(dLon) * Math.Cos(lat2),
				Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon)
				) / PI_180;

			return new Vector(distance, bearing);
		}

		public ZonePoint TranslatePoint(ZonePoint point, LocationVector vector)
		{
			// Computes the input.
			CalcInput c = new CalcInput();
			c.MainPoint = new Point(point);
			c.TargetVector = new Vector(vector);

			// Performs the computation.
			Point newPoint = TranslatePointCore(c);

			// Returns the right object.
			return newPoint.ToZonePoint(_dataFactory);
		}

		private Point TranslatePointCore(CalcInput c)
		{
			double lat = c.MainPoint.Lat;
			double lon = c.MainPoint.Lon;
			double dist = c.TargetVector.Distance.Value;

			double rad = AzimuthToAngle(c.TargetVector.Bearing.GetValueOrDefault());
			double x = MetersToLatitude(dist * Math.Sin(rad));
			double y = MetersToLongitude(lat, dist * Math.Cos(rad));

			return new Point(lat + x, lon + y);
		}

		public ZonePoint[] GetCircle(ZonePoint center, double radius, int points)
		{
			// Computes the input.
			CalcInput c = new CalcInput();
			c.MainPoint = new Point(center);
			c.TargetVector = new Vector(radius, null);
			
			// For each needed point, translate the center "radius meters"
			// towards "i * 360/points".
			ZonePoint[] pts = new ZonePoint[points];
			double step = 360d / points;
			for (int i = 0; i < points; i++)
			{
				c.TargetVector.Bearing = step * i;
				pts[i] = TranslatePointCore(c).ToZonePoint(_dataFactory);
			}

			return pts;
		}

		#endregion

		#region Numeric Location Math

		private double NauticalMilesToMeters(double nmiles)
		{
			return nmiles * NMILES_COEF;
		}

		private double MetersToNauticalMiles(double meters)
		{
			return meters * METER_NMI_COEF;
		}

		private double LatitudeToMeters(double degrees)
		{
			return degrees * LATITUDE_COEF;
		}

		private double LongitudeToMeters(double latitude, double degrees)
		{
			return degrees * PI_180 * Math.Cos(latitude * PI_180) * 6367449;
		}

		private double MetersToLatitude(double meters)
		{
			return meters * METER_COEF;
		}

		private double MetersToLongitude(double latitude, double meters)
		{
			return meters / (PI_180 * Math.Cos(latitude * PI_180) * EARTH_RADIUS);
		}

		private double AzimuthToAngle(double azim)
		{
			double ret = -(azim * PI_180) + PI_2;

			while (ret > Math.PI) ret -= Math.PI * 2;
			while (ret <= -Math.PI) ret += Math.PI * 2;

			return ret;
		}

		#endregion
	}
}
