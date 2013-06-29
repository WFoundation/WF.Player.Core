///
/// WF.Player.Core - A Wherigo Player Core for different platforms.
/// Copyright (C) 2012-2013  Dirk Weltz <web@weltz-online.de>
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

namespace WF.Player.Core
{
	public class FileWFC
	{
		/// <summary>
		/// Determines, if stream contains a valid WFC file.
		/// </summary>
		/// <returns><c>true</c> if is valid WFC file; otherwise, <c>false</c>.</returns>
		/// <param name="inputStream">Stream with cartridge file.</param>
		public static bool IsValidFile(Stream inputStream)
		{
			return false;
		}

		/// <summary>
		/// Load a whole WFC file into a Cartridge object.
		/// </summary>
		/// <param name="cart">Cartridge object to file with data.</param>
		public static void Load(Stream inputStream, Cartridge cart)
		{
			throw new NotImplementedException(@"FileWFC.Load is not implemented yet.");
		}

		/// <summary>
		/// Load only header data of a WFC file into a Cartridge object.
		/// </summary>
		/// <param name="cart">Cartridge object to file with data.</param>
		public static void LoadHeader(Stream inputStream, Cartridge cart)
		{
			throw new NotImplementedException(@"FileWFC.LoadHeader is not implemented yet.");
		}
	}
}