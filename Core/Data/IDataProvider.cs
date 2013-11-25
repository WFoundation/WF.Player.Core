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
using System.Collections;

namespace WF.Player.Core.Data
{
    /// <summary>
    /// An executable construct that can generate data values and containers.
    /// </summary>
    interface IDataProvider
	{
        /// <summary>
        /// Executes this provider immediately, as an Action that does not return a result.
        /// </summary>
        /// <param name="args">The optional arguments to pass to the provider. Their types
		/// must be value types (string, double...) or IDataProvider, IDataContainer
		/// or WherigoObject.</param>
		/// <exception cref="InvalidOperationException">A problem occured while executing the
		/// underlying function.</exception>
        void Execute(params object[] args);

		/// <summary>
		/// Executes this provider immediately as a Function which returns at least one
		/// IDataContainer.
		/// </summary>
		/// <param name="args">The optional arguments to pass to the provider. Their types
		/// must be value types (string, double...) or IDataProvider, IDataContainer
		/// or WherigoObject.</param>
		/// <returns>The first instance of IDataContainer found among the returned results
		/// of the provider, or null if none were found.</returns>
		/// <exception cref="InvalidOperationException">A problem occured while executing the
		/// underlying function.</exception>
        IDataContainer FirstContainerOrDefault(params object[] args);

		/// <summary>
		/// Executes this provider immediately as a Function which returns at least one
		/// value of a particular type.
		/// </summary>
		/// <typeparam name="T">The type of value to get, among value types (string, double...)
		/// or IDataContainer, or IDataProvider.</typeparam>
		/// <param name="args">The optional arguments to pass to the provider. Their types
		/// must be value types (string, double...) or IDataProvider, IDataContainer
		/// or WherigoObject.</param>
		/// <returns>The first instance of <typeparamref name="T"/> found among the returned results
		/// of the provider, or the default for <typeparamref name="T"/> if none were found.</returns>
		/// <exception cref="InvalidOperationException">A problem occured while executing the
		/// underlying function.</exception>
        T FirstOrDefault<T>(params object[] args);

    }
}
