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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using NLua;


namespace WF.Player.Core
{

    /// <summary>
	/// Class for objects to store informations for cartridges.
    /// </summary>
    #if MONOTOUCH
	    [MonoTouch.Foundation.Preserve(AllMembers=true)]
    #endif
	public class Cartridge : INotifyPropertyChanged
    {

        #region Private variables

        private bool complete;
        private DateTime completionTime = DateTime.MinValue;
		private Engine engine;
		private Media[] resources;
		private LuaTable wigTable;

        #endregion

        #region Public variables

        // Properties for C#
        public string Activity;
        public string Author;
		public DateTime CreateDate;
        public string Company;
        public string CompletionCode;
        public string Description;
        public string Device;
        public string Guid;
        public Media Icon;
        public string Name;
        public string Player;
        public Media Poster;
        public string StartingDescription;
        public double StartingLocationLatitude = 360.0;
        public double StartingLocationLongitude = 360.0;
        public double StartingLocationAltitude = 360.0;
        public string Version;
        public string Filename { get; internal set; }

        #endregion

        #region Constructor

        public Cartridge ( string filename )
		{
            // Save filename of the gwc file for later use
            Filename = filename;
        }

        #endregion

        #region C# Property

		/// <summary>
		/// Gets/sets the cartridge completion state.
		/// </summary>
		/// <value><c>true</c> if the cartridge is complete; otherwise, <c>false</c>.</value>
		public bool Complete {
			get {
				return complete;
			}
			set {
				if (complete != value)
					complete = value;
				if (complete && completionTime == DateTime.MinValue)
					completionTime = DateTime.Now;
			}
		}

		/// <summary>
		/// Gets the empty inventory list text.
		/// </summary>
		/// <value>The empty inventory list text.</value>
		public string EmptyInventoryListText {
			get {
				return GetString ("EmptyInventoryListText");
			}
		}

		/// <summary>
		/// Gets the empty tasks list text.
		/// </summary>
		/// <value>The empty tasks list text.</value>
		public string EmptyTasksListText {
			get {
				return GetString ("EmptyTasksListText");
			}
		}

		/// <summary>
		/// Gets the empty you see list text.
		/// </summary>
		/// <value>The empty you see list text.</value>
		public string EmptyYouSeeListText {
			get {
				return GetString ("EmptyYouSeeListText");
			}
		}

		/// <summary>
		/// Gets the empty zones list text.
		/// </summary>
		/// <value>The empty zones list text.</value>
		public string EmptyZonesListText {
			get {
				return GetString ("EmptyZonesListText");
			}
		}

		/// <summary>
		/// Sets the engine to which this cartridge belongs.
		/// </summary>
		/// <value>The engine.</value>
		public Engine Engine {
			set {
				engine = value;
			}
		}

		/// <summary>
		/// Gets the cartridge log filename with extension .gwl.
		/// </summary>
		/// <value>Cartridge log filename with extension gwl.</value>
		public string LogFilename {
			get {
				return Path.ChangeExtension (Filename, ".gwl");
			}
		}

		/// <summary>
		/// Gets the array with all resources, belonging to this catridge.
		/// </summary>
		/// <value>The resources.</value>
		public Media[] Resources {
			get {
				return resources;
			}
		}

		/// <summary>
		/// Gets the cartridge save filename with extension .gws.
		/// </summary>
		/// <value>Cartridge save filename with extension gws.</value>
		public string SaveFilename {
			get {
				return Path.ChangeExtension (Filename, ".gws");
			}
		}

		/// <summary>
		/// Gets or sets the Lua table representing the object on the Lua side.
		/// </summary>
		/// <value>The Lua table.</value>
		public LuaTable WIGTable {
			get {
				return wigTable;
			}
			set {
				if (wigTable != value)
					wigTable = value;
			}
		}
		
		#endregion

        #region C# Function for handling of cartridge outside the game

