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
using System.Linq;
using System.Reflection;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace WF.Player.Core.Engines
{
	/// <summary>
	/// A standard Android implementation of IPlatformHelper.
	/// </summary>
	public class AndroidPlatformHelper : IPlatformHelper
	{
		static PackageInfo pInfo;

		#region Constructors

		public AndroidPlatformHelper(Context context)
		{
			pInfo = context.PackageManager.GetPackageInfo(context.PackageName,PackageInfoFlags.Activities);
		} 
		#endregion

		#region Members

		public Activity Ctrl;
		private static Version EntryAssemblyVersion; 

		#endregion

		#region Properties

		public virtual string CartridgeFolder
		{
			get { return "/"; }
		}

		public virtual string SavegameFolder
		{
			get { return "/Savegames"; }
		}

		public virtual string LogFolder
		{
			get { return "/Log"; }
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
			get { return System.IO.Path.DirectorySeparatorChar.ToString(); }
		}

		public string Platform
		{
				get { return global::System.Environment.OSVersion.Platform.ToString(); }
		}

		public string Device
		{
			get
			{
				return String.Format(
					"Android {0}/{1}",
					Build.VERSION.Release,
					Build.Model);
			}
		}

		public string DeviceId
		{
			get
			{
				// TODO: Insert right DeviceId
				return "Unknown";
			}
		}

		public virtual string ClientVersion
		{
			get
			{
				return String.Format("WF.Player.Android {0}.{1}", pInfo.VersionName, pInfo.VersionCode);
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
			if (Ctrl != null)
				Ctrl.RunOnUiThread(action);
		} 

		#endregion
	}
}
