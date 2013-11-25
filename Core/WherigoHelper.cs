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
			IPlatformHelper platform = new DefaultPlatformHelper();
#if WINDOWS_PHONE
			platform = new WinPhonePlatformHelper();
#endif

			// Creates a new Engine for the platform.
			return new Engine(platform);
		}

		public static Cartridge InitAndStartCartridge(Engine engine, Stream cartridgeStream, string filename, long firstLat, long firstLon, long firstAlt, long accuracy)
		{
			// Boot Time: inits the cartridge and process position.
			Cartridge cart = new Cartridge(filename);

			engine.Init(cartridgeStream, cart);

			engine.RefreshLocation(firstLat, firstLon, firstAlt, accuracy);
			engine.RefreshHeading(0);

			// Run Time: the game starts.

			engine.Start();

			return cart;
		}
	}
}
