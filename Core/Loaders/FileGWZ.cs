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
using Ionic.Zip;
using System;
using System.IO;
using System.Linq;

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
			ZipEntry zipEntry = zipInputStream.GetNextEntry();
			while (zipEntry != null)
			{
				String entryFileName = zipEntry.FileName;
				if (Path.GetExtension(entryFileName).Equals("lua"))
					// Lua file exists, so it should be a valid GWZ file
					return true;
				zipEntry = zipInputStream.GetNextEntry();
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