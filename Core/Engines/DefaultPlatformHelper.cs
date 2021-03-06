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
using System.IO;

namespace WF.Player.Core.Engines
{
	internal class DefaultPlatformHelper : IPlatformHelper
	{
        /// <summary>
        /// A string which can be used for unknown data.
        /// </summary>
        public static string UnknownValue = "unknown";
        
        public string CartridgeFolder
		{
            get { return UnknownValue; }
		}

		public string SavegameFolder
		{
            get { return UnknownValue; }
		}

		public string LogFolder
		{
            get { return UnknownValue; }
		}

		public string Ok
		{
			get { return "Ok"; }
		}

		public string EmptyYouSeeListText
		{
			get { return "Nothing of interest"; }
		}

		public string EmptyInventoryListText 
		{
			get { return "No items"; }
		}

		public string EmptyTasksListText 
		{
			get { return "No new tasks"; }
		}

		public string EmptyZonesListText
		{
			get { return "Nowhere to go"; }
		}

		public string EmptyTargetListText 
		{
			get { return "Nothing available"; }
		}

		public string PathSeparator
		{
			get { return Path.DirectorySeparatorChar.ToString(); }
		}

		public string Platform
		{
            get { return UnknownValue; }
		}

		public string Device
		{
            get { return UnknownValue; }
		}

		public string DeviceId
		{
            get { return UnknownValue; }
		}

		public string ClientVersion
		{
            get { return UnknownValue; }
		}

		public bool CanDispatchOnUIThread
		{
			get { return false; }
		}

		public void BeginDispatchOnUIThread(Action action)
		{
			throw new NotImplementedException();
		}
	}
}
