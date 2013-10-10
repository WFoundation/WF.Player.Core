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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using NLua;
using System.Net;
using Newtonsoft.Json;

namespace WF.Player.Core
{

    /// <summary>
	/// Class for objects to store informations for cartridges.
    /// </summary>
    #if MONOTOUCH
	    [MonoTouch.Foundation.Preserve(AllMembers=true)]
    #endif
	// TODO: Make inherit Table and change generation strategy from Engine.
	// TODO: Multithreading safety.
	public class Cartridge : INotifyPropertyChanged
    {

        #region Private variables

        private Live.WherigoCartridge.ActivityTypes activityType;
        private string authorCompany;
        private string authorName;
        private DateTime createDate;
        private bool complete;
        private DateTime completedTime = DateTime.MinValue;
        private string completionCode;
        private Live.WherigoCartridge.CompletionTimes completionTime;
        private int countryID;
        private DateTime? dateAdded;
        private DateTime? dateLastPlayed;
        private DateTime? dateLastUpdated;
        private string device;
        private Engine engine;
        private string guid;
        private Media icon;
        private string iconFileURL;
        private bool isArchived;
        private bool isDisabled;
        private bool isOpenSource;
        private bool isPlayAnywhere;
        private string[] linkedGeocacheNames;
        private string[] linkedGeocacheGCs;
        private Guid[] linkedGeocacheGUIDs;
        private string longDescription;
        private string name;
        private int numberOfCompletions;
        private int numberOfUsersWatching;
        private string player;
        private Media poster;
        private string posterFileURL;
        private Media[] resources;
        private string shortDescription;
        private string startingDescription;
        private double startingLocationLatitude = 360.0;
        private double startingLocationLongitude = 360.0;
        private double startingLocationAltitude = 360.0;
        private int stateID;
        private int uniqueDownloads;
        private bool userHasPartiallyPlayed;
        private string version;
		private string wgCode;
		private LuaTable wigTable;

        #endregion

        #region Constructor

        public Cartridge ( string filename = null )
		{
            // Save filename of the gwc file for later use.
            // If filename starts with WG, than filename is an online cartridge
			if (filename != null)
            	Filename = filename;
        }

        #endregion

        #region C# Properties

        /// <summary>
        /// Type of activity this cartridge has (like "Geocaching" or "Puzzle").
        /// </summary>
        public Live.WherigoCartridge.ActivityTypes ActivityType
        {
            get
            {
                return activityType;
            }
            set
            {
                if (activityType != value)
                {
                    activityType = value;
                    NotifyPropertyChanged("ActivityType");
                }
            }
        }

        /// <summary>
        /// Company the author belongs to.
        /// </summary>
        public string AuthorCompany
        {
            get
            {
                return authorCompany;
            }
            set
            {
                if (authorCompany != value)
                {
                    authorCompany = value;
                    NotifyPropertyChanged("AuthorCompany");
                }
            }
        }

        /// <summary>
        /// Name of the author of this cartridge.
        /// </summary>
        public string AuthorName
        {
            get
            {
                return authorName;
            }
            set
            {
                if (authorName != value)
                {
                    authorName = value;
                    NotifyPropertyChanged("AuthorName");
                }
            }
        }

        /// <summary>
        /// Date of creation of this cartridge.
        /// </summary>
        public DateTime CreateDate
        {
            get
            {
                return createDate;
            }
            set
            {
                if (createDate != value)
                {
                    createDate = value;
                    NotifyPropertyChanged("CreateDate");
                }
            }
        }

        /// <summary>
        /// Gets/sets the cartridge completion state.
        /// </summary>
        /// <value><c>true</c> if the cartridge is complete; otherwise, <c>false</c>.</value>
        public bool Complete
        {
            get
            {
                return complete;
            }
            set
            {
                if (complete != value)
                {
                    complete = value;
                    NotifyPropertyChanged("Complete");
                }
                if (complete && completedTime == DateTime.MinValue)
                    completedTime = DateTime.Now;
            }
        }

        /// <summary>
        /// CompletionCode for this cartridge.
        /// </summary>
        public string CompletionCode
        {
            get
            {
                return completionCode;
            }
            set
            {
                if (completionCode != value)
                {
                    completionCode = value;
                    NotifyPropertyChanged("CompletionCode");
                }
            }
        }

