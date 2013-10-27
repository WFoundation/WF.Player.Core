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
using System.ComponentModel;
using NLua;
using WF.Player.Core.Utils;
using WF.Player.Core.Engines;

namespace WF.Player.Core
{
	/// <summary>
	/// Base class for a game entity that notifies of changes of its properties.
	/// </summary>
	public class UIObject : Table, INotifyPropertyChanged
	{
		private string html;

		#region Constructor

		internal UIObject (Engine e, LuaTable t) : base (e, t)
		{
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the containe, which is holding this object.
		/// </summary>
		/// <value>The Container holding this object.</value>
		public UIObject Container {
			get {
				return GetTable("Container") as UIObject;
			}
		}

		/// <summary>
		/// Gets the description.
		/// </summary>
		/// <value>The description.</value>
		public string Description {
			get {
				return GetString ("Description").ReplaceHTMLMarkup();
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
				return (this.GetLuaFunc("OnClick") != null);
			}
		}
		
		/// <summary>
		/// Gets the description as Html.
		/// </summary>
		/// <value>The description.</value>
		public string HTML {
			get {
				if (html == null) {
					html = "<html><body><center>" + GetString("Description").ReplaceHTMLScriptMarkup().ReplaceMarkdown() +"</center></body></html>";
				}
				return html;
			}
		}

		/// <summary>
		/// Gets the icon.
		/// </summary>
		/// <value>The icon.</value>
		public Media Icon {
			get 
			{
                return GetMedia("Icon");
			}
		}

		/// <summary>
		/// Gets the image.
		/// </summary>
		/// <value>The image as Media object.</value>
		public Media Image {
			get 
			{
				return GetMedia("Media");
			}
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name {
			get 
			{
				return GetString ("Name");
			}
		}

		/// <summary>
		/// Gets the index of the object.
		/// </summary>
		/// <value>The index of the object.</value>
		public int ObjIndex {
			get 
			{
				return GetInt ("ObjIndex");
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="WF.Player.Core.UIObject"/> is visible.
		/// </summary>
		/// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
		public bool Visible {
			get 
			{
				return GetBool ("Visible");
			}
		}

		/// <summary>
		/// Gets the location of the object.
		/// </summary>
		public ZonePoint ObjectLocation
		{
			get
			{
				return GetTable("ObjectLocation") as ZonePoint;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Calls OnClick.
		/// </summary>
		public void CallOnClick()
		{
			engine.LuaExecQueue.BeginCallSelf(this, "OnClick");
		}

		#endregion
		
		#region Notify Property Change

		public event PropertyChangedEventHandler PropertyChanged;

		internal void NotifyPropertyChanged(string propName)
		{
			if (propName.Equals ("Description"))
				html = null;

			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
			}
		}

		#endregion

	}

}
