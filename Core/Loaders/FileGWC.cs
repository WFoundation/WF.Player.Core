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
#if SILVERLIGHT
using System.Reflection; 
#endif

/// <summary>
/// File format of GWC files:
/// 
///	@0000:							; Signature
///		BYTE	 0x02
///		BYTE	 0x0a
///		BYTE	 "CART"
///		BYTE	 0x00
///
///	@0007:
///		USHORT	 NumberOfObjects	; Number of objects ("media files") in cartridge:
///
///	@0009:
///		; References to individual objects in cartridge.
///		; Object 0 is always Lua bytecode for cartridge.
///		; There is exactly [number_of_objects] blocks like this:
///		repeat <NumberOfObjects> times
///		{
///			USHORT	 ObjectID		; Distinct ID for each object, duplicates are forbidden
///			INT		 Address		; Address of object in GWC file
///		}
///
/// @xxxx:	 						; 0009 + <NumberOfObjects> * 0006 bytes from begining
///		; Header with all important informations for this cartridge
///		INT		 HeaderLength		; Length of information header (following block):
///
///		DOUBLE	 Latitude			; N+/S-
///		DOUBLE	 Longitude			; E+/W-
///		DOUBLE	 Altitude			; Meters
///
///		LONG	 Date of creation	; Seconds since 2004-02-10 01:00:00
///
///		; MediaID of icon and splashscreen
///		SHORT	 ObjectID of splashscreen	 ; -1 = without splashscreen/poster
///		SHORT	 ObjectID of icon			 ; -1 = without icon
///
///		ASCIIZ	 TypeOfCartridge			 ; "Tour guide", "Wherigo cache", etc.
///		ASCIIZ	 Player						 ; Name of player downloaded cartridge
///		LONG	 PlayerID					 ; ID of player in the Groundspeak database
///
///		ASCIIZ	 CartridgeName				 ; "Name of this cartridge"
///		ASCIIZ	 CartridgeGUID
///		ASCIIZ	 CartridgeDescription		 ; "This is a sample cartridge"
///		ASCIIZ	 StartingLocationDescription ; "Nice parking"
///		ASCIIZ	 Version					 ; "1.2"
///		ASCIIZ	 Author						 ; Author of cartridge
///		ASCIIZ	 Company					 ; Company of cartridge author
///		ASCIIZ	 RecommendedDevice			 ; "Garmin Colorado", "Windows PPC", etc.
///
///		INT		 Length						 ; Length of CompletionCode
///		ASCIIZ	 CompletionCode				 ; Normally 15/16 characters
///
/// @address_of_FIRST_object (with ObjectID = 0):
///		; always Lua bytecode
///		INT		 Length						 ; Length of Lua bytecode
///		BYTE[Length]	ContentOfObject		 ; Lua bytecode
///
///	@address_of_ALL_OTHER_objects (with ID > 0):
///		BYTE	 ValidObject
///		if (ValidObject == 0)
///		{
///			; when ValidObject == 0, it means that object is DELETED and does
///			; not exist in cartridge. Nothing else follows.
///		}
///		else
///		{
///			INT		ObjectType				 ; 1=bmp, (2=png?), 3=jpg, 4=gif, 17=wav, 18=mp3, 19=fdl, other values have unknown meaning
///			INT	 	Length
///			BYTE[Length]	content_of_object
///		}
///
///	@end
///
/// Varibles
/// 
///		BYTE   = unsigned char (1 byte)
///		SHORT  = signed short (2 bytes)
///		USHORT = unsigned short (2 bytes)
///		INT	   = signed long (4 bytes)
///		UINT   = unsigned long (4 bytes)
///		LONG   = signed long (8 bytes)
///		DOUBLE = double-precision floating point number (8 bytes)
///		ASCIIZ = zero-terminated string ("hello world!", 0x00)
///
/// </summary>
namespace WF.Player.Core
{

	public class FileGWC
	{

		/// <summary>
		/// Determines, if stream contains a valid GWC file.
		/// </summary>
		/// <returns><c>true</c> if is valid GWC file; otherwise, <c>false</c>.</returns>
		/// <param name="inputStream">Stream with cartridge file.</param>
		public static bool IsValidFile(Stream inputStream)
		{
			// If stream is shorter than 7 bytes, that could not a valid GWC file
			if (inputStream.Length < 7)
				return false;

			BinaryReader reader = new BinaryReader (inputStream);

			// Save old position of stream
			var oldPos = inputStream.Position;

			// Read first 7 bytes
			inputStream.Position = 0;
			byte[] b = reader.ReadBytes(7);

			// Go back to old position
			inputStream.Position = oldPos;

			// Signature of the compiled gwc file
			byte[] signature = { 0x02, 0x0a, 0x43, 0x41, 0x52, 0x54, 0x00 };

			return b.SequenceEqual<byte>(signature);
		}

