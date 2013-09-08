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
/// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if SILVERLIGHT
using System.Reflection;
#endif

namespace WF.Player.Core
{
	public enum CartridgeFileTypes 
	{ 
		Unknown, 
		GWC, 
		GWS, 
		GWZ, 
		WFC, 
		WFZ 
	};

    class FileFormats
    {

        #region Public static functions

        /// <summary>
        /// Get FileFormat from a stream.
        /// </summary>
        /// <param name="inputStream">Stream, which holds the file to check.</param>
        /// <returns>FileFormat of given stream.</returns>
		public static CartridgeFileTypes GetFileFormat(Stream inputStream)
        {
			if (FileGWC.IsValidFile (inputStream))
				return CartridgeFileTypes.GWC;

			if (FileGWZ.IsValidFile (inputStream))
				return CartridgeFileTypes.GWZ;

			return CartridgeFileTypes.Unknown;
        }

        /// <summary>
        /// Get FileFormat from a file filename.
        /// </summary>
        /// <param name="filename">Filename with path of the file to check.</param>
        /// <returns>The FileFormat of the file in the given MemoryStream.</returns>
		public static CartridgeFileTypes GetFileFormat(string filename)
        {
            if (File.Exists(filename))
                return GetFileFormat(new FileStream(filename,FileMode.Open));
            else
				return CartridgeFileTypes.Unknown;
        }

        /// <summary>
        /// Load a cartridge file of unkown type into an Cartridge object.
        /// </summary>
        /// <param name="inputStream">Stream containing the cartridge file.</param>
        /// <param name="cart">Cartridge object, which should be used.</param>
        public static void Load(Stream inputStream, Cartridge cart)
        {
            switch (GetFileFormat(inputStream))
            {
				case CartridgeFileTypes.GWC:
                    FileGWC.Load(inputStream, cart);
                    break;
				case CartridgeFileTypes.GWZ:
					FileGWZ.Load(inputStream, cart);
					break;
         }
        }

        /// <summary>
        /// Load the header of a cartridge file of unkown type into an Cartridge object.
        /// </summary>
        /// <param name="inputStream">Stream containing the cartridge file.</param>
        /// <param name="cart">Cartridge object, which should be used.</param>
        public static void LoadHeader(Stream inputStream, Cartridge cart)
        {
            switch (GetFileFormat(inputStream))
            {
				case CartridgeFileTypes.GWC:
                    FileGWC.LoadHeader(inputStream, cart);
                    break;
				case CartridgeFileTypes.GWZ:
					FileGWZ.LoadHeader(inputStream, cart);
					break;
         }
        }

        /// <summary>
        /// Load a whole WFC file into a Cartridge object.
        /// </summary>
        /// <param name="cart">Cartridge object to file with data.</param>
        public static void LoadWFC(Stream inputStream, Cartridge cart)
        {
            throw new NotImplementedException(@"FileFormats.LoadWFC is not implemented yet.");
        }

        /// <summary>
        /// Load only header data of a WFC file into a Cartridge object.
        /// </summary>
        /// <param name="cart">Cartridge object to file with data.</param>
        public static void LoadWFCHeader(Stream inputStream, Cartridge cart)
        {
            throw new NotImplementedException(@"FileFormats.LoadWFCHeader is not implemented yet.");
        }

        /// <summary>
        /// Load a whole WFZ file into a Cartridge object.
        /// </summary>
        /// <param name="cart">Cartridge object to file with data.</param>
        public static void LoadWFZ(Stream inputStream, Cartridge cart)
        {
            throw new NotImplementedException(@"FileFormats.LoadWFZ is not implemented yet.");
        }

        /// <summary>
        /// Load only header data of a WFZ file into a Cartridge object.
        /// </summary>
        /// <param name="cart">Cartridge object to file with data.</param>
        public static void LoadWFZHeader(Stream inputStream, Cartridge cart)
        {
            throw new NotImplementedException(@"FileFormats.LoadWFZHeader is not implemented yet.");
        }

        #endregion

    }
}
