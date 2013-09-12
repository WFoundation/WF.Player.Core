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
	/// Class for objects to store informations for medias.
	/// </summary>
    #if MONOTOUCH
	    [MonoTouch.Foundation.Preserve(AllMembers=true)]
    #endif
    public class Media
    {

        #region Constructor

        public Media()
        {
        }

        #endregion

        #region C# Property

        /// <summary>
        /// Bytes for the media.
        /// </summary>
		public byte[] Data { get; internal set; }

		/// <summary>
		/// Name of the file, which is containing this media.
		/// </summary>
		public string FileName { get; internal set; }

		/// <summary>
		/// File position for media in the underlaying file.
		/// </summary>
		public long FileOffset { get; internal set; }

		/// <summary>
		/// File size for media in the underlaying file.
		/// </summary>
		public long FileSize { get; internal set; }

		/// <summary>
        /// Id for media, which determins the position in gwc file.
        /// </summary>
		public int MediaId { get; internal set; }

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name { get; internal set; }

        /// <summary>
        /// Type of the media.
        /// </summary>
		public MediaType Type { get; internal set; }

        #endregion

    }

}
