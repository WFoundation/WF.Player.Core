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
using WF.Player.Core.Engines;
using WF.Player.Core.Data;

namespace WF.Player.Core
{
	public class WherigoObject
	{
		#region Fields

		private IDataContainer _dataContainer;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets this object's data container.
		/// </summary>
		internal IDataContainer DataContainer
		{
			get
			{
				return _dataContainer;
			}

			set
			{
				_dataContainer = value;

				OnDataContainerChanged(value);
			}
		}

		#endregion

		#region Constructor

		internal WherigoObject(IDataContainer container)
		{
			_dataContainer = container;
		} 

		#endregion

		/// <summary>
		/// Called when this object's data container has changed.
		/// </summary>
		/// <param name="value">The new value of DataContainer, eventually null.</param>
		internal virtual void OnDataContainerChanged(IDataContainer newValue)
		{
			
		}
	}

}

