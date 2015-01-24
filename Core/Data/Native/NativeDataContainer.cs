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

namespace WF.Player.Core.Data.Native
{
	/// <summary>
	/// An implementation of IDataContainer that uses native .NET data 
	/// structures.
	/// </summary>
	internal class NativeDataContainer : IDataContainer
	{
		#region Fields

		private Dictionary<string, object> _entries; 

		#endregion
		
		public int Count
		{
			get { return _entries.Count; }
		}

		#region Indexers

		internal object this[string key]
		{
			set
			{
				_entries[key] = value;
			}
		}

		#endregion

		#region Constructors
		/// <summary>
		/// Constructs a new empty data container using native data
		/// structures.
		/// </summary>
		internal NativeDataContainer()
		{
			_entries = new Dictionary<string, object>();
		}
		#endregion

		#region IDataContainer
		public bool? GetBool(string key)
		{
			return GetFieldOrDefault<bool?>(key);
		}

		public IDataContainer GetContainer(string key)
		{
			return GetFieldOrDefault<IDataContainer>(key);
		}

		public double? GetDouble(string key)
		{
			return GetFieldOrDefault<double?>(key);
		}

		public E? GetEnum<E>(string key, E? defaultValue = null) where E : struct, IConvertible
		{
			return GetFieldOrDefault<E?>(key);
		}

		public int? GetInt(string key)
		{
			return GetFieldOrDefault<int?>(key);
		}

		public System.Collections.Generic.IEnumerable<T> GetList<T>(string key)
		{
			return GetFieldOrDefault<IEnumerable<T>>(key);
		}

		public System.Collections.Generic.IEnumerable<T> GetListFromProvider<T>(string key, params object[] parameters)
		{
			throw new NotImplementedException();
		}

		public IDataProvider GetProvider(string key)
		{
			return GetFieldOrDefault<IDataProvider>(key);
		}

		public string GetString(string key)
		{
			return GetFieldOrDefault<string>(key);
		}

		public W GetWherigoObject<W>(string key) where W : WherigoObject
		{
			return GetFieldOrDefault<W>(key);
		}

		public WherigoCollection<W> GetWherigoObjectList<W>(string key) where W : WherigoObject
		{
			throw new NotImplementedException();
		}

		public WherigoCollection<W> GetWherigoObjectListFromProvider<W>(string key, params object[] parameters) where W : WherigoObject
		{
			throw new NotImplementedException();
		}

		public System.Collections.IEnumerator GetEnumerator()
		{
			return _entries.GetEnumerator();
		} 
		#endregion

		private T GetFieldOrDefault<T>(string key)
		{
			// Gets the field.
			object obj;
			if (!_entries.TryGetValue(key, out obj) || !(obj is T))
			{
				return default(T);
			}

			return (T)obj;
		}
	}
}
