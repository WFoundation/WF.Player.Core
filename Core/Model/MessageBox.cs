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

namespace WF.Player.Core
{
	public class MessageBox
	{
		#region Properties
		/// <summary>
		/// Gets the text of the message box to display.
		/// </summary>
		public string Text { get; private set; }

		/// <summary>
		/// Gets the media object associated to the message box.
		/// </summary>
		public Media Image { get; private set; }

		/// <summary>
		/// Gets the text of the first button label. If null, a default value should be provided.
		/// </summary>
		public string FirstButtonLabel { get; private set; }

		/// <summary>
		/// Gets the text of the second button label. If null, the button shouldn't be displayed.
		/// </summary>
		public string SecondButtonLabel { get; private set; }

		#endregion

		#region Fields

		private Action<string> callback;

		#endregion

		/// <summary>
		/// Creates a message box descriptor.
		/// </summary>
		/// <param name="text">Text to display.</param>
		/// <param name="mediaObj">Media object to display (can be null.)</param>
		/// <param name="btn1label">Label of the first button (if null or empty, a default value will be used.)</param>
		/// <param name="btn2label">Label of the second button (if null or empty, the button will not be shown.)</param>
		/// <param name="callback">Function to call once the message box has gotten a result.</param>
		public MessageBox(string text, Media mediaObj, string btn1label, string btn2label, Action<string> cb)
		{
			Text = Engine.ReplaceMarkup(text);
			Image = mediaObj;
			FirstButtonLabel = String.IsNullOrEmpty(btn1label) ? null : btn1label;
			SecondButtonLabel = String.IsNullOrEmpty(btn2label) ? null : btn2label;

			callback = cb;
		}

		/// <summary>
		/// Gives a result to the message box, allowing its underlying execution tree to continue.
		/// </summary>
		/// <param name="result"></param>
		public void GiveResult(MessageBoxResult result)
		{
			// Message boxes with no callbacks don't give result and silenty return.
			if (callback == null)
			{
				return;
			}

			switch (result)
			{
				case MessageBoxResult.FirstButton:
					if (FirstButtonLabel == null)
					{
						throw new InvalidOperationException("There is no first button on this message box.");
					}

					callback("Button1");

					break;

				case MessageBoxResult.SecondButton:

					if (SecondButtonLabel == null)
					{
						throw new InvalidOperationException("There is no second button on this message box.");
					}

					callback("Button2");

					break;

				case MessageBoxResult.Cancel:

					// Cancelled message boxes call the callback with a nil parameter.
					callback (null);

					break;

				default:
					throw new NotImplementedException("This result type is not implemented: " + result.ToString());
			}
		}
	}
}
