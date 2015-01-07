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

namespace WF.Player.Core.Engines
{
	/// <summary>
	/// Provides platform-specific information and operations that the engine depends on.
	/// </summary>
	public interface IPlatformHelper
	{
		#region Properties

		/// <summary>
		/// Gets the path to the folder that contains cartridges on this device.
		/// </summary>
		string CartridgeFolder { get; }

		/// <summary>
		/// Gets the path to the folder that contains savegames on this platform.
		/// </summary>
		string SavegameFolder { get; }

		/// <summary>
		/// Gets the path to the folder that contains log files on this platform.
		/// </summary>
		string LogFolder { get; }

		/// <summary>
		/// Gets the string representing the normal text for Ok buttons.
		/// </summary>
		string Ok { get; }

		/// <summary>
		/// Gets the string shown for empty You See list.
		/// </summary>
		string EmptyYouSeeListText { get; }

		/// <summary>
		/// Gets the string shown for empty Inventory list.
		/// </summary>
		string EmptyInventoryListText { get; }

		/// <summary>
		/// Gets the string shown for empty Tasks list.
		/// </summary>
		string EmptyTasksListText { get; }

		/// <summary>
		/// Gets the string shown for empty Zones list.
		/// </summary>
		string EmptyZonesListText { get; }

		/// <summary>
		/// Gets the string shown when there is no target for the command.
		/// </summary>
		string EmptyTargetListText { get; }

		/// <summary>
		/// Gets the character sequence representing a path separator on this platform.
		/// </summary>
		string PathSeparator { get; }

		/// <summary>
		/// Gets a human-readable description of this platform.
		/// </summary>
		string Platform { get; }

		/// <summary>
		/// Gets a human-readable description of the current device.
		/// </summary>
		string Device { get; }

		/// <summary>
		/// Gets a string that uniquely identifies the current device.
		/// </summary>
        /// <remarks>An approximate or bogus value can be returned if this information is
        /// too sensitive to be given for the current device.</remarks>
		string DeviceId { get; }

		/// <summary>
		/// Gets the version of the client application.
		/// </summary>
		string ClientVersion { get; }

		/// <summary>
		/// Gets if this platform helper is able to dispatch an action on the UI thread
		/// of this platform.
		/// </summary>
		bool CanDispatchOnUIThread { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Asynchronously executes an action on the UI thread of this platform.
		/// </summary>
		/// <remarks>
		/// <para>This method has several contracts:
		/// 1. It MUST return immediately after scheduling the action to be executed
		/// on the UI thread, without waiting for the action to even start.
		/// 2. It MUST NOT throw an exception if <code>CanDispatchOnUIThread</code>
		/// is <code>true</code>.
		/// 3. It MUST be thread-safe, because it will be called from various 
		/// threads.</para>
		/// </remarks>
		/// <param name="action">The action to be executed. The behavior if this parameter 
		/// is <code>null</code> is not specified.</param>
		void BeginDispatchOnUIThread(Action action);

		#endregion
	}
}
