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

        #region Fields

        private Live.WherigoCartridge.ActivityTypes _activityType;
        private string _authorCompany;
        private string _authorName;
        private DateTime _createDate;
        private bool _complete;
        private DateTime _completedTime = DateTime.MinValue;
        private string _completionCode;
        private Live.WherigoCartridge.CompletionTimes _completionTime;
        private int _countryID;
        private DateTime? _dateAdded;
        private DateTime? _dateLastPlayed;
        private DateTime? _dateLastUpdated;
        private string _device;
		private string _emptyInventoryListText;
		private string _emptyTasksListText;
		private string _emptyYouSeeListText;
		private string _emptyZonesListText;
        private string _guid;
        private Media _icon;
        private string _iconFileURL;
		private string _internalDescription;
		private string _internalName;
		private string _internalStartingDescription;
        private bool _isArchived;
        private bool _isDisabled;
        private bool _isOpenSource;
        private bool _isPlayAnywhere;
        private string[] _linkedGeocacheNames;
        private string[] _linkedGeocacheGCs;
        private Guid[] _linkedGeocacheGUIDs;
        private string _longDescription;
        private string _name;
        private int _numberOfCompletions;
        private int _numberOfUsersWatching;
        private string _player;
        private Media _poster;
        private string _posterFileURL;
        private Media[] _resources;
        private string _shortDescription;
        private string _startingDescription;
        private double _startingLocationLatitude = 360.0;
        private double _startingLocationLongitude = 360.0;
        private double _startingLocationAltitude = 360.0;
        private int _stateID;
        private int _uniqueDownloads;
        private bool _userHasPartiallyPlayed;
        private string _version;

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
                return _activityType;
            }

            internal set
            {
                if (_activityType != value)
                {
                    _activityType = value;
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
                return _authorCompany;
            }

            internal set
            {
                if (_authorCompany != value)
                {
                    _authorCompany = value;
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
                return _authorName;
            }

            internal set
            {
                if (_authorName != value)
                {
                    _authorName = value;
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
                return _createDate;
            }

            internal set
            {
                if (_createDate != value)
                {
                    _createDate = value;
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
                return _complete;
            }

            internal set
            {
                if (_complete != value)
                {
                    _complete = value;
                    NotifyPropertyChanged("Complete");
                }
                if (_complete && _completedTime == DateTime.MinValue)
                    _completedTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Gets the completion code for this cartridge.
        /// </summary>
        public string CompletionCode
        {
            get
            {
                return _completionCode;
            }

            internal set
            {
                if (_completionCode != value)
                {
                    _completionCode = value;
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
                return _completedTime;
            }
        }

        /// <summary>
        /// Gets an estimation of the time needed to complete the cartridge.
        /// </summary>
        public Live.WherigoCartridge.CompletionTimes CompletionTime
        {
            get
            {
                return _completionTime;
            }

            internal set
            {
                if (_completionTime != value)
                {
                    _completionTime = value;
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
                return _countryID;
            }

            internal set
            {
                if (_countryID != value)
                {
                    _countryID = value;
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
                return _dateAdded;
            }
            
			internal set
            {
                if (_dateAdded != value)
                {
                    _dateAdded = value;
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
                return _dateLastPlayed;
            }
            
			internal set
            {
                if (_dateLastPlayed != value)
                {
                    _dateLastPlayed = value;
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
                return _dateLastUpdated;
            }

            internal set
            {
                if (_dateLastUpdated != value)
                {
                    _dateLastUpdated = value;
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
                return _device;
            }
            
			internal set
            {
                if (_device != value)
                {
                    _device = value;
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
				return _emptyInventoryListText;
			}

			internal set
			{
				if (_emptyInventoryListText != value)
				{
					_emptyInventoryListText = value;
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
				return _emptyTasksListText;
			}

			internal set
			{
				if (_emptyTasksListText != value)
				{
					_emptyTasksListText = value;
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
				return _emptyYouSeeListText;
			}

			internal set
			{
				if (_emptyYouSeeListText != value)
				{
					_emptyYouSeeListText = value;
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
				return _emptyZonesListText;
			}

			internal set
			{
				if (_emptyZonesListText != value)
				{
					_emptyZonesListText = value;
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
                return _guid;
            }
            
			internal set
            {
                if (_guid != value)
                {
                    _guid = value;
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
                if (_icon == null && !String.IsNullOrEmpty(_iconFileURL))
                {
                    // Load icon from URL
					asyncDownloadFile(_iconFileURL, ms =>
					{
						if (ms != null)
						{
							_icon = new Media();
							_icon.Data = ms.ToArray();
							NotifyPropertyChanged("Icon");
						}
					});
                }
                return _icon;
            }
            
			internal set
            {
                if (_icon != value)
                {
                    _icon = value;
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
                return _iconFileURL;
            }
            
			internal set
            {
                if (_iconFileURL != value)
                {
                    _iconFileURL = value;
                    // If there is an icon, than load the new one immediatly
                    if (_icon != null)
                    {
                        // Load icon from URL
						asyncDownloadFile(_iconFileURL, ms =>
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
				return _internalDescription;
			}

			internal set
			{
				if (_internalDescription != value)
				{
					_internalDescription = value;
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
				return _internalName;
			}

			internal set
			{
				if (_internalName != value)
				{
					_internalName = value;
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
				return _internalStartingDescription;
			}

			internal set
			{
				if (_internalStartingDescription != value)
				{
					_internalStartingDescription = value;
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
                return _isArchived;
            }
            
			internal set
            {
                if (_isArchived != value)
                {
                    _isArchived = value;
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
                return _isDisabled;
            }
            
			internal set
            {
                if (_isDisabled != value)
                {
                    _isDisabled = value;
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
                return _isOpenSource;
            }
            
			internal set
            {
                if (_isOpenSource != value)
                {
                    _isOpenSource = value;
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
                return _isPlayAnywhere;
            }
            
			internal set
            {
                if (_isPlayAnywhere != value)
                {
                    _isPlayAnywhere = value;
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
                return _linkedGeocacheNames;
            }
            
			internal set
            {
                if (_linkedGeocacheNames != value)
                {
                    _linkedGeocacheNames = value;
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
                return _linkedGeocacheGCs;
            }
            
			internal set
            {
                if (_linkedGeocacheGCs != value)
                {
                    _linkedGeocacheGCs = value;
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
                return _linkedGeocacheGUIDs;
            }
            
			internal set
            {
                if (_linkedGeocacheGUIDs != value)
                {
                    _linkedGeocacheGUIDs = value;
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
                return _longDescription;
            }
            
			internal set
            {
                if (_longDescription != value)
                {
                    _longDescription = value.ReplaceHTMLMarkup();
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
                return _name;
            }
            
			internal set
            {
                if (_name != value)
                {
                    _name = value;
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
                return _numberOfCompletions;
            }
            
			internal set
            {
                if (_numberOfCompletions != value)
                {
                    _numberOfCompletions = value;
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
                return _numberOfUsersWatching;
            }
            
			internal set
            {
                if (_numberOfUsersWatching != value)
                {
                    _numberOfUsersWatching = value;
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
                return _player;
            }
            
			internal set
            {
                if (_player != value)
                {
                    _player = value;
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
                if (_poster == null && !String.IsNullOrEmpty(_posterFileURL))
                {
                    // Load icon from URL
					asyncDownloadFile(_posterFileURL, ms =>
					{
						if (ms != null)
						{
							_poster = new Media();
							_poster.Data = ms.ToArray();
							NotifyPropertyChanged("Poster");
						}
					});
                }

                return _poster;
            }
            
			internal set
            {
                if (_poster != value)
                {
                    _poster = value;
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
                return _posterFileURL;
            }
            
			internal set
            {
                if (_posterFileURL != value)
                {
                    _posterFileURL = value;
                    // If there is a poster, than load the new one immediatly
                    if (_poster != null)
                    {
                        // Load icon from URL
						asyncDownloadFile(_posterFileURL, ms =>
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
                return _resources;
            }
            
			internal set
            {
                if (_resources != value)
                {
                    _resources = value;
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
                return _shortDescription;
            }
            
			internal set
            {
                if (_shortDescription != value)
                {
                    _shortDescription = value;
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
                return _startingDescription;
            }
            
			internal set
            {
                if (_startingDescription != value)
                {
                    _startingDescription = value;
                    NotifyPropertyChanged("StartingDescription");
                }
            }
        }

		/// <summary>
		/// Gets a point representing the starting location of this cartridge.
		/// </summary>
		public ZonePoint StartingLocation
		{
			get
			{
				return new ZonePoint(StartingLocationLatitude, StartingLocationLongitude, StartingLocationAltitude);
			}
		}

        /// <summary>
        /// Gets the latitude of the starting location.
        /// </summary>
        public double StartingLocationLatitude
        {
            get
            {
                return _startingLocationLatitude;
            }
            
			internal set
            {
                if (_startingLocationLatitude != value)
                {
                    _startingLocationLatitude = value;
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
                return _startingLocationLongitude;
            }
            
			internal set
            {
                if (_startingLocationLongitude != value)
                {
                    _startingLocationLongitude = value;
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
                return _startingLocationAltitude;
            }
            
			internal set
            {
                if (_startingLocationAltitude != value)
                {
                    _startingLocationAltitude = value;
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
                return _stateID;
            }
            
			internal set
            {
                if (_stateID != value)
                {
                    _stateID = value;
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
                return _uniqueDownloads;
            }
            
			internal set
            {
                if (_uniqueDownloads != value)
                {
                    _uniqueDownloads = value;
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
                return _userHasPartiallyPlayed;
            }
            
			internal set
            {
                if (_userHasPartiallyPlayed != value)
                {
                    _userHasPartiallyPlayed = value;
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
                return _version;
            }
            
			internal set
            {
                if (_version != value)
                {
                    _version = value;
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