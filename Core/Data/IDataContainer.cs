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
using System.Collections;

namespace WF.Player.Core.Data
{
	/// <summary>
	/// A container of simple and complex data constructs that indexes data using strings.
	/// </summary>
	interface IDataContainer : IEnumerable
	{
		#region Properties

		/// <summary>
		/// Gets how many elements this container has.
		/// </summary>
		int Count { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Gets a boolean for a particular key.
		/// </summary>
		/// <param name="key">The key to query for.</param>
		/// <returns>Null if the value was not found or is not a boolean, a boolean otherwise.</returns>
		bool? GetBool(string key);

		/// <summary>
		/// Gets a byte array for a particular key.
		/// </summary>
		/// <param name="key">The key to query for.</param>
		/// <returns>Null if the value was not found or is not a byte array, a byte array 
		/// otherwise.</returns>
		byte[] GetByteArray(object key);

		/// <summary>
		/// Gets a data container for a particular key.
		/// </summary>
		/// <param name="key">The key to query for.</param>
		/// <returns>Null if the value was not found or is not a IDataContainer, a IDataContainer otherwise.</returns>
		IDataContainer GetContainer(string key);

		/// <summary>
		/// Gets a double for a particular key.
		/// </summary>
		/// <param name="key">The key to query for.</param>
		/// <returns>Null if the value was not found or is not a double, a double otherwise.</returns>
		double? GetDouble(string key);

		/// <summary>
		/// Gets an enumerated value for a particular key.
		/// </summary>
		/// <typeparam name="E">Type of the Enum to get.</typeparam>
		/// <param name="key">The key to query for.</param>
		/// <param name="defaultValue">The default value to return if none were found for the key.</param>
		/// <returns><paramref name="defaultValue"/> if the value for the key was not found or could not be 
		/// converted to an instance of <typeparamref name="E"/>, a valid instance of
		/// <typeparamref name="E"/> otherwise.</returns>
		E? GetEnum<E>(string key, E? defaultValue = null) where E : struct, IConvertible;

		/// <summary>
		/// Gets an integer for a particular key.
		/// </summary>
		/// <param name="key">The key to query for.</param>
		/// <returns>Null if the value was not found or is not an integer, an integer 
		/// otherwise.</returns>
		int? GetInt(string key);

		/// <summary>
		/// Gets a list of values for a particular key.
		/// </summary>
		/// <typeparam name="T">Type of values to return, among value types (string, double...)
		/// and IDataContainer, IDataProvider.</typeparam>
		/// <param name="key">The key to query for.</param>
		/// <returns>An enumeration of values of type <typeparamref name="T"/>, computed
		/// from a container existing at key <paramref name="key"/>. If no container
		/// is present at this key, or no element of type <typeparamref name="T"/>
		/// could be found inside, a non-null but empty enumeration is returned.</returns>
		IEnumerable<T> GetList<T>(string key);

		/// <summary>
		/// Gets a list of values from a provider which exists for a particular key.
		/// </summary>
		/// <typeparam name="T">Type of values to return, among value types (string, double...)
		/// and IDataContainer, IDataProvider.</typeparam>
		/// <param name="key">The key to query for.</param>
		/// <returns>An enumeration of values of type <typeparamref name="T"/>, computed
		/// from a provider existing at key <paramref name="key"/>. If no provider
		/// is present at this key, or no element of type <typeparamref name="T"/>
		/// could be returned by it, a non-null but empty enumeration is returned.</returns>
        IEnumerable<T> GetListFromProvider<T>(string key, params object[] parameters);

		/// <summary>
		/// Gets a data provider for a particular key.
		/// </summary>
		/// <param name="key">The key to query for.</param>
		/// <returns>Null if the value was not found or is not a IDataProvider, a IDataProvider 
		/// otherwise.</returns>
		IDataProvider GetProvider(string key);

		/// <summary>
		/// Gets a string for a particular key.
		/// </summary>
		/// <param name="key">The key to query for.</param>
		/// <returns>Null if the value was not found or is not a string, a string 
		/// otherwise.</returns>
		string GetString(string key);

		/// <summary>
		/// Gets a Wherigo object for a particular key.
		/// </summary>
		/// <typeparam name="W">Type of the WherigoObject to get.</typeparam>
		/// <param name="key">The key to query for.</param>
		/// <returns>Null if the value was not found or is not a WherigoObject of type 
		/// <typeparamref name="W"/>, a <typeparamref name=">"/> otherwise.</returns>
		W GetWherigoObject<W>(string key) where W : WherigoObject;

		/// <summary>
		/// Gets a list of Wherigo object for a particular key.
		/// </summary>
		/// <typeparam name="W">Type of the WherigoObject to get.</typeparam>
		/// <param name="key">The key to query for.</param>
		/// <returns>An enumeration of values of type <typeparamref name="W"/>, computed
		/// from a container existing at key <paramref name="key"/>. If no container
		/// is present at this key, or no element of type <typeparamref name="W"/>
		/// could be found inside, a non-null but empty enumeration is returned.</returns>
        WherigoCollection<W> GetWherigoObjectList<W>(string key) where W : WherigoObject;

		/// <summary>
		/// Gets a list of Wherigo objects from a provider which exists for a particular key.
		/// </summary>
		/// <typeparam name="W">Type of the WherigoObject to get.</typeparam>
		/// <param name="key">The key to query for.</param>
		/// <param name="parameters">Optional parameters to pass to the provider to execute.</param>
		/// <returns>An enumeration of values of type <typeparamref name="W"/>, computed
		/// from a provider existing at key <paramref name="key"/>. If no provider
		/// is present at this key, or no element of type <typeparamref name="W"/>
		/// could be returned, a non-null but empty enumeration is returned.</returns>
        WherigoCollection<W> GetWherigoObjectListFromProvider<W>(string key, params object[] parameters) where W : WherigoObject;

		#endregion
	}
}
