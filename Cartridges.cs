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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.Linq;
using System.Text;

namespace WF.Player.Core
{
    public class Cartridges : List<Cartridge>
    {
        private string consumerToken = "";
        private int status = 0;
        private string statusMessage = "";
        private API.APIv1Client webClient;

        #region Constructor

        public Cartridges(string token) : base ()
        {
            consumerToken = token;

            try
            {
                System.ServiceModel.BasicHttpBinding binding = new System.ServiceModel.BasicHttpBinding();
//                binding.MaxReceivedMessageSize = 1024 * 1024 * 1024;
                System.ServiceModel.EndpointAddress endpoint = new System.ServiceModel.EndpointAddress(new Uri("http://foundation.rangerfox.com/API/APIv1.svc"));

                webClient = new API.APIv1Client(binding, endpoint);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Fill cartridge list by a list of filenames. All filenames in the list contains a valid path and filename.
        /// </summary>
        /// <param name="dir">Path to directory to get cartridges from.</param>
        /// <returns>True, if the list has changed, else false.</returns>
        public bool GetByFileList(List<string> files)
        {
            if (files == null)
            {
                statusMessage = "No files found";
                return false;
            }

            // Delete old ones
            RemoveRange(0, Count);

            foreach (string fileName in files)
            {
                if (File.Exists(fileName))
                {
                    Cartridge cart = new Cartridge(fileName);
                    FileFormats.LoadHeader(new FileStream(fileName, FileMode.Open), cart);
                    Add(cart);
                }
            }

            return false;
        }

        /// <summary>
        /// Get cartridge list by a coordinate and a radius.
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <param name="radius">Radius in km</param>
        /// <returns>True, if the list has changed, else false.</returns>
        public bool GetByCoords(double lat, double lon, double radius)
        {
            if (!checkConnection())
                return false;

   			API.SearchCartridgesRequest search = new API.SearchCartridgesRequest();
			search.consumerToken = consumerToken;
			search.PageNumber = 1;
			search.ResultsPerPage = 50;
			search.SearchArguments = new API.CartridgeSearchArguments();
			search.SearchArguments.OrderSearchBy = API.CartridgeSearchArguments.OrderBy.Distance;
			search.SearchArguments.Latitude = lat;
			search.SearchArguments.Longitude = lon;
			search.SearchArguments.SearchRadiusInKm = radius;

            return getCartridges(search);
        }

        /// <summary>
        /// Get cartridge list by a part of the name.
        /// </summary>
        /// <param name="name">Part of the name</param>
        /// <returns>True, if the list has changed, else false.</returns>
        public bool GetByName(string name)
        {
            if (!checkConnection())
                return false;
            
            API.SearchCartridgesRequest search = new API.SearchCartridgesRequest();
			search.consumerToken = consumerToken;
			search.PageNumber = 1;
			search.ResultsPerPage = 50;
			search.SearchArguments = new API.CartridgeSearchArguments();
			search.SearchArguments.CartridgeName = name.Trim();
			search.SearchArguments.OrderSearchBy = API.CartridgeSearchArguments.OrderBy.Distance;

            return getCartridges(search);
        }

        /// <summary>
        /// Get cartridge list with only play anywhere cartridges. Sort list by date.
        /// </summary>
        /// <returns>True, if the list has changed, else false.</returns>
        public bool GetByPlayAnywhere()
        {
            if (!checkConnection())
                return false;
            
            API.SearchCartridgesRequest search = new API.SearchCartridgesRequest();
			search.consumerToken = consumerToken;
			search.PageNumber = 1;
			search.ResultsPerPage = 50;
			search.SearchArguments = new API.CartridgeSearchArguments();
			search.SearchArguments.OrderSearchBy = API.CartridgeSearchArguments.OrderBy.PublishDate;
			search.SearchArguments.IsPlayAnywhere = true;

            return getCartridges(search);
        }

        #endregion

        #region Private Functions

        /// <summary>
        /// Check, if the connection is ok.
        /// </summary>
        /// <returns>True, if the connections is ok, else false.</returns>
        private bool checkConnection()
        {
            if (String.Empty.Equals(consumerToken))
            {
                statusMessage = "Missing token";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Search cartridges via web client.
        /// </summary>
        /// <param name="search">Search parameters</param>
        /// <returns>True, if the request was ok, else false.</returns>
        private bool getCartridges(API.SearchCartridgesRequest search)
        {
            search.consumerToken = consumerToken;

            try
            {
//                API.DownloadCartridgeRequest req = new API.DownloadCartridgeRequest();
//                req.consumerToken = consumerToken;
//                req.WGCode = "WG13";
//                API.DownloadCartridgeResponse response = webClient.DownloadCartridge(req);

                API.SearchCartridgesResponse resp = webClient.SearchCartridges(search);

                status = resp.Status.StatusCode;

                if (resp.Status.StatusCode == 0)
                {
                    if (resp.Cartridges != null)
                    {
                        // Delete old ones
                        RemoveRange(0, Count);
                        // Get new ones
                        foreach (API.CartridgeSearchResult res in resp.Cartridges)
                        {
                            Cartridge cart = new Cartridge(res.WGCode);

                            cart.Name = res.Name;
                            cart.AuthorName = res.AuthorName;
                            cart.AuthorCompany = res.AuthorCompany;
                            cart.DateAdded = res.DateAdded;
                            cart.DateLastPlayed = res.DateLastPlayed;
                            cart.DateLastUpdated = res.DateLastUpdated;
                            cart.CompletionTime = res.CompletionTime;
                            cart.ActivityType = res.ActivityType;
                            cart.CountryID = res.CountryID;
                            cart.StateID = res.StateID;
                            cart.IsArchived = res.IsArchived;
                            cart.IsDisabled = res.IsDisabled;
                            cart.IsOpenSource = res.IsOpenSource;
                            cart.IsPlayAnywhere = res.IsPlayAnywhere;
                            cart.IconFileURL = res.IconFileURL;
                            cart.PosterFileURL = res.PosterFileURL;
                            cart.SetStartingLocation(res.Latitude, res.Longitude);
                            cart.LinkedGeocacheNames = res.LinkedGeocacheNames;
                            cart.LinkedGeocacheGCs = res.LinkedGeocacheGCs;
                            cart.LinkedGeocacheGUIDs = res.LinkedGeocacheGuids;
                            cart.LongDescription = res.LongDescription;
                            cart.ShortDescription = res.ShortDescription;
                            cart.NumberOfCompletions = res.NumberOfCompletions;
                            cart.NumberOfUsersWatching = res.NumberOfUsersWatching;
                            cart.UniqueDownloads = res.UniqueDownloads;
                            cart.Complete = res.UserHasCompleted;
                            cart.UserHasPartiallyPlayed = res.UserHasPartiallyPlayed;

                            Add(cart);
                        }
                    }

                    return true;
                }
                else
                {
                    statusMessage = resp.Status.StatusMessage;
                    return false;
                }
            }
            catch (Exception e)
            {
                throw e;
            }


        }

        #endregion

    }
}
