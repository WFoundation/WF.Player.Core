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
		public const double METER_NMI_COEF = 0.00053989383d;

		#endregion

		#region Members

		private IDataFactory _dataFactory;

		#endregion

		#region Constructors

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
			Point mainPoint = c.MainPoint;
			Point firstLinePoint = c.SegmentPoint1;
			Point secondLinePoint = c.SegmentPoint2;

			c.MainPoint = firstLinePoint;
			c.TargetPoint = mainPoint;
			Vector d1 = VectorToPointCore(c);
			double b1 = d1.Bearing.GetValueOrDefault();
			double dd1 = PI_180 * MetersToNauticalMiles(d1.Distance.Value) / 60;

			c.MainPoint = firstLinePoint;
			c.TargetPoint = secondLinePoint;
			Vector ds = VectorToPointCore(c);
			double bs = ds.Bearing.GetValueOrDefault();
			double dds = PI_180 * MetersToNauticalMiles(ds.Distance.Value) / 60;

			var dist = Math.Asin(Math.Sin(dd1) * Math.Sin(PI_180 * (b1 - bs)));
			var dat = Math.Acos(Math.Cos(dd1) / Math.Cos(dist));

			c.MainPoint = mainPoint;
			if (dat <= 0)
			{
				c.TargetPoint = firstLinePoint;
				return VectorToPointCore(c);
			}
			else if (dat >= PI_180 * dds)
			{
				c.TargetPoint = secondLinePoint;
				return VectorToPointCore(c);
			}

			c.MainPoint = firstLinePoint;
			c.TargetVector = new Vector(NauticalMilesToMeters(dat * 60), 0d);
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
			double lat1 = c.MainPoint.Lat;
			double lon1 = c.MainPoint.Lon;
			double lat2 = c.TargetPoint.Lat;
			double lon2 = c.TargetPoint.Lon;

			double mx = Math.Abs(LatitudeToMeters(lat1 - lat2));
			double my = Math.Abs(LongitudeToMeters(lat2, lon1 - lon2));

			double distance = Math.Sqrt(mx * mx + my * my);
			double bearing = (Math.Atan2(LatitudeToMeters(lat2 - lat1), LongitudeToMeters(lat2, lon2 - lon1)) + Math.PI / 2) * (180.0 / Math.PI);

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

		/// <summary>
		/// Convert angle in degree for latitude into distance in meters.
		/// </summary>
		/// <param name="angle">Angle for latitude in degree.</param>
		/// <returns>Distance in meters.</returns>
		private double LatitudeToMeters(double degrees)
		{
			return degrees * LATITUDE_COEF;
		}

		/// <summary>
		/// Convert angle in degree for longitude into distance in meters.
		/// </summary>
		/// <param name="angle">Angle for longitude in degree.</param>
		/// <returns>Distance in meters.</returns>
		private double LongitudeToMeters(double latitude, double degrees)
		{
			return degrees * PI_180 * Math.Cos(latitude * PI_180) * 6367449;
		}

		/// <summary>
		/// Convert distance in meters to a latitude of a coordinate.
		/// </summary>
		/// <param name="meters">Distance in meters.</param>
		/// <returns>Degree in latitude direction.</returns>
		private double MetersToLatitude(double meters)
		{
			return meters * METER_COEF;
		}

		/// <summary>
		/// Convert distance in meters to a longitude of a coordinate.
		/// </summary>
		/// <param name="meters">Distance in meters.</param>
		/// <returns>Degree in longitude direction.</returns>
		private double MetersToLongitude(double latitude, double meters)
		{
			return meters / (PI_180 * Math.Cos(latitude * PI_180) * 6367449);
		}

		/// <summary>
		/// Convert radiant to angle.
		/// </summary>
		/// <param name="angle">Angle in radiant.</param>
		/// <returns>Angle in degree.</returns>
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
