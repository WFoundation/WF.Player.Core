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

namespace WF.Player.Core.Data
{
	/// <summary>
	/// A factory of data containers, data providers and Wherigo objects.
	/// </summary>
	interface IDataFactory : IDisposable
	{
		/// <summary>
		/// Constructs a WherigoObject of a particular type.
		/// </summary>
		/// <typeparam name="W">Type of the WherigoObject to create.</typeparam>
		/// <param name="arguments">Optional arguments to pass to the underlying
		/// constructor of the object.</param>
		/// <returns>An instance of <typeparamref name="W"/>.</returns>
		/// <exception cref="InvalidOperationException">The created Wherigo object
		/// is not of type <typeparamref name="W"/> or another error occured while
		/// creating the instance.</exception>
		W CreateWherigoObject<W>(params object[] arguments) where W : WherigoObject;

		/// <summary>
		/// Constructs a WherigoObject of a particular classname.
		/// </summary>
		/// <param name="wClassname">Wherigo classname of the object to construct.
		/// (For instance, "ZThing", or "Distance").</param>
		/// <param name="arguments">Optional arguments to pass to the underlying
		/// constructor of the object.</param>
		/// <returns>An instance of WherigoObject.</returns>
		/// <exception cref="InvalidOperationException">An error occured while
		/// creating the instance.</exception>
        WherigoObject CreateWherigoObject(string wClassname, params object[] arguments);

		/// <summary>
		/// Gets the data container of a Wherigo object which has a particular
		/// object index.
		/// </summary>
		/// <param name="objIndex">Object index to query for.</param>
		/// <returns>The IDataContainer of the object.</returns>
		/// <exception cref="KeyNotFoundException">No object was found
		/// with such an index.</exception>
		/// <exception cref="InvalidOperationException">An error has occured
		/// while looking up.</exception>
		/// <exception cref="ArgumentException"><paramref name="objIndex"/> is
		/// smaller than 0.</exception>
        IDataContainer GetContainer(int objIndex);

		/// <summary>
		/// Gets the WherigoObject of a particular type that corresponds to
		/// a Wherigo entity in a data container.
		/// </summary>
		/// <remarks>If no such WherigoObject exists already, this method
		/// constructs a new one that uses the supplied data container.</remarks>
		/// <typeparam name="W">Type of the Wherigo object to retrieve.</typeparam>
		/// <param name="data">The container supporting a Wherigo entity..</param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">The container does not
		/// contain a Wherigo entity, or the created Wherigo object
		/// is not of type <typeparamref name="W"/> or another error occured while
		/// creating the instance.</exception>
		W GetWherigoObject<W>(IDataContainer data) where W : WherigoObject;

		/// <summary>
		/// Gets the WherigoObject of a particular type and which has a particular
		/// object index.
		/// </summary>
		/// <param name="objIndex">Object index to query for.</param>
		/// <typeparam name="W">Type of the Wherigo object to retrieve.</typeparam>
		/// <returns>An instance of WherigoObject, or null if none were found.</returns>
		/// <exception cref="KeyNotFoundException">No object was found
		/// with such an index.</exception>
		/// <exception cref="InvalidOperationException">An error has occured
		/// while looking up.</exception>
		W GetWherigoObject<W>(int objIndex) where W : WherigoObject;

		/// <summary>
		/// Gets a list of Wherigo objects from a data container.
		/// </summary>
		/// <typeparam name="W">Type of the WherigoObject to get.</typeparam>
		/// <returns>An enumeration of values of type <typeparamref name="W"/>, computed
		/// from the container. If no element of type <typeparamref name="W"/>
		/// could be found inside, a non-null but empty enumeration is returned.</returns>
        WherigoCollection<W> GetWherigoObjectList<W>(IDataContainer data) where W : WherigoObject;
	}
}
