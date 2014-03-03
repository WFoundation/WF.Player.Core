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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Net;
using WF.Player.Core.Utils;
//using Newtonsoft.Json;

namespace WF.Player.Core
{
    #if MONOTOUCH
	    [MonoTouch.Foundation.Preserve(AllMembers=true)]
    #endif
	// TODO: Multithreading safety.

	/// <summary>
	/// A particular game of Wherigo.
	/// </summary>
	public class Cartridge : WherigoObject, INotifyPropertyChanged
    {

        #region Members

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
		private string emptyInventoryListText;
		private string emptyTasksListText;
		private string emptyYouSeeListText;
		private string emptyZonesListText;
        private string guid;
        private Media icon;
        private string iconFileURL;
		private string internalDescription;
		private string internalName;
		private string internalStartingDescription;
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

        #endregion

        #region Constructor

        public Cartridge ( string filename = null ) : base(null)
		{
            // Save filename of the gwc file for later use.
            // If filename starts with WG, than filename is an online cartridge
			if (filename != null)
            	Filename = filename;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the type of activity this cartridge provides the player with.
        /// </summary>
        public Live.WherigoCartridge.ActivityTypes ActivityType
        {
            get
            {
                return activityType;
            }

            internal set
            {
                if (activityType != value)
                {
                    activityType = value;
                    NotifyPropertyChanged("ActivityType");
                }
            }
        }

        /// <summary>
        /// Gets the company the author belongs to.
        /// </summary>
        public string AuthorCompany
        {
            get
            {
                return authorCompany;
            }

            internal set
            {
                if (authorCompany != value)
                {
                    authorCompany = value;
                    NotifyPropertyChanged("AuthorCompany");
                }
            }
        }

        /// <summary>
        /// Gets the name of the author of this cartridge.
        /// </summary>
        public string AuthorName
        {
            get
            {
                return authorName;
            }

            internal set
            {
                if (authorName != value)
                {
                    authorName = value;
                    NotifyPropertyChanged("AuthorName");
                }
            }
        }

        /// <summary>
        /// Gets the date of creation of this cartridge.
        /// </summary>
        public DateTime CreateDate
        {
            get
            {
                return createDate;
            }

            internal set
            {
                if (createDate != value)
                {
                    createDate = value;
                    NotifyPropertyChanged("CreateDate");
                }
            }
        }

        /// <summary>
        /// Gets if the cartridge has been completed by the player.
        /// </summary>
        /// <value><c>true</c> if the cartridge is complete; otherwise, <c>false</c>.</value>
        public bool Complete
        {
            get
            {
                return complete;
            }

            internal set
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
        /// Gets the completion code for this cartridge.
        /// </summary>
        public string CompletionCode
        {
            get
            {
                return completionCode;
            }

            internal set
            {
                if (completionCode != value)
                {
                    completionCode = value;
                    NotifyPropertyChanged("CompletionCode");
                }
            }
        }

        /// <summary>
        /// Gets the time when the cartridge was completed.
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
        /// Gets an estimation of the time needed to complete the cartridge.
        /// </summary>
        public Live.WherigoCartridge.CompletionTimes CompletionTime
        {
            get
            {
                return completionTime;
            }

            internal set
            {
                if (completionTime != value)
                {
                    completionTime = value;
                    NotifyPropertyChanged("CompletionTime");
                }
            }
        }

        /// <summary>
        /// Gets the country ID for this cartridge.
        /// </summary>
        public int CountryID
        {
            get
            {
                return countryID;
            }

            internal set
            {
                if (countryID != value)
                {
                    countryID = value;
                    NotifyPropertyChanged("CountryID");
                }
            }
        }

        /// <summary>
        /// Gets the date when this cartridge was added to the website.
        /// </summary>
        public DateTime? DateAdded
        {
            get
            {
                return dateAdded;
            }
            
			internal set
            {
                if (dateAdded != value)
                {
                    dateAdded = value;
                    NotifyPropertyChanged("DateAdded");
                }
            }
        }

        /// <summary>
        /// Gets the date when this cartridge was played for the last time.
        /// </summary>
        public DateTime? DateLastPlayed
        {
            get
            {
                return dateLastPlayed;
            }
            
			internal set
            {
                if (dateLastPlayed != value)
                {
                    dateLastPlayed = value;
                    NotifyPropertyChanged("DateLastPlayed");
                }
            }
        }

        /// <summary>
		/// Gets the date when this cartridge was updated for the last time.
        /// </summary>
        public DateTime? DateLastUpdated
        {
            get
            {
                return dateLastUpdated;
            }

            internal set
            {
                if (dateLastUpdated != value)
                {
                    dateLastUpdated = value;
                    NotifyPropertyChanged("DateLastUpdated");
                }
            }
        }

        /// <summary>
        /// Gets the device for which this cartridge has been created.
        /// </summary>
        public string Device
        {
            get
            {
                return device;
            }
            
			internal set
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
				return emptyInventoryListText;
			}

			internal set
			{
				if (emptyInventoryListText != value)
				{
					emptyInventoryListText = value;
					NotifyPropertyChanged("EmptyInventoryListText");
				}
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
				return emptyTasksListText;
			}

			internal set
			{
				if (emptyTasksListText != value)
				{
					emptyTasksListText = value;
					NotifyPropertyChanged("EmptyTasksListText");
				}
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
				return emptyYouSeeListText;
			}

			internal set
			{
				if (emptyYouSeeListText != value)
				{
					emptyYouSeeListText = value;
					NotifyPropertyChanged("EmptyYouSeeListText");
				}
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
				return emptyZonesListText;
			}

			internal set
			{
				if (emptyZonesListText != value)
				{
					emptyZonesListText = value;
					NotifyPropertyChanged("EmptyZonesListText");
				}
			}
        }

		/// <summary>
		/// Gets or sets the filename of the cartridge.
		/// </summary>
		public string Filename 
		{ 
			get; 
			set; 
		}

        /// <summary>
        /// Gets the global unique ID of the cartridge.
        /// </summary>
        public string Guid
        {
            get
            {
                return guid;
            }
            
			internal set
            {
                if (guid != value)
                {
                    guid = value;
                    NotifyPropertyChanged("Guid");
                }
            }
        }

        /// <summary>
        /// Gets the icon of this cartridge.
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
            
			internal set
            {
                if (icon != value)
                {
                    icon = value;
                    NotifyPropertyChanged("Icon");
                }
            }
        }

