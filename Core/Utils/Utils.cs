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
using System.Reflection;

namespace WF.Player.Core
{
	/// <summary>
	/// Defines utility methods.
	/// </summary>
	public static class Utils
	{
		/// <summary>
		/// Parses an Enum of a particular type and value.
		/// </summary>
		/// <typeparam name="T">Type of the Enum to get.</typeparam>
		/// <param name="fieldName">The string name of the enum value.</param>
		/// <param name="defaultValue">Default value for the enum if the field is not found. If null,
		/// an exception is thrown in place of returning the default value.</param>
		/// <returns></returns>
		public static T ParseEnum<T>(string fieldName, T? defaultValue) where T : struct, IConvertible
		{
			if (!typeof(T).IsEnum)
				throw new ArgumentException("T must be an enumerated type.");

			if (string.IsNullOrEmpty(fieldName))
			{
				if (!defaultValue.HasValue)
				{
					throw new ArgumentException("No such value for this enum.");
				}

				return defaultValue.Value;
			}

#if SILVERLIGHT
			foreach (FieldInfo fi in typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public))
			{
				string name = fi.Name;
				T item = (T)Enum.Parse(typeof(T), name, false);
#else
			foreach (T item in Enum.GetValues(typeof(T)))
			{
				string name = item.ToString();
#endif

				if (String.Equals(name, fieldName.Trim(), StringComparison.InvariantCultureIgnoreCase))
				{
					return item;
				}
			}

			if (!defaultValue.HasValue)
			{
				throw new ArgumentException("No such value for this enum.");
			}

			return defaultValue.Value;
		}
	}
}