        /// <summary>
        /// Gets/sets the cartridge completed time.
        /// </summary>
        /// <value>DateTime when the cartridge was completed.</value>
        public DateTime CompletedTime
        {
            get
            {
                return completedTime;
            }
        }

        /// <summary>
        /// Time to be needed to play throught the cartridge.
        /// </summary>
        public Live.WherigoCartridge.CompletionTimes CompletionTime
        {
            get
            {
                return completionTime;
            }
            set
            {
                if (completionTime != value)
                {
                    completionTime = value;
                    NotifyPropertyChanged("CompletionTime");
                }
            }
        }

        /// <summary>
        /// CountryID for this cartridge.
        /// </summary>
        public int CountryID
        {
            get
            {
                return countryID;
            }
            set
            {
                if (countryID != value)
                {
                    countryID = value;
                    NotifyPropertyChanged("CountryID");
                }
            }
        }

        /// <summary>
        /// Date of adding this cartridge to the server.
        /// </summary>
        public DateTime? DateAdded
        {
            get
            {
                return dateAdded;
            }
            set
            {
                if (dateAdded != value)
                {
                    dateAdded = value;
                    NotifyPropertyChanged("DateAdded");
                }
            }
        }

        /// <summary>
        /// Date, when this cartridge is last played.
        /// </summary>
        public DateTime? DateLastPlayed
        {
            get
            {
                return dateLastPlayed;
            }
            set
            {
                if (dateLastPlayed != value)
                {
                    dateLastPlayed = value;
                    NotifyPropertyChanged("DateLastPlayed");
                }
            }
        }

        /// <summary>
        /// Date, when the source of the cartridge is last updated.
        /// </summary>
        public DateTime? DateLastUpdated
        {
            get
            {
                return dateLastUpdated;
            }
            set
            {
                if (dateLastUpdated != value)
                {
                    dateLastUpdated = value;
                    NotifyPropertyChanged("DateLastUpdated");
                }
            }
        }

        /// <summary>
        /// Device, for which this cartridge is created.
        /// </summary>
        public string Device
        {
            get
            {
                return device;
            }
            set
            {
                if (device != value)
                {
                    device = value;
                    NotifyPropertyChanged("Device");
                }
            }
        }

        /// <summary>
        /// Gets the empty inventory list text.
        /// </summary>
        /// <value>The empty inventory list text.</value>
        public string EmptyInventoryListText
        {
            get
            {
                return GetString("EmptyInventoryListText");
            }
        }

        /// <summary>
        /// Gets the empty tasks list text.
        /// </summary>
        /// <value>The empty tasks list text.</value>
        public string EmptyTasksListText
        {
            get
            {
                return GetString("EmptyTasksListText");
            }
        }

        /// <summary>
        /// Gets the empty you see list text.
        /// </summary>
        /// <value>The empty you see list text.</value>
        public string EmptyYouSeeListText
        {
            get
            {
                return GetString("EmptyYouSeeListText");
            }
        }

        /// <summary>
        /// Gets the empty zones list text.
        /// </summary>
        /// <value>The empty zones list text.</value>
        public string EmptyZonesListText
        {
            get
            {
                return GetString("EmptyZonesListText");
            }
        }

        /// <summary>
        /// Sets the engine to which this cartridge belongs.
        /// </summary>
        /// <value>The engine.</value>
        public Engine Engine
        {
            set
            {
                engine = value;
            }
        }

		/// <summary>
		/// Filename of the cartridge.
		/// </summary>
		public string Filename 
		{ 
			get; 
			set; 
		}

        /// <summary>
        /// Guid of the cartridge.
        /// </summary>
        public string Guid
        {
            get
            {
                return guid;
            }
            set
            {
                if (guid != value)
                {
                    guid = value;
                    NotifyPropertyChanged("Guid");
                }
            }
        }

        /// <summary>
        /// Icon of this cartridge.
        /// </summary>
        public Media Icon
        {
            get
            {
                if (icon == null && !String.IsNullOrEmpty(iconFileURL))
                {
                    // Load icon from URL
					asyncDownloadFile(iconFileURL, ms =>
					{
						if (ms != null)
						{
							icon = new Media();
							icon.Data = ms.ToArray();
							NotifyPropertyChanged("Icon");
						}
					});
                }
                return icon;
            }
            set
            {
                if (icon != value)
                {
                    icon = value;
                    NotifyPropertyChanged("Icon");
                }
            }
        }