		/// <summary>
		/// Load whole GWC file into an Cartridge object.
		/// </summary>
		/// <param name="cart">Cartridge object to file with data.</param>
		public static void Load(Stream inputStream, Cartridge cart)
		{
			try
			{
				// Open binary reader for reading the gwc file
				using (BinaryReader reader = new BinaryReader(inputStream))
				{
					// Loads the media file offsets.
					Dictionary<int, int> objects;
					int maxMediaFiles = loadGWCOffsets(reader, out objects);

					// Loads header
					loadGWCHeader(reader, cart);

					// Create array for resources
					cart.Resources = new Media[maxMediaFiles];

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
				}
			}
			catch (Exception e)
			{
				throw new Exception(String.Format ("An exception has occurred while reading the GWC file: {0}.",e.Message), e);
			}
		}

		/// <summary>
		/// Load only header data of a GWC file into an Cartridge object.
		/// </summary>
		/// <param name="cart">Cartridge object to file with data.</param>
		public static void LoadHeader(Stream inputStream, Cartridge cart)
		{
			try
			{
				// Open binary reader for reading stream
				using (BinaryReader reader = new BinaryReader(inputStream))
				{
					loadGWCHeader(reader, cart);
				}

			}
			catch (Exception e)
			{
				throw new Exception("An exception has occurred while reading the GWC file header.", e);
			}
		}

		#region Private Functions

		private static void loadGWCHeader(BinaryReader reader, Cartridge cart)
		{
			int maxMediaFiles;
			int posterId;
			int iconId;

			Dictionary<int, int> objects;

			// Reads the file offsets.
			maxMediaFiles = loadGWCOffsets(reader, out objects);

			// Reads header length.
			int headerLength = reader.ReadInt32();

			// Reads the starting location.
			cart.StartingLocationLatitude = reader.ReadDouble();
			cart.StartingLocationLongitude = reader.ReadDouble();
			cart.StartingLocationAltitude = reader.ReadDouble();

			// The cartridge is playable anywhere if both its starting location coordinates are 360.
			if (cart.StartingLocationLatitude == 360 && cart.StartingLocationLongitude == 360)
			{
				cart.IsPlayAnywhere = true;
			}

			// Dates are in seconds beyond 2004-02-10 01:00 (it's a palindrom, if you write it as 10-02-2004 ;-) )
			long seconds = reader.ReadInt64 ();

			cart.CreateDate = new DateTime (2004, 02, 10, 01, 00, 00);

			if (seconds != 0)
				cart.CreateDate = cart.CreateDate.AddSeconds (seconds);

			// Load ids for poster and incon
			posterId = reader.ReadInt16();
			iconId = reader.ReadInt16();

			// Get activity as string and convert it to type ActivityType
			string activity = readCString(reader);
			#if SILVERLIGHT
			int enumLength = 0;
			foreach (FieldInfo fi in cart.ActivityType.GetType().GetFields(BindingFlags.Static | BindingFlags.Public))
				enumLength++;
			#else
			int enumLength = Enum.GetValues(cart.ActivityType.GetType()).Length;
			#endif
			for (int i = 0; i < enumLength; i++)
				if (((LiveAPI.WherigoCartridge.ActivityTypes)i).ToString().Equals(activity))
					cart.ActivityType = (LiveAPI.WherigoCartridge.ActivityTypes)i;

			// Read name of player
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

			// Saves the current reader position.
			long oldReaderPosition = reader.BaseStream.Position;

			// Read poster
			if (posterId > 0 && posterId < maxMediaFiles)
				cart.Poster = loadGWCMedia(reader, objects[posterId], posterId, maxMediaFiles);

			// Read icon
			if (iconId > 0 && iconId < maxMediaFiles)
				cart.Icon = loadGWCMedia(reader, objects[iconId], iconId, maxMediaFiles);

			// Restores the reader position
			reader.BaseStream.Position = oldReaderPosition;
		}

		/// <summary>
		/// Tries to load a GWC Media file only if its id is valid.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="position"></param>
		/// <param name="id"></param>
		/// <param name="maxMediaFiles"></param>
		/// <returns></returns>
		private static Media loadGWCMedia(BinaryReader reader, int position, int id, int maxMediaFiles)
		{
			Media media = new Media();

			reader.BaseStream.Position = position;
			readMedia(media, reader);

			return media;
		}

		/// <summary>
		/// Loads the GWC media files' offsets.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="offsets">A dictionary of key/value pairs describing for each Id of a file,
		/// the position in the GWC file where it can be found.</param>
		/// <returns>The maximum of Media files the GWC file contains.</returns>
		private static int loadGWCOffsets(BinaryReader reader, out Dictionary<int, int> offsets)
		{
			// Positions the stream right after the file descriptor.
			reader.BaseStream.Position = 7;

			// Max number of objects in cartridge file
			int maxMediaFiles = (int)reader.ReadInt16();

			// Create table for objects in cartridge files
			offsets = new Dictionary<int, int>();

			// Read table
			for (int i = 0; i < maxMediaFiles; i++)
			{
				short id = reader.ReadInt16();
				int position = reader.ReadInt32();
				offsets[(int)id] = position;
			}

			return maxMediaFiles;
		}

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
				media.Type = (MediaType)Enum.ToObject (typeof(MediaType), input.ReadInt32());
				// Read resources data
				long fileSize = input.ReadInt32();
				media.Data = new byte[fileSize];
				input.Read(media.Data, 0, media.Data.Length);
			}
		}

		#endregion

	}

}