		/// <summary>
		/// Gets the boolean from LuaTable for entry key as string.
		/// </summary>
		/// <returns>The boolean.</returns>
		/// <param name="key">Key as string for the entry.</param>
		public bool GetBool(string key)
		{
			if (wigTable == null)
				return false;

			object value = wigTable [key];

			return value == null ? false : (bool)value;
		}

		/// <summary>
		/// Gets the boolean from LuaTable for entry key as number.
		/// </summary>
		/// <returns>The boolean.</returns>
		/// <param name="key">Key as number for the entry.</param>
		public bool GetBool(double key)
		{
			if (wigTable == null)
				return false;

			object value = wigTable [key];

			return value == null ? false : (bool)value;
		}

		/// <summary>
		/// Gets the double from LuaTable for entry key as string.
		/// </summary>
		/// <returns>The double.</returns>
		/// <param name="key">Key as string for the entry.</param>
		public double GetDouble(string key)
		{
			if (wigTable == null)
				return 0;

			object num = wigTable [key];

			return num == null ? 0 : (double)num;
		}

		/// <summary>
		/// Gets the double from LuaTable for entry key as number.
		/// </summary>
		/// <returns>The double.</returns>
		/// <param name="key">Key as number for the entry.</param>
		public double GetDouble(double key)
		{
			if (wigTable == null)
				return 0;

			object num = wigTable [key];

			return num == null ? 0 : (double)num;
		}

		/// <summary>
		/// Gets the integer from LuaTable for entry key as string.
		/// </summary>
		/// <returns>The integer.</returns>
		/// <param name="key">Key as string for the entry.</param>
		public int GetInt(string key)
		{
			if (wigTable == null)
				return 0;

			object num = wigTable [key];

			return num == null ? 0 : Convert.ToInt32 ((double)num);
		}

		/// <summary>
		/// Gets the integer from LuaTable for entry key as number.
		/// </summary>
		/// <returns>The integer.</returns>
		/// <param name="key">Key as number for the entry.</param>
		public int GetInt(double key)
		{
			if (wigTable == null)
				return 0;

			object num = wigTable [key];

			return num == null ? 0 : Convert.ToInt32 ((double)num);
		}

		/// <summary>
		/// Gets the string from LuaTable for entry key as string.
		/// </summary>
		/// <returns>The string.</returns>
		/// <param name="key">Key as string for the entry.</param>
		public string GetString(string key)
		{
			if (wigTable == null)
				return "";

			object obj = wigTable [key];

			return obj is string ? (string)obj : "";
		}

		/// <summary>
		/// Gets the string from LuaTable for entry key as number.
		/// </summary>
		/// <returns>The string.</returns>
		/// <param name="key">Key as number for the entry.</param>
		public string GetString(double key)
		{
			if (wigTable == null)
				return "";

			object obj = wigTable [key];

			return obj is string ? (string)obj : "";
		}

		public Table GetTable(LuaTable t)
		{
			return engine.GetTable (t);
		}

		/// <summary>
		/// Load all resources from gwc file, like position binary chunk and images. 
		/// Don't touch the rest. We read this already in PreLoadGWC.
		/// </summary>
		/// <param name="inputStream">Stream with GWC file.</param>
		public void LoadGWC(Stream inputStream)
		{
			try
			{
				// Open binary reader for reading the gwc file
				BinaryReader reader = new BinaryReader(inputStream);

				// Number of media files
				int maxMediaFiles;

				// Dictionary for the object table
				Dictionary<int,int> objects = new Dictionary<int,int>();

				// Jump over signature.
				// We didn't have to check it, because it is allready done in PreLoadGWC.
				reader.BaseStream.Position = 7;

				// Max number of objects in cartridge file
				maxMediaFiles = reader.ReadInt16();

				// Create array for resources
				resources = new Media[maxMediaFiles];

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
				resources[0] = new Media();
				resources[0].Data = new byte[fileSize];
				reader.Read(resources[0].Data, 0, resources[0].Data.Length);

				// Read all other resources
				for (int i = 1; i < maxMediaFiles; i++)
				{
					// Get pos of resources in stream, ...
					pos = objects[i];
					// ... jump to this position ...
					reader.BaseStream.Position = pos;
					// ... and read resources
					resources[i] = new Media();
					readMedia(resources[i], reader);
				}

				reader.Close();
			}
			catch (Exception e)
			{
                throw e;
			} 

		}

