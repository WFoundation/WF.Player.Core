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
using System.Collections.Generic;
using System.Text;

namespace WF.Player.Core
{
	/// <summary>
	/// A container for a media resource.
	/// </summary>
    #if MONOTOUCH
	    [MonoTouch.Foundation.Preserve(AllMembers=true)]
    #endif
    public class Media : WherigoObject
    {

        #region Constructor

        internal Media() : base(null)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the raw data of the media.
        /// </summary>
		public byte[] Data { get; internal set; }

		/// <summary>
		/// Gets the name of the file, which is containing this media.
		/// </summary>
		public string FileName { get; internal set; }

		/// <summary>
		/// Gets the position of the media in the underlying cartridge.
		/// </summary>
		public long FileOffset { get; internal set; }

		/// <summary>
		/// Gets the file size of the media in the underlying file.
		/// </summary>
		public long FileSize { get; internal set; }

		/// <summary>
        /// Gets the id of the media in the cartridge file it came from.
        /// </summary>
		public int MediaId { get; internal set; }

        /// <summary>
        /// Gets the type of the media.
        /// </summary>
		public MediaType Type { get; internal set; }

        /// <summary>
        /// Gets the name of the media in the game.
        /// </summary>
        public string Name
        {
            get
            {
                return IsBound ? DataContainer.GetString("Name") : null;
            }
        }

        /// <summary>
        /// Gets the description of the media.
        /// </summary>
        public string Description
        {
            get
            {
                return IsBound ? DataContainer.GetString("Description") : null;
            }
        }

        /// <summary>
        /// Gets the alternative text for the media if it cannot be displayed.
        /// </summary>
        public string AltText
        {
            get
            {
                return IsBound ? DataContainer.GetString("AltText") : null;
            }
        }

        // TODO: check if value is the same as FileName
        public string MediaFilename
        {
            get
            {
                return IsBound ? DataContainer.GetContainer("Resources").GetString("Filename") : null;
            }
        }

        // TODO: check values.
        public IEnumerable<string> Directives
        {
            get
            {
                return IsBound ? DataContainer.GetContainer("Resources").GetList<string>("Directives") : null;
            }
        }

        private bool IsBound
        {
            get
            {
                return DataContainer != null;
            }
        }

        #endregion

    }

}
