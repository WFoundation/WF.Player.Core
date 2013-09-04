using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace WF.Player.Core
{
	public class MessageBox
	{
		/// <summary>
		/// Represents the different kinds of results a message box can have.
		/// </summary>
		public enum Result
		{
			FirstButton,
			SecondButton,
			Cancel
		}

		#region Properties
		/// <summary>
		/// Gets the text of the message box to display.
		/// </summary>
		public string Text { get; private set; }

		/// <summary>
		/// Gets the media object associated to the message box.
		/// </summary>
		public Media MediaObject { get; private set; }

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

		private Action<string> _Callback;

		#endregion

		/// <summary>
		/// Creates a message box descriptor.
		/// </summary>
		/// <param name="text">Text to display.</param>
		/// <param name="mediaObj">Media object to display (can be null.)</param>
		/// <param name="btn1label">Label of the first button (if null or empty, a default value will be used.)</param>
		/// <param name="btn2label">Label of the second button (if null or empty, the button will not be shown.)</param>
		/// <param name="callback">Function to call once the message box has gotten a result.</param>
		public MessageBox(string text, Media mediaObj, string btn1label, string btn2label, Action<string> callback)
		{
			Text = text;
			MediaObject = mediaObj;
			FirstButtonLabel = String.IsNullOrEmpty(btn1label) ? null : btn1label;
			SecondButtonLabel = String.IsNullOrEmpty(btn2label) ? null : btn2label;

			_Callback = callback;
		}

		/// <summary>
		/// Gives a result to the message box, allowing its underlying execution tree to continue.
		/// </summary>
		/// <param name="result"></param>
		public void GiveResult(Result result)
		{
			if (_Callback == null)
			{
				throw new InvalidOperationException("No callback has been specified for this message box.");
			}

			switch (result)
			{
				case Result.FirstButton:
					if (FirstButtonLabel == null)
					{
						throw new InvalidOperationException("There is no first button on this message box.");
					}

					_Callback(FirstButtonLabel);

					break;

				case Result.SecondButton:

					if (SecondButtonLabel == null)
					{
						throw new InvalidOperationException("There is no second button on this message box.");
					}

					_Callback(SecondButtonLabel);

					break;

				case Result.Cancel:

					// TODO: Cancelled message boxes call the callback with a nil parameter.

					break;

				default:
					throw new NotImplementedException("This result type is not implemented: " + result.ToString());
			}
		}
	}
}