        /// <summary>
        /// Gets the URL for the icon image of this cartridge.
        /// </summary>
        public string IconFileURL
        {
            get
            {
                return iconFileURL;
            }
            
			internal set
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
		/// Gets the description of this cartridge, as defined in the internal
		/// program of the cartridge.
		/// </summary>
		public string InternalDescription
		{
			get
			{
				return internalDescription;
			}

			internal set
			{
				if (internalDescription != value)
				{
					internalDescription = value;
					NotifyPropertyChanged("InternalDescription");
				}
			}
		}

		/// <summary>
		/// Gets the title of this cartridge, as defined in the internal
		/// program of the cartridge.
		/// </summary>
		public string InternalName
		{
			get
			{
				return internalName;
			}

			internal set
			{
				if (internalName != value)
				{
					internalName = value;
					NotifyPropertyChanged("InternalName");
				}
			}
		}

		/// <summary>
		/// Gets the description of the start point of this cartridge, 
		/// as defined in the internal program of the cartridge.
		/// </summary>
		public string InternalStartingDescription
		{
			get
			{
				return internalStartingDescription;
			}

			internal set
			{
				if (internalStartingDescription != value)
				{
					internalStartingDescription = value;
					NotifyPropertyChanged("InternalStartingDescription");
				}
			}
		}

        /// <summary>
        /// Gets if the cartridge is archived.
        /// </summary>
        public bool IsArchived
        {
            get
            {
                return isArchived;
            }
            
			internal set
            {
                if (isArchived != value)
                {
                    isArchived = value;
                    NotifyPropertyChanged("IsArchived");
                }
            }
        }

        /// <summary>
        /// Gets if the cartridge is disabled.
        /// </summary>
        public bool IsDisabled
        {
            get
            {
                return isDisabled;
            }
            
			internal set
            {
                if (isDisabled != value)
                {
                    isDisabled = value;
                    NotifyPropertyChanged("IsDisabled");
                }
            }
        }

		/// <summary>
		/// Gets if this cartridge is fully loaded, including its metadata,
		/// and game resources.
		/// </summary>
		public bool IsLoaded
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets if the cartridge is open source, i.e. the source is availible for download.
		/// </summary>
		/// <value><c>true</c> if this instance is open source; otherwise, <c>false</c>.</value>
        public bool IsOpenSource
        {
            get
            {
                return isOpenSource;
            }
            
			internal set
            {
                if (isOpenSource != value)
                {
                    isOpenSource = value;
                    NotifyPropertyChanged("IsOpenSource");
                }
            }
        }

        /// <summary>
        /// Gets if the cartridge can be played anywhere.
        /// </summary>
        public bool IsPlayAnywhere
        {
            get
            {
                return isPlayAnywhere;
            }
            
			internal set
            {
                if (isPlayAnywhere != value)
                {
                    isPlayAnywhere = value;
                    NotifyPropertyChanged("IsPlayAnywhere");
                }
            }
        }

        /// <summary>
        /// Gets a list of geocache names on geocaching.com that are linked to this cartridge.
        /// </summary>
        public string[] LinkedGeocacheNames
        {
            get
            {
                return linkedGeocacheNames;
            }
            
			internal set
            {
                if (linkedGeocacheNames != value)
                {
                    linkedGeocacheNames = value;
                    NotifyPropertyChanged("LinkedGeocacheNames");
                }
            }
        }

        /// <summary>
		/// Gets a list of geocache GC-codes on geocaching.com that are linked to this cartridge.
        /// </summary>
        public string[] LinkedGeocacheGCs
        {
            get
            {
                return linkedGeocacheGCs;
            }
            
			internal set
            {
                if (linkedGeocacheGCs != value)
                {
                    linkedGeocacheGCs = value;
                    NotifyPropertyChanged("LinkedGeocacheGCs");
                }
            }
        }

        /// <summary>
        /// Gets a list of geocache GUIDs on geocaching.com that are linked to this cartridge.
        /// </summary>
        public Guid[] LinkedGeocacheGUIDs
        {
            get
            {
                return linkedGeocacheGUIDs;
            }
            
			internal set
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
        /// Gets the long description for this cartridge.
        /// </summary>
        public string LongDescription
        {
            get
            {
                return longDescription;
            }
            
			internal set
            {
                if (longDescription != value)
                {
                    longDescription = value.ReplaceHTMLMarkup();
                    NotifyPropertyChanged("LongDescription");
                }
            }
        }

        /// <summary>
        /// Gets the name of this cartridge.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
            
			internal set
            {
                if (name != value)
                {
                    name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        /// <summary>
        /// Gets how often this cartridge has been marked as completed so far.
        /// </summary>
        public int NumberOfCompletions
        {
            get
            {
                return numberOfCompletions;
            }
            
			internal set
            {
                if (numberOfCompletions != value)
                {
                    numberOfCompletions = value;
                    NotifyPropertyChanged("NumberOfCompletions");
                }
            }
        }

        /// <summary>
        /// Gets how many users watching this cartridge on the server.
        /// </summary>
        public int NumberOfUsersWatching
        {
            get
            {
                return numberOfUsersWatching;
            }
            
			internal set
            {
                if (numberOfUsersWatching != value)
                {
                    numberOfUsersWatching = value;
                    NotifyPropertyChanged("NumberOfUsersWatching");
                }
            }
        }

        /// <summary>
        /// Gets the name of the player this cartridge was compiled for.
        /// </summary>
        public string Player
        {
            get
            {
                return player;
            }
            
			internal set
            {
                if (player != value)
                {
                    player = value;
                    NotifyPropertyChanged("Player");
                }
            }
        }

        /// <summary>
        /// Gets the poster image for this cartridge.
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
            
			internal set
            {
                if (poster != value)
                {
                    poster = value;
                    NotifyPropertyChanged("Poster");
                }
            }
        }

        /// <summary>
        /// Gets the URL for the poster image of this cartridge.
        /// </summary>
        public string PosterFileURL
        {
            get
            {
                return posterFileURL;
            }
            
			internal set
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
        /// Gets all media resources of this catridge.
        /// </summary>
        /// <value>The resources.</value>
        public Media[] Resources
        {
            get
            {
                return resources;
            }
            
			internal set
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
        /// Gets the short description of this cartridge.
        /// </summary>
        public string ShortDescription
        {
            get
            {
                return shortDescription;
            }
            
			internal set
            {
                if (shortDescription != value)
                {
                    shortDescription = value;
                    NotifyPropertyChanged("ShortDescription");
                }
            }
        }

        /// <summary>
        /// Gets the starting description for this cartridge.
        /// </summary>
        public string StartingDescription
        {
            get
            {
                return startingDescription;
            }
            
			internal set
            {
                if (startingDescription != value)
                {
                    startingDescription = value;
                    NotifyPropertyChanged("StartingDescription");
                }
            }
        }

        /// <summary>
        /// Gets the latitude of the starting location.
        /// </summary>
        public double StartingLocationLatitude
        {
            get
            {
                return startingLocationLatitude;
            }
            
			internal set
            {
                if (startingLocationLatitude != value)
                {
                    startingLocationLatitude = value;
                    NotifyPropertyChanged("StartingLocationLatitude");
                }
            }
        }

        /// <summary>
        /// Gets the longitude of the starting location.
        /// </summary>
        public double StartingLocationLongitude
        {
            get
            {
                return startingLocationLongitude;
            }
            
			internal set
            {
                if (startingLocationLongitude != value)
                {
                    startingLocationLongitude = value;
                    NotifyPropertyChanged("StartingLocationLongitude");
                }
            }
        }

        /// <summary>
        /// Gets the altitude of the starting location.
        /// </summary>
        public double StartingLocationAltitude
        {
            get
            {
                return startingLocationAltitude;
            }
            
			internal set
            {
                if (startingLocationAltitude != value)
                {
                    startingLocationAltitude = value;
                    NotifyPropertyChanged("StartingLocationAltitude");
                }
            }
        }

        /// <summary>
        /// Gets the StateID for this cartridge.
        /// </summary>
        public int StateID
        {
            get
            {
                return stateID;
            }
            
			internal set
            {
                if (stateID != value)
                {
                    stateID = value;
                    NotifyPropertyChanged("StateID");
                }
            }
        }

        /// <summary>
        /// Gets the amount of downloads from server for this cartridge.
        /// </summary>
        public int UniqueDownloads
        {
            get
            {
                return uniqueDownloads;
            }
            
			internal set
            {
                if (uniqueDownloads != value)
                {
                    uniqueDownloads = value;
                    NotifyPropertyChanged("UniqueDownloads");
                }
            }
        }

        /// <summary>
        /// Gets if the user has already started to play the cartrtridge.
        /// </summary>
        public bool UserHasPartiallyPlayed
        {
            get
            {
                return userHasPartiallyPlayed;
            }
            
			internal set
            {
                if (userHasPartiallyPlayed != value)
                {
                    userHasPartiallyPlayed = value;
                    NotifyPropertyChanged("UserHasPartiallyPlayed");
                }
            }
        }

        /// <summary>
        /// Gets the version of this cartridge, as defined by its author.
        /// </summary>
        public string Version
        {
            get
            {
                return version;
            }
            
			internal set
            {
                if (version != value)
                {
                    version = value;
                    NotifyPropertyChanged("Version");
                }
            }
        }

		/// <summary>
		/// Gets the WGCode of this cartridge.
		/// </summary>
		/// <value>The WG code.</value>
		public string WGCode
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets if this Cartridge is bound to a data source.
		/// </summary>
		internal bool IsBound
		{
			get
			{
				return DataContainer != null;
			}
		}
		
		#endregion

        #region Downloading

		/// <summary>
		/// Download a file from web in async mode and call if ready callback with result as MemoryStream.
		/// </summary>
		/// <param name="url">URL address.</param>
		/// <param name="callback">Callback function.</param>
		private void asyncDownloadFile(string url, Action<MemoryStream> callback)
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

		internal override void OnDataContainerChanged(Data.IDataContainer dc)
		{
			// Refreshes some properties.
			bool ok = dc != null;
			EmptyInventoryListText = ok ? dc.GetString("EmptyInventoryListText") : null;
			EmptyTasksListText = ok ? dc.GetString("EmptyTasksListText") : null;
			EmptyYouSeeListText = ok ? dc.GetString("EmptyYouSeeListText") : null;
			EmptyZonesListText = ok ? dc.GetString("EmptyZonesListText") : null;
			InternalDescription = ok ? dc.GetString("Description") : null;
			InternalName = ok ? dc.GetString("Name") : null;
			InternalStartingDescription = ok ? dc.GetString("StartingLocationDescription") : null;
		}
   }

}