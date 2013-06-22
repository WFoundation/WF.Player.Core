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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WF.Player.Core
{
    class FileFormats
    {

        public enum FileType { ffUnknown, ffGWC, ffGWS, ffGWZ, ffWFC, ffWFZ };

        #region Public static functions

        /// <summary>
        /// Get FileFormat from a stream.
        /// </summary>
        /// <param name="inputStream">Stream, which holds the file to check.</param>
        /// <returns>FileFormat of given stream.</returns>
        public static FileType GetFileFormat(Stream inputStream)
        {
            // Open binary reader for reading stream
            BinaryReader reader = new BinaryReader(inputStream);

            if (inputStream.Length < 7)
                return FileType.ffUnknown;

            // Read first 7 bytes
            byte[] b = reader.ReadBytes(7);

            // Go back to start
            inputStream.Position = 0;

            // Signature of the compiled gwc file
            byte[] signature = { 0x02, 0x0a, 0x43, 0x41, 0x52, 0x54, 0x00 };

            if (b.SequenceEqual<byte>(signature))
                return FileType.ffGWC;

            return FileType.ffUnknown;
        }

        /// <summary>
        /// Get FileFormat from a file filename.
        /// </summary>
        /// <param name="filename">Filename with path of the file to check.</param>
        /// <returns>The FileFormat of the file in the given MemoryStream.</returns>
        public static FileType GetFileFormat(string filename)
        {
            if (File.Exists(filename))
                return GetFileFormat(new FileStream(filename,FileMode.Open));
            else
                return FileType.ffUnknown;
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
                case FileType.ffGWC:
                    LoadGWC(inputStream, cart);
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
                case FileType.ffGWC:
                    LoadGWCHeader(inputStream, cart);
                    break;
            }
        }

        /// <summary>
        /// Load whole GWC file into an Cartridge object.
        /// </summary>
        /// <param name="cart">Cartridge object to file with data.</param>
        public static void LoadGWC(Stream inputStream, Cartridge cart)
        {
            try
            {
                // Load header, if it is not done yet
                LoadGWCHeader(inputStream, cart);

                // Go back to start of stream
                inputStream.Position = 0;

                // Open binary reader for reading the gwc file
                BinaryReader reader = new BinaryReader(inputStream);

                // Number of media files
                int maxMediaFiles;

                // Dictionary for the object table
                Dictionary<int, int> objects = new Dictionary<int, int>();

                // Jump over signature.
                // We didn't have to check it, because it is allready done in LoadGWCHeader.
                reader.BaseStream.Position = 7;

                // Max number of objects in cartridge file
                maxMediaFiles = reader.ReadInt16();

                // Create array for resources
                cart.Resources = new Media[maxMediaFiles];

                // Create table for objects in cartridge files
                objects = new Dictionary<int, int>();

                // Read table
                for (int i = 0; i < maxMediaFiles; i++)
                {
                    short id = reader.ReadInt16();
                    int position = reader.ReadInt32();
                    objects.Add(id, position);
                }

                // Read Lua binary

                // Get pos of resources in stream, ...
                int pos = objects[0];
                // ... jump to this position ...
                reader.BaseStream.Position = pos;
                // ... and read resources
                long fileSize = reader.ReadInt32();
                cart.Resources[0] = new Media();
                cart.Resources[0].Data = new byte[fileSize];
                reader.Read(cart.Resources[0].Data, 0, cart.Resources[0].Data.Length);

                // Read all other resources
                for (int i = 1; i < maxMediaFiles; i++)
                {
                    // Get pos of resources in stream, ...
                    pos = objects[i];
                    // ... jump to this position ...
                    reader.BaseStream.Position = pos;
                    // ... and read resources
                    cart.Resources[i] = new Media();
                    readMedia(cart.Resources[i], reader);
                }

                reader.Close();
            }
            catch (Exception e)
            {
                throw e;
            } 
        }

        /// <summary>
        /// Load only header data of a GWC file into an Cartridge object.
        /// </summary>
        /// <param name="cart">Cartridge object to file with data.</param>
        public static void LoadGWCHeader(Stream inputStream, Cartridge cart)
        {
            int maxMediaFiles;
            int posterId;
            int iconId;

            Dictionary<int, int> objects = new Dictionary<int, int>();

            if (GetFileFormat(inputStream) != FileType.ffGWC)
                throw new Exception(@"Invalid file format");

            // Open binary reader for reading stream
            BinaryReader reader = new BinaryReader(inputStream);

            try
            {
                // Max number of objects in cartridge file
                maxMediaFiles = reader.ReadInt16();

                // Create table for objects in cartridge files
                objects = new Dictionary<int, int>();

                // Read table
                for (int i = 0; i < maxMediaFiles; i++)
                {
                    short id = reader.ReadInt16();
                    int position = reader.ReadInt32();
                    objects.Add(id, position);
                }

                // Read header
                reader.ReadInt32();
                cart.SetStartingLocation(reader.ReadDouble(), reader.ReadDouble());
                cart.StartingLocationAltitude = reader.ReadDouble();
                // Dates are in seconds beyond 2004-02-10 01:00 (it's a palindrom, if you write it as 10-02-2004 ;-) )
                cart.CreateDate = new DateTime(2004, 02, 10, 01, 00, 00).AddSeconds(reader.ReadInt64());
                posterId = reader.ReadInt16();
                iconId = reader.ReadInt16();
                string activity = readCString(reader);
                for (int i = 0; i < Enum.GetValues(cart.ActivityType.GetType()).Length; i++)
                    if (((API.WherigoCartridge.ActivityTypes)i).ToString().Equals(activity))
                        cart.ActivityType = (API.WherigoCartridge.ActivityTypes)i;
                cart.Player = readCString(reader);
                reader.ReadInt32();
                reader.ReadInt32();
                cart.Name = readCString(reader);
                cart.Guid = readCString(reader);
                cart.LongDescription = readCString(reader);
                cart.StartingDescription = readCString(reader);
                cart.Version = readCString(reader);
                cart.AuthorName = readCString(reader);
                cart.AuthorCompany = readCString(reader);
                cart.Device = readCString(reader);
                reader.ReadInt32();
                cart.CompletionCode = readCString(reader);

                // Read poster
                if (posterId > -1 && posterId < maxMediaFiles)
                {
                    cart.Poster = new Media();

                    reader.BaseStream.Position = objects[posterId];
                    readMedia(cart.Poster, reader);
                }

                // Read icon
                if (iconId > -1 && iconId < maxMediaFiles)
                {
                    cart.Icon = new Media();

                    reader.BaseStream.Position = objects[iconId];
                    readMedia(cart.Icon, reader);
                }

                reader.Close();

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Load a whole GWC file into a Cartridge object.
        /// </summary>
        /// <param name="cart">Cartridge object to file with data.</param>
        public static void LoadGWZ(Stream inputStream, Cartridge cart)
        {
            throw new NotImplementedException(@"FileFormats.LoadGWZ is not implemented yet.");
        }

        /// <summary>
        /// Load only header data of a GWZ file into a Cartridge object.
        /// </summary>
        /// <param name="cart">Cartridge object to file with data.</param>
        public static void LoadGWZHeader(Stream inputStream, Cartridge cart)
        {
            throw new NotImplementedException(@"FileFormats.LoadGWZHeader is not implemented yet.");
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

        #region Private static functions

        /// <summary>
        /// Read a null terminated string from binary stream.
        /// </summary>
        /// <param name="reader">Binary stream with gwc file as input.</param>
        /// <returns>String, which represents the C# string.</returns>
        private static string readCString(BinaryReader input)
        {
            var bytes = new List<byte>();
            byte b;

            while ((b = input.ReadByte()) != 0)
            {
                bytes.Add(b);
            }

            return Encoding.UTF8.GetString(bytes.ToArray(), 0, bytes.ToArray().Length);
        }

        /// <summary>
        /// Read data for the next media from binary stream.
        /// </summary>
        /// <param name="media"></param>
        /// <param name="input"></param>
        private static void readMedia(Media media, BinaryReader input)
        {
            byte valid = input.ReadByte();
            if (valid == 0)
            {
                // No resources 
                media.Type = 0;
                media.Data = null;
            }
            else
            {
                // Read resources type
                media.Type = input.ReadInt32();
                // Read resources data
                long fileSize = input.ReadInt32();
                media.Data = new byte[fileSize];
                input.Read(media.Data, 0, media.Data.Length);
            }
        }

        #endregion

    }
}
