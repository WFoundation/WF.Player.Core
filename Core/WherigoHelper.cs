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
using WF.Player.Core.Engines;
using System.IO;

namespace WF.Player.Core
{
	public static class WherigoHelper
	{
		/// <summary>
		/// Creates an instance of Engine for the current platform
		/// </summary>
		/// <returns></returns>
		public static Engine CreateEngine()
		{
			// Chooses one of the standard platform helpers.
			IPlatformHelper platform = null;
#if WINDOWS_PHONE
			platform = new WinPhonePlatformHelper();
#elif __ANDROID__
			platform = new AndroidPlatformHelper(null);
#elif __IOS__
			platform = new iOSPlatformHelper();
#else
            platform = new DefaultPlatformHelper();
#endif

			// Creates a new Engine for the platform.
			return new Engine(platform);
		}
	}
}
