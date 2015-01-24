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

namespace WF.Player.Core.Formats
{
	/// <summary>
	/// A loader capable of reading Cartridges from data streams.
	/// </summary>
	public interface ICartridgeLoader
	{
		/// <summary>
		/// Determines if this ICartridgeLoader is capable of loading a certain
		/// file format.
		/// </summary>
		/// <param name="targetFileFormat">The file format to be tested.</param>
		/// <returns>True if this loader can correctly load the file format.</returns>
		bool CanLoad(CartridgeFileFormat targetFileFormat);
		
		/// <summary>
		/// Determines if a stream contains data that is conform to a certain
		/// cartridge file format.
		/// </summary>
		/// <param name="inputStream">The cartridge stream.</param>
		/// <param name="targetFormat">The file format to test against.</param>
		/// <returns>True if the file is conform to the format, false otherwise.</returns>
		/// <exception cref="InvalidOperationException">The file format is not supported
		/// by this ICartridgeLoader (CanLoad returns false).</exception>
		bool IsValidFile(Stream inputStream, CartridgeFileFormat targetFormat);

		/// <summary>
		/// Gets the exepected file format of a stream.
		/// </summary>
		/// <param name="inputStream">The stream to check.</param>
		/// <returns>A file format, supported by this ICartridgeLoader, that was
		/// recognized in the stream. If no supported format was recognized,
		/// CartridgeFileFormat.Unknown is returned.</returns>
		CartridgeFileFormat GetFileFormat(Stream inputStream);

		/// <summary>
		/// Loads fully the cartridge data from the stream into a Cartridge object,
		/// including metadata, code and resources.
		/// </summary>
		/// <param name="inputStream">The stream to parse.</param>
		/// <param name="cart">The cartridge object that will contain the data.</param>
		/// <exception cref="Exception">The underlying format of the stream is not
		/// supported by this loader.</exception>
		void Load(Stream inputStream, Cartridge cart);

		/// <summary>
		/// Loads only cartridge metadata from a stream into a Cartridge object.
		/// </summary>
		/// <param name="inputStream">The stream to parse.</param>
		/// <param name="cart">The cartridge object that will contain the data.</param>
		/// <exception cref="Exception">The underlying format of the stream is not
		/// supported by this loader.</exception>
		void LoadMetadata(Stream inputStream, Cartridge cart);
	}
}
