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
using System.Collections.Generic;
using System.ComponentModel;
using WF.Player.Core.Utils;
using WF.Player.Core.Engines;

namespace WF.Player.Core
{
	/// <summary>
	/// Base class for a game entity that notifies of changes of its properties.
	/// </summary>
	public class UIObject : WherigoObject, INotifyPropertyChanged
	{
		#region Delegates

		internal delegate void RunOnClick();

		#endregion
		
		#region Fields

		private RunOnClick _runOnClick;

		#endregion

		#region Constructor

		internal UIObject(WF.Player.Core.Data.IDataContainer data, RunOnClick runOnClick)
			: base(data)
		{
			_runOnClick = runOnClick;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the description.
		/// </summary>
		/// <value>The description.</value>
		public string Description {
			get {
				return DataContainer.GetString("Description").ReplaceHTMLMarkup();
			}
		}

		/// <summary>
		/// Gets a value indicating whether this object has event OnClick or not.
		/// </summary>
		/// <value><c>true</c> if this object has OnClick; otherwise, <c>false</c>.</value>
		public bool HasOnClick 
		{
			get
			{
				return DataContainer.GetProvider("OnClick") != null;
			}
		}
		
		/// <summary>
		/// Gets the description as Html.
		/// </summary>
		/// <value>The description.</value>
		public string Html {
			get 
			{
				return DataContainer.GetString("Html"); // + "</center></body></html>";
			}
		}

		/// <summary>
		/// Gets the description as Markdown.
		/// </summary>
		/// <value>The description.</value>
		public string Markdown {
			get 
			{
				return DataContainer.GetString("Markdown");
			}
		}

		/// <summary>
		/// Gets the icon.
		/// </summary>
		/// <value>The icon.</value>
		public Media Icon {
			get 
			{
				return DataContainer.GetWherigoObject<Media>("Icon");
			}
		}

		/// <summary>
		/// Gets the image.
		/// </summary>
		/// <value>The image as Media object.</value>
		public Media Media {
			get 
			{
                return DataContainer.GetWherigoObject<Media>("Media");
			}
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name {
			get 
			{
				return DataContainer.GetString("Name");
			}
		}

		/// <summary>
		/// Gets the index of the object.
		/// </summary>
		/// <value>The index of the object.</value>
		public int ObjIndex {
			get 
			{
                return DataContainer.GetInt("ObjIndex").Value;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="WF.Player.Core.UIObject"/> is visible.
		/// </summary>
		/// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
		public bool Visible {
			get 
			{
                return DataContainer.GetBool("Visible").Value;
			}
		}

		/// <summary>
		/// Gets the location of the object, or null if no location is set.
		/// </summary>
		/// <remarks>If the object has a non-null container and no own location, 
		/// the container's <code>ObjectLocation</code> is returned.</remarks>
		public ZonePoint ObjectLocation
		{
			get
			{
				return DataContainer.GetWherigoObject<ZonePoint>("ObjectLocation");
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Calls OnClick.
		/// </summary>
		public void CallOnClick()
		{
			//engine.LuaExecQueue.BeginCallSelf(this, "OnClick");
			_runOnClick();
		}

		/// <summary>
		/// Determines if two objects are equal.
		/// </summary>
		/// <param name="obj">An object to compare.</param>
		/// <returns>True if <paramref name="obj"/> is a UIObject
		/// and has the same <code>ObjIndex</code> as the current object.</returns>
		public override bool Equals(object obj)
		{
			return obj != null && obj.GetType().Equals(GetType()) && ((UIObject)obj).ObjIndex == ObjIndex;
		}

		public override string ToString()
		{
			// Gets the name of the object.
			string nameExtra;
			try
			{
				// We don't want this to crash.
				nameExtra = ": " + Name;
			}
			catch (Exception)
			{
				nameExtra = "";
			}

			// Returns the name if possible.
			return base.ToString() + nameExtra;
		}

		#endregion
		
		#region Notify Property Change

		public event PropertyChangedEventHandler PropertyChanged;

		internal void NotifyPropertyChanged(string propName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
			}
		}

		#endregion

	}

}
