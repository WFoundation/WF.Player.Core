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

namespace WF.Player.Core
{
	/// <summary>
	/// A task that the player has to perform.
	/// </summary>
	public class Task : UIObject
	{

		#region Constructor

		internal Task(WF.Player.Core.Data.IDataContainer data, RunOnClick runOnClick)
			: base(data, runOnClick)
		{
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets a value indicating whether this <see cref="WF.Player.Core.Item"/> is active.
		/// </summary>
		/// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
		public bool Active {
			get {
				return DataContainer.GetBool("Active").Value;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="WF.Player.Core.Item"/> is complete.
		/// </summary>
		/// <value><c>true</c> if complete; otherwise, <c>false</c>.</value>
		public bool Complete {
			get {
                return DataContainer.GetBool("Complete").Value;
			}
		}

		/// <summary>
		/// Gets the CorrectState.
		/// </summary>
		/// <value>The CorrectState.</value>
		public TaskCorrectness CorrectState {
			get {
				return DataContainer.GetEnum<TaskCorrectness>("CorrectState", TaskCorrectness.None).Value;
			}
		}

		#endregion

	}

}
