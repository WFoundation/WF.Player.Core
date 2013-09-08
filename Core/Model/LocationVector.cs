using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

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
