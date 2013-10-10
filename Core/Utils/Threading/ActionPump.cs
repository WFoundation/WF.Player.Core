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

namespace WF.Player.Core.Utils.Threading
{
	/// <summary>
	/// A thread-safe queue of actions to be defered and executed on a trigger.
	/// </summary>
	internal class ActionPump : JobQueue
	{			
		#region Properties

		/// <summary>
		/// Gets or sets if this ActionPump is executing its job queue.
		/// </summary>
		public bool IsPumping
		{
			get
			{
				return IsActive;
			}

			set
			{
				IsActive = value;
			}
		}

		#endregion

		#region Constructors and Destructors

		/// <summary>
		/// Creates an ActionPump that is initially not pumping.
		/// </summary>
		public ActionPump()
		{
			// JobQueue configuration.
			ContinuesOnCompletion = true;
			IsActive = false;
			DelayBetweenJobs = TimeSpan.FromMilliseconds(10);
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Adds an action on the queue without activating the pump up.
		/// </summary>
		/// <param name="action"></param>
		public void AcceptAction(Action action)
		{
			AcceptJob(action, IsPumping);
		}

		#endregion
	}
}
