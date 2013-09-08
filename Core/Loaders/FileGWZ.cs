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

#if WINDOWS_PHONE
using System;
using System.IO;
namespace WF.Player.Core
{
	public class FileGWZ
	{
		/// <summary>
		/// Determines, if stream contains a valid GWZ file.
		/// </summary>
		/// <returns><c>true</c> if is valid GWZ file; otherwise, <c>false</c>.</returns>
		/// <param name="inputStream">Stream with cartridge file.</param>
		public static bool IsValidFile(Stream inputStream)
		{
			throw new NotImplementedException(@"FileGWZ.IsValidFile is not implemented yet.");
		}

		/// <summary>
		/// Load a whole GWZ file into a Cartridge object.
		/// </summary>
		/// <param name="cart">Cartridge object to file with data.</param>
		public static void Load(Stream inputStream, Cartridge cart)
		{
			throw new NotImplementedException(@"FileGWZ.Load is not implemented yet.");
		}

		/// <summary>
		/// Load only header data of a GWZ file into a Cartridge object.
		/// </summary>
		/// <param name="cart">Cartridge object to file with data.</param>
		public static void LoadHeader(Stream inputStream, Cartridge cart)
		{
			throw new NotImplementedException(@"FileGWZ.LoadHeader is not implemented yet.");
		}
	}

}
#else
using System;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace WF.Player.Core
{
	public class FileGWZ
	{

		/// <summary>
		/// Determines, if stream contains a valid GWZ file.
		/// </summary>
		/// <returns><c>true</c> if is valid GWZ file; otherwise, <c>false</c>.</returns>
		/// <param name="inputStream">Stream with cartridge file.</param>
		public static bool IsValidFile(Stream inputStream)
		{
			// If stream is shorter than 7 bytes, that could not a valid GWC file
			if (inputStream.Length < 2)
				return false;

			BinaryReader reader = new BinaryReader(inputStream);

			// Save old position of stream
			var oldPos = inputStream.Position;

			// Read first 2 bytes
			inputStream.Position = 0;
			byte[] b = reader.ReadBytes(2);

			// Go back to old position
			inputStream.Position = oldPos;

			// Signature of the ZIP file ("PK")
			byte[] signature = { 0x50, 0x4b };

			if (!b.SequenceEqual<byte>(signature))
				return false;

			ZipInputStream zipInputStream = new ZipInputStream(inputStream);
			ZipFile zf = new ZipFile(inputStream);
			foreach(ZipEntry ze in zf)
			{
				String entryFileName = ze.Name;

				if (Path.GetExtension(entryFileName).Equals("lua"))
					// Lua file exists, so it should be a valid GWZ file
					return true;
			}

			return false;
		}
		
		/// <summary>
		/// Load a whole GWZ file into a Cartridge object.
		/// </summary>
		/// <param name="cart">Cartridge object to file with data.</param>
		public static void Load(Stream inputStream, Cartridge cart)
		{
			throw new NotImplementedException(@"FileGWZ.Load is not implemented yet.");
		}

		/// <summary>
		/// Load only header data of a GWZ file into a Cartridge object.
		/// </summary>
		/// <param name="cart">Cartridge object to file with data.</param>
		public static void LoadHeader(Stream inputStream, Cartridge cart)
		{
			throw new NotImplementedException(@"FileGWZ.LoadHeader is not implemented yet.");
		}

	}
}


#endif