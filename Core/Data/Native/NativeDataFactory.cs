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

namespace WF.Player.Core.Data.Native
{
	/// <summary>
	/// An implementation of IDataFactory that uses native .NET data 
	/// structures.
	/// </summary>
	internal class NativeDataFactory : IDataFactory
	{
		#region Fields

		private static NativeDataFactory _Instance;

		#endregion
		
		#region Properties

		internal static NativeDataFactory Instance
		{
			get
			{
				return _Instance ?? (_Instance = new NativeDataFactory());
			}
		}

		#endregion

		#region Constructor

		internal NativeDataFactory()
		{

		}

		#endregion

		#region IDataFactory
		public W CreateWherigoObject<W>(params object[] arguments) where W : WherigoObject
		{
			Type wherigoType = typeof(W);
			WherigoObject obj = null;

			// Conforms parameters.
			List<object> args = new List<object>();
			if (arguments != null)
			{
				args.AddRange(arguments);
			}

			// Creates the object if the requested type is supported.
			if (wherigoType == typeof(ZonePoint))
			{
				obj = CreateZonePoint(args);
			}
			else if (wherigoType == typeof(Distance))
			{
				obj = CreateDistance(args);
			}
			else
			{
				throw new NotSupportedException("Requested type is not supported.");
			}

			// Checks the type and returns the object.
			if (!(obj is W))
			{
				throw new InvalidOperationException(String.Format("Requested type {0}, got type {1}.", wherigoType.Name, obj.GetType().Name));
			}

			return (W) obj;
		}

		public WherigoObject CreateWherigoObject(string wClassname, params object[] arguments)
		{
			throw new NotImplementedException();
		}

		public IDataContainer GetContainer(int objIndex)
		{
			throw new NotImplementedException();
		}

		public W GetWherigoObject<W>(IDataContainer data) where W : WherigoObject
		{
			throw new NotImplementedException();
		}

		public W GetWherigoObject<W>(int objIndex) where W : WherigoObject
		{
			throw new NotImplementedException();
		}

		public WherigoCollection<W> GetWherigoObjectList<W>(IDataContainer data) where W : WherigoObject
		{
			throw new NotImplementedException();
		}

		#endregion

		private Distance CreateDistance(List<object> arguments)
		{
			// Conforms the parameters.
			double value = arguments.OfType<double>().FirstOrDefault();
			
			// Constructs the instance.
			return new Distance(value, DistanceUnit.Meters);
		}

		private ZonePoint CreateZonePoint(List<object> arguments)
		{
			// Conforms the parameters.
			IEnumerable<double> doubles = arguments.OfType<double>();
			double lat = doubles.FirstOrDefault();
			double lon = doubles.Skip(1).FirstOrDefault();
			double alt = doubles.Skip(2).FirstOrDefault();

			// Constructs the instance.
			return new ZonePoint(lat, lon, alt);
		}
	}
}
