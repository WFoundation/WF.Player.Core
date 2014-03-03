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
using WF.Player.Core.Utils;

namespace WF.Player.Core
{
	/// <summary>
	/// An input requested of the player.
	/// </summary>
	public class Input : WherigoObject
	{
		#region Delegates

		internal delegate void RunOnGetInput(string result);

		#endregion

		#region Members

		private RunOnGetInput _runOnGetInput;

		#endregion
		
		#region Constructor

		internal Input(WF.Player.Core.Data.IDataContainer data, RunOnGetInput onGetInput)
			: base(data)
		{
			_runOnGetInput = onGetInput;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets a list of possible choices for the answer.
		/// </summary>
		public IEnumerable<string> Choices 
		{
			get 
			{
				return DataContainer.GetList<string>("Choices");
			}
		}

		/// <summary>
		/// Gets the standard input type.
		/// </summary>
		/// <value>The input type.</value>
		public InputType InputType {
			get 
			{
				return DataContainer.GetEnum<InputType>("InputType", InputType.Unknown).Value;
			}
		}

		/// <summary>
		/// Gets the text as Markdown converted to HTML.
		/// </summary>
		/// <value>The text.</value>
		public string HTML {
			get {
				return DataContainer.GetString("Text").ReplaceHTMLScriptMarkup().ReplaceMarkdown(); // + "</center></body></html>";
			}
		}

		/// <summary>
		/// Gets the image.
		/// </summary>
		/// <value>The image.</value>
		public Media Image {
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
			get {
				return DataContainer.GetString("Name");
			}
		}

		/// <summary>
		/// Gets the index of the object.
		/// </summary>
		/// <value>The index of the object.</value>
		public int ObjIndex {
			get {
                return DataContainer.GetInt("ObjIndex").Value;
			}
		}

		/// <summary>
		/// Gets the text.
		/// </summary>
		/// <value>The text.</value>
		public string Text {
			get {
				return DataContainer.GetString("Text").ReplaceHTMLMarkup();
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="WF.Player.Core.Input"/> is visible.
		/// </summary>
		/// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
		public bool Visible {
			get {
                return DataContainer.GetBool("Visible").Value;
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
			//engine.LuaExecQueue.BeginCallSelf(this, "OnGetInput", result);
			_runOnGetInput(result);
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