        /// <summary>
        /// Set URL for the icon image of this cartridge.
        /// </summary>
        /// <value>The new URL for icon file.</value>
        public string IconFileURL
        {
            get
            {
                return iconFileURL;
            }
            set
            {
                if (iconFileURL != value)
                {
                    iconFileURL = value;
                    // If there is an icon, than load the new one immediatly
                    if (icon != null)
                    {
                        // Load icon from URL
						asyncDownloadFile(iconFileURL, ms =>
						{
							if (ms != null)
							{
								Media temp = new Media();
								temp.Data = ms.ToArray();
								Icon = temp;
							}
						});
                    }
                }
            }
        }

        /// <summary>
        /// If the cartridge is archived.
        /// </summary>
        public bool IsArchived
        {
            get
            {
                return isArchived;
            }
            set
            {
                if (isArchived != value)
                {
                    isArchived = value;
                    NotifyPropertyChanged("IsArchived");
                }
            }
        }

        /// <summary>
        /// If the cartridge is disabled.
        /// </summary>
        public bool IsDisabled
        {
            get
            {
                return isDisabled;
            }
            set
            {
                if (isDisabled != value)
                {
                    isDisabled = value;
                    NotifyPropertyChanged("IsDisabled");
                }
            }
        }

		/// <summary>
		/// If the cartridge is an open source, so that the source is availible for download.
		/// </summary>
		/// <value><c>true</c> if this instance is open source; otherwise, <c>false</c>.</value>
        public bool IsOpenSource
        {
            get
            {
                return isOpenSource;
            }
            set
            {
                if (isOpenSource != value)
                {
                    isOpenSource = value;
                    NotifyPropertyChanged("IsOpenSource");
                }
            }
        }

        /// <summary>
        /// If the cartridge is a play anywhere.
        /// </summary>
        public bool IsPlayAnywhere
        {
            get
            {
                return isPlayAnywhere;
            }
            set
            {
                if (isPlayAnywhere != value)
                {
                    isPlayAnywhere = value;
                    NotifyPropertyChanged("IsPlayAnywhere");
                }
            }
        }

        /// <summary>
        /// List of geocache names on www.geocaching.com belonging to this cartridge.
        /// </summary>
        public string[] LinkedGeocacheNames
        {
            get
            {
                return linkedGeocacheNames;
            }
            set
            {
                if (linkedGeocacheNames != value)
                {
                    linkedGeocacheNames = value;
                    NotifyPropertyChanged("LinkedGeocacheNames");
                }
            }
        }

        /// <summary>
        /// List of geocache GC numbers on www.geocaching.com belonging to this cartridge.
        /// </summary>
        public string[] LinkedGeocacheGCs
        {
            get
            {
                return linkedGeocacheGCs;
            }
            set
            {
                if (linkedGeocacheGCs != value)
                {
                    linkedGeocacheGCs = value;
                    NotifyPropertyChanged("LinkedGeocacheGCs");
                }
            }
        }

        /// <summary>
        /// List of geocache GUIDs on www.geocaching.com belonging to this cartridge.
        /// </summary>
        public Guid[] LinkedGeocacheGUIDs
        {
            get
            {
                return linkedGeocacheGUIDs;
            }
            set
            {
                if (linkedGeocacheGUIDs != value)
                {
                    linkedGeocacheGUIDs = value;
                    NotifyPropertyChanged("LinkedGeocacheGUIDs");
                }
            }
        }

        /// <summary>
        /// Gets the cartridge log filename with extension .gwl.
        /// </summary>
        /// <value>Cartridge log filename with extension gwl.</value>
        public string LogFilename
        {
            get
            {
                return Path.ChangeExtension(Filename, ".gwl");
            }
        }

        /// <summary>
        /// Long description for this cartridge.
        /// </summary>
        public string LongDescription
        {
            get
            {
                return longDescription;
            }
            set
            {
                if (longDescription != value)
                {
                    longDescription = value;
                    NotifyPropertyChanged("LongDescription");
                }
            }
        }

