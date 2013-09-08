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
using NLua;

namespace WF.Player.Core
{

	public class Input : Table
	{
		#region Constructor

		public Input (Engine e, LuaTable t) : base (e, t)
		{
		}

		#endregion

		#region Properties

		public List<string> Choices {
			get {
				List<string> result = new List<string> (); 

				var s = ((LuaTable)wigTable ["Choices"]).GetEnumerator ();
				while (s.MoveNext())
					result.Add ((string)s.Value);

				return result;
			}
		}

		/// <summary>
		/// Gets the standard input type.
		/// </summary>
		/// <value>The input type.</value>
		public InputType InputType {
			get 
			{
				string type = GetString("InputType");
					
				if (String.IsNullOrWhiteSpace(type))
					return InputType.Unknown;

				return (InputType) Enum.Parse(typeof(InputType), type, true);
			}
		}

		/// <summary>
		/// Gets the image.
		/// </summary>
		/// <value>The image.</value>
		public Media Image {
			get {
                var media = wigTable["Media"];

				if (media is LuaTable)
				    return engine.GetMedia (Convert.ToInt32 ((double)((LuaTable)media)["ObjIndex"]));
                else
					return null;

			}
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name {
			get {
				return GetString ("Name");
			}
		}

		/// <summary>
		/// Gets the index of the object.
		/// </summary>
		/// <value>The index of the object.</value>
		public int ObjIndex {
			get {
				return GetInt ("ObjIndex");
			}
		}

		/// <summary>
		/// Gets the text.
		/// </summary>
		/// <value>The text.</value>
		public string Text {
			get {
				return Engine.ReplaceMarkup(GetString ("Text"));
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="WF.Player.Core.Input"/> is visible.
		/// </summary>
		/// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
		public bool Visible {
			get {
				return GetBool ("Visible");
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gives a result answer to the input.
		/// </summary>
		/// <param name="result">The answer. If null, the input is considered to be cancelled.</param>
		public void GiveResult(string result)
		{
			engine.Call (wigTable, "OnGetInput", new object[] { wigTable, result });
		}

		/// <summary>
		/// Notifies the input that it has been dismissed before a result could be given.
		/// </summary>
		/// <remarks>Equivalent to <code>GiveResult(null)</code>.</remarks>
		public void Cancel()
		{
			GiveResult(null);
		}

		#endregion

	}

}

