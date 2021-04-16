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
/// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WF.Player.Core.Formats
{
    public static class CartridgeLoaders
    {
		#region Fields

		private static List<ICartridgeLoader> _loaders;

		#endregion

		#region Constructors

		static CartridgeLoaders()
		{
			_loaders = new List<ICartridgeLoader>();

			_loaders.Add(new GWC());
		}

		#endregion

        #region Public static functions

        /// <summary>
        /// Get FileFormat from a stream.
        /// </summary>
        /// <param name="inputStream">Stream, which holds the file to check.</param>
        /// <returns>FileFormat of given stream.</returns>
		public static CartridgeFileFormat GetFileFormat(Stream inputStream)
        {
			CartridgeFileFormat current = CartridgeFileFormat.Unknown;

			foreach (ICartridgeLoader loader in _loaders)
			{
				// Gets the file format.
				current = loader.GetFileFormat(inputStream);

				// Returns it if it's not unknown.
				if (current != CartridgeFileFormat.Unknown)
					return current;
			}

			return CartridgeFileFormat.Unknown;
        }

        /// <summary>
        /// Get FileFormat from a file filename.
        /// </summary>
        /// <param name="filename">Filename with path of the file to check.</param>
        /// <returns>The FileFormat of the file in the given MemoryStream.</returns>
		public static CartridgeFileFormat GetFileFormat(string filename)
        {
            if (File.Exists(filename))
                return GetFileFormat(new FileStream(filename,FileMode.Open));
            else
				return CartridgeFileFormat.Unknown;
        }

        /// <summary>
        /// Load a cartridge file of unknown type into an Cartridge object.
        /// </summary>
        /// <param name="inputStream">Stream containing the cartridge file.</param>
        /// <param name="cart">Cartridge object, which should be used.</param>
        public static void Load(Stream inputStream, Cartridge cart)
        {
            // Gets the first loader that can load the stream.
			CartridgeFileFormat fFormat = GetFileFormat(inputStream);
			ICartridgeLoader loader = _loaders.FirstOrDefault(icl => icl.CanLoad(fFormat));

			if (loader == null)
				throw new InvalidOperationException("The file format is not supported.");

			// Loads the cartridge.
			loader.Load(inputStream, cart);
        }

        /// <summary>
        /// Load a cartridge file of unknown type.
        /// </summary>
        /// <param name="filename">Path to the file to load.</param>
        public static Cartridge Load(string filename)
        {
            // Creates a cartridge object for that path.
            string path = Path.GetFullPath(filename);
            Cartridge cart = new Cartridge(path);

            // Loads the file into the cartridge.
            using (FileStream fs = File.OpenRead(path))
            {
                Load(fs, cart);
            }

            // Returns the cartridge object now it's loaded.
            return cart;
        }

        /// <summary>
        /// Load the header of a cartridge file of unknown type into a Cartridge object.
        /// </summary>
        /// <param name="inputStream">Stream containing the cartridge file.</param>
        /// <param name="cart">Cartridge object, which should be used.</param>
        public static void LoadMetadata(Stream inputStream, Cartridge cart)
        {
			// Gets the first loader that can load the stream.
			CartridgeFileFormat fFormat = GetFileFormat(inputStream);
			ICartridgeLoader loader = _loaders.FirstOrDefault(icl => icl.CanLoad(fFormat));

			if (loader == null)
				throw new InvalidOperationException("The file format is not supported.");

			// Loads the cartridge.
			loader.LoadMetadata(inputStream, cart);
        }

        #endregion

    }
}