        /// <summary>
        /// Name of this cartridge.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (name != value)
                {
                    name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        /// <summary>
        /// How often this cartridge is marked as completed.
        /// </summary>
        public int NumberOfCompletions
        {
            get
            {
                return numberOfCompletions;
            }
            set
            {
                if (numberOfCompletions != value)
                {
                    numberOfCompletions = value;
                    NotifyPropertyChanged("NumberOfCompletions");
                }
            }
        }

        /// <summary>
        /// How many users watching this cartridge on the server.
        /// </summary>
        public int NumberOfUsersWatching
        {
            get
            {
                return numberOfUsersWatching;
            }
            set
            {
                if (numberOfUsersWatching != value)
                {
                    numberOfUsersWatching = value;
                    NotifyPropertyChanged("NumberOfUsersWatching");
                }
            }
        }

        /// <summary>
        /// Object for the player of this cartridge.
        /// </summary>
        public string Player
        {
            get
            {
                return player;
            }
            set
            {
                if (player != value)
                {
                    player = value;
                    NotifyPropertyChanged("Player");
                }
            }
        }

        /// <summary>
        /// Poster image for this cartridge.
        /// </summary>
        public Media Poster
        {
            get
            {
                if (poster == null && !String.IsNullOrEmpty(posterFileURL))
                {
                    // Load icon from URL
					asyncDownloadFile(posterFileURL, ms =>
					{
						if (ms != null)
						{
							poster = new Media();
							poster.Data = ms.ToArray();
							NotifyPropertyChanged("Poster");
						}
					});
                }

                return poster;
            }
            set
            {
                if (poster != value)
                {
                    poster = value;
                    NotifyPropertyChanged("Poster");
                }
            }
        }

        /// <summary>
        /// Set URL for the poster image of this cartridge.
        /// </summary>
        /// <value>The new URL for poster file.</value>
        public string PosterFileURL
        {
            get
            {
                return posterFileURL;
            }
            set
            {
                if (posterFileURL != value)
                {
                    posterFileURL = value;
                    // If there is a poster, than load the new one immediatly
                    if (poster != null)
                    {
                        // Load icon from URL
						asyncDownloadFile(posterFileURL, ms =>
						{
							if (ms != null)
							{
								// Use a temp Media, because of NotifyPropertyEvent
								Media temp = new Media();
								temp.Data = ms.ToArray();
								Poster = temp;
							}
						});
                    }
                }
            }
        }

        /// <summary>
        /// Gets the array with all resources, belonging to this catridge.
        /// </summary>
        /// <value>The resources.</value>
        public Media[] Resources
        {
            get
            {
                return resources;
            }
            set
            {
                if (resources != value)
                {
                    resources = value;
                    NotifyPropertyChanged("Resources");
                }
            }
        }

        /// <summary>
        /// Gets the cartridge save filename with extension .gws.
        /// </summary>
        /// <value>Cartridge save filename with extension gws.</value>
        public string SaveFilename
        {
            get
            {
                return Path.ChangeExtension(Filename, ".gws");
            }
        }

        /// <summary>
        /// Short description for this cartridge.
        /// </summary>
        public string ShortDescription
        {
            get
            {
                return shortDescription;
            }
            set
            {
                if (shortDescription != value)
                {
                    shortDescription = value;
                    NotifyPropertyChanged("ShortDescription");
                }
            }
        }

        /// <summary>
        /// Starting description for this cartridge.
        /// </summary>
        public string StartingDescription
        {
            get
            {
                return startingDescription;
            }
            set
            {
                if (startingDescription != value)
                {
                    startingDescription = value;
                    NotifyPropertyChanged("StartingDescription");
                }
            }
        }

        /// <summary>
        /// Latitude of the starting location.
        /// </summary>
        public double StartingLocationLatitude
        {
            get
            {
                return startingLocationLatitude;
            }
            set
            {
                if (startingLocationLatitude != value)
                {
                    startingLocationLatitude = value;
                    NotifyPropertyChanged("StartingLocationLatitude");
                }
            }
        }

        /// <summary>
        /// Longitude of the starting location.
        /// </summary>
        public double StartingLocationLongitude
        {
            get
            {
                return startingLocationLongitude;
            }
            set
            {
                if (startingLocationLongitude != value)
                {
                    startingLocationLongitude = value;
                    NotifyPropertyChanged("StartingLocationLongitude");
                }
            }
        }

        /// <summary>
        /// Altitude of the starting location.
        /// </summary>
        public double StartingLocationAltitude
        {
            get
            {
                return startingLocationAltitude;
            }
            set
            {
                if (startingLocationAltitude != value)
                {
                    startingLocationAltitude = value;
                    NotifyPropertyChanged("StartingLocationAltitude");
                }
            }
        }

        /// <summary>
        /// StateID for this cartridge.
        /// </summary>
        public int StateID
        {
            get
            {
                return stateID;
            }
            set
            {
                if (stateID != value)
                {
                    stateID = value;
                    NotifyPropertyChanged("StateID");
                }
            }
        }

        /// <summary>
        /// Number of downloads from server for this cartridge.
        /// </summary>
        public int UniqueDownloads
        {
            get
            {
                return uniqueDownloads;
            }
            set
            {
                if (uniqueDownloads != value)
                {
                    uniqueDownloads = value;
                    NotifyPropertyChanged("UniqueDownloads");
                }
            }
        }

        /// <summary>
        /// Has user started to play the cartrtridge.
        /// </summary>
        public bool UserHasPartiallyPlayed
        {
            get
            {
                return userHasPartiallyPlayed;
            }
            set
            {
                if (userHasPartiallyPlayed != value)
                {
                    userHasPartiallyPlayed = value;
                    NotifyPropertyChanged("UserHasPartiallyPlayed");
                }
            }
        }

        /// <summary>
        /// Version of this cartridge.
        /// </summary>
        public string Version
        {
            get
            {
                return version;
            }
            set
            {
                if (version != value)
                {
                    version = value;
                    NotifyPropertyChanged("Version");
                }
            }
        }

		/// <summary>
		/// Gets or sets the WGCode of this cartridge.
		/// </summary>
		/// <value>The WG code.</value>
		public string WGCode {
			get {
				return wgCode;
			}
			set {
				if (wgCode != value)
					wgCode = value;
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

        #region C# Methods

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

		/// <summary>
		/// Gets a table of a LuaTable.
		/// </summary>
		/// <returns>Table.</returns>
		/// <param name="t">LuaTable.</param>
		public Table GetTable(LuaTable t)
		{
			return engine.GetTable (t);
		}

        #endregion

        #region C# Helper Function

		/// <summary>
		/// Download a file from web in async mode and call if ready callback with result as MemoryStream.
		/// </summary>
		/// <param name="url">URL address.</param>
		/// <param name="callback">Callback function.</param>
		internal void asyncDownloadFile(string url, Action<MemoryStream> callback)
        {
            try
            {
                // Open a connection
                HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);

#if !WINDOWS_PHONE
                httpWebRequest.AllowWriteStreamBuffering = true;
                httpWebRequest.Referer = @"http://www.google.com/";
                httpWebRequest.Timeout = 20000;
#endif

				// Waits for response asynchronously.
				IAsyncResult asyncResult = httpWebRequest.BeginGetResponse(new AsyncCallback(r =>
				{
					// Async handling of the response.
					HttpWebRequest request = r.AsyncState as HttpWebRequest;
					if (request != null)
					{
						// The target memory stream.
						MemoryStream result = new MemoryStream();
						
						try
						{							
							// Gets and copies the response stream to the result stream.
							using (WebResponse response = request.EndGetResponse(r))
							{
								using (Stream webStream = response.GetResponseStream())
								{
									webStream.CopyTo(result);
								}
							}

						}
						catch (Exception)
						{
							// Exception, therefore null returns.
							callback(null);
						}

						// Calls the callback with the stream.
						callback(result);
					}
					else
					{
						// No appropriate response, therefore null returns.
						callback(null);
					}

				}), httpWebRequest);

            }
            catch (Exception)
            {
				throw;
            }
        }

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
                media.Type = (MediaType)Enum.ToObject(typeof(MediaType), reader.ReadInt32());
                // Read resources data
				long fileSize = reader.ReadInt32();
				media.Data = new byte[fileSize];
				reader.Read( media.Data, 0, media.Data.Length );
			}
        }

		#endregion

		#region Notify Property Change

		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Notifies if a property has changed.
		/// </summary>
		/// <param name="info">Name of property, which has changed.</param>
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