        /// <summary>
        /// Load only important data from gwc file, like position poster and icon. Drop the rest.
        /// </summary>
        /// <param name="inputStream">Stream with GWC file.</param>
        public void PreLoadGWC(Stream inputStream)
		{
		    int maxMediaFiles;
            int posterId;
            int iconId;

		    Dictionary<int,int> objects = new Dictionary<int,int>();

            // Signature of the compiled gwc file
            byte[] signature = { 0x02, 0x0a, 0x43, 0x41, 0x52, 0x54, 0x00 };

            // Open binary reader for reading the gwc file
			BinaryReader reader = new BinaryReader ( inputStream );

			try
			{	
				// Look for signature
				bool found = true;
				for ( int i = 0; i < signature.Length; i++ ) {
					if ( reader.ReadByte() != signature[i] )
					{
						found = false;
					}
				}

				// Signature correct?
				if ( !found ) 
				{
					throw new Exception ( "Invalide file format" );
				}

				// Max number of objects in cartridge file
				maxMediaFiles = reader.ReadInt16();

				// Create table for objects in cartridge files
				objects = new Dictionary<int,int>();

				// Read table
				for (int i = 0; i < maxMediaFiles; i++) 
				{
					short id = reader.ReadInt16();
					int position = reader.ReadInt32();
					objects.Add( id, position );
				}

				// Read header
				reader.ReadInt32 ();
				StartingLocationLatitude = reader.ReadDouble();
                StartingLocationLongitude = reader.ReadDouble();
                StartingLocationAltitude = reader.ReadDouble();
				// Dates are in seconds beyond 2004-02-10 01:00 (it's a palindrom, if you write it as 10-02-2004 ;-) )
				CreateDate = new DateTime(2004,02,10,01,00,00).AddSeconds(reader.ReadInt64());
				posterId = reader.ReadInt16();
				iconId = reader.ReadInt16();
				Activity = readCString(reader);
				Player = readCString(reader);
				reader.ReadInt32();
				reader.ReadInt32();
				Name = readCString(reader);
				Guid = readCString(reader);
				Description = readCString(reader);
				StartingDescription = readCString(reader);
				Version = readCString(reader);
				Author = readCString(reader);
				Company = readCString(reader);
				Device = readCString(reader);
				reader.ReadInt32();
				CompletionCode = readCString(reader);

                // Read poster
                if ( posterId > -1 && posterId < maxMediaFiles )
                {
                    Poster = new Media();

                    reader.BaseStream.Position = objects[posterId];
                    readMedia ( Poster, reader);
                }

                // Read icon
                if (iconId > -1 && iconId < maxMediaFiles)
                {
                    Icon = new Media();

                    reader.BaseStream.Position = objects[iconId];
                    readMedia(Icon, reader);
                }

                reader.Close ();

			}
			catch (Exception e)
			{
                throw e;
			} 
		}

        #endregion

        #region C# Helper Function

        /// <summary>
        /// Read a null terminated string from binary stream.
        /// </summary>
        /// <param name="reader">Binary stream with gwc file as input.</param>
        /// <returns>String, which represents the C# string.</returns>
        private string readCString(BinaryReader input)
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
        /// <param name="reader"></param>
        private void readMedia(Media media, BinaryReader reader)
        {
			byte valid = reader.ReadByte();
			if ( valid == 0 )
			{
				// No resources 
                media.Type = 0;
                media.Data = null;
			}
			else
			{
                // Read resources type
                media.Type = reader.ReadInt32();
                // Read resources data
				long fileSize = reader.ReadInt32();
				media.Data = new byte[fileSize];
				reader.Read( media.Data, 0, media.Data.Length );
			}
        }

        #endregion

		#region Notify Property Change

		public event PropertyChangedEventHandler PropertyChanged;

		internal void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}

		#endregion


    }

}