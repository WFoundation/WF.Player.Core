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
using System.Linq;
using System.Reflection;

namespace WF.Player.Core.Engines
{
	/// <summary>
	/// A standard Windows Phone implementation of IPlatformHelper.
	/// </summary>
	public class WinPhonePlatformHelper : IPlatformHelper
	{
		#region Constructors
		static WinPhonePlatformHelper()
		{
			try
			{
				_entryAssemblyVersion = Version.Parse(Assembly.GetExecutingAssembly()
						.GetCustomAttributes(false)
						.OfType<AssemblyFileVersionAttribute>()
						.First()
						.Version);
			}
			catch (Exception)
			{
				_entryAssemblyVersion = null;
			}
		} 
		#endregion

		#region Fields
		
		private static Version _entryAssemblyVersion; 

		#endregion

		#region Properties

		public virtual string CartridgeFolder
		{
			get { return "/Cartridges"; }
		}

		public virtual string SavegameFolder
		{
			get { return "/Savegames"; }
		}

		public virtual string LogFolder
		{
			get { return "/Log"; }
		}

        public virtual string Ok
        {
            get { return "Ok"; }
        }

        public string EmptyYouSeeListText
        {
            get { return "Empty"; }
        }

        public string EmptyInventoryListText
        {
            get { return "Empty"; }
        }

        public string EmptyTasksListText
        {
            get { return "Empty"; }
        }

        public string EmptyZonesListText
        {
            get { return "Empty"; }
        }

        public string EmptyTargetListText
        {
            get { return "Empty"; }
        }

		public string PathSeparator
		{
			get { return System.IO.Path.DirectorySeparatorChar.ToString(); }
		}

		public string Platform
		{
			get { return Environment.OSVersion.Platform.ToString(); }
		}

		public string Device
		{
			get
			{
				return String.Format(
					"Windows Phone {0}/{1}",
					Environment.OSVersion.Version.ToString(2),
					Microsoft.Phone.Info.DeviceStatus.DeviceManufacturer);
			}
		}

		public string DeviceId
		{
			get
			{
				object idHash;
				if (!Microsoft.Phone.Info.DeviceExtendedProperties.TryGetValue("DeviceUniqueId", out idHash))
				{
					return "Unknown";
				}

				return Convert.ToBase64String((byte[])idHash);
			}
		}

		public virtual string ClientVersion
		{
			get
			{
				return _entryAssemblyVersion != null ? _entryAssemblyVersion.ToString() : "Unknown";
			}

			// The value is set by the static constructor in order to catch the UI thread's 
			// calling assembly's version.
		}

		public bool CanDispatchOnUIThread
		{
			get { return true; }
		} 

		#endregion

		#region Methods

		public void BeginDispatchOnUIThread(Action action)
		{
			System.Windows.Deployment.Current.Dispatcher.BeginInvoke(action);
		} 

		#endregion
    }
}
