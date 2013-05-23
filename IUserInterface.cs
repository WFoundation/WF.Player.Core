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
using System.Collections.Generic;


namespace WF.Player.Core
{

	/// <summary>
	/// This functions must be implemented for WF.Player to work.
	/// </summary>
    public interface IUserInterface
    {

        #region Functions that are called from WF.Player.Core

        /// <summary>
		/// Logs the message with level to the gwl file.
		/// </summary>
		/// <param name="level">Level of messages like defined in Engine.LOG... .</param>
		/// <param name="message">Text of message.</param>
        void LogMessage(int level, string message);

		/// <summary>
		/// Shows a messages with text and image on the screen. It could have two possible buttons. 
		/// If one of the buttons is pressed or the message is aborted, than is the Lua function wrapper 
		/// called. Arguments for this call are the text from the buttons or nil.
		/// </summary>
		/// <param name="text">Text to show.</param>
		/// <param name="mediaObj">Media object of the message.</param>
		/// <param name="btn1Label">Label of button1.</param>
		/// <param name="btn2Label">Label of button2.</param>
		/// <param name="wrapper">Lua function for callback.</param>
        void MessageBox(string text, Media mediaObj, string btn1Label, string btn2Label, CallbackFunction wrapper);

		/// <summary>
		/// Shows a input (text/choice) on the screen.
		/// </summary>
		/// <param name="inputObj">Object for input.</param>
        void GetInput(Input inputObj);

		/// <summary>
		/// Plays a media of given type.
		/// </summary>
		/// <param name="type">Type of media object.</param>
		/// <param name="mediaObj">ObjIndex of media.</param>
        void MediaEvent(int type, Media mediaObj);

		/// <summary>
		/// Shows the given text as a status text.
		/// </summary>
		/// <param name="text">Text to show.</param>
        void ShowStatusText(string text);

		/// <summary>
		/// Shows a special screen.
		/// </summary>
		/// <param name="screen">Type of screen like defined by Engine. ...SCREEN.</param>
		/// <param name="idxObj">ObjIndex of the object, which should shown on Engine.DETAILSCREEN.</param>
        void ShowScreen(int screen, int idxObj);

		/// <summary>
		/// Notifies the user interface to do a special command.
		/// </summary>
		/// <param name="command">Command as string.</param>
        void NotifyOS(string command);

        #endregion

		#region Events that are called from WF.Player.Core

		/// <summary>
		/// Event, which is called, if a attribute of a object has changed.
		/// </summary>
		/// <param name="obj">Object, which was changed.</param>
		/// <param name="type">Attribute, which was changed.</param>
		void AttributeChanged(Table obj, string type);

		/// <summary>
		/// Event, which is call, if arguments had changed for the cartridge object.
		/// </summary>
		/// <param name="type">Name of argument, which was changed.</param>
		void CartridgeChanged(string type);

		/// <summary>
		/// Event, which is called, if a commands has changed.
		/// </summary>
		/// <param name="obj">Command, which was changed.</param>
		void CommandChanged(Command obj);

		/// <summary>
		/// Event, which is called, if a object has changed its container.
		/// </summary>
		/// <param name="obj">Object, which changed the container.</param>
		/// <param name="fromContainer">Source container, from which the object was removed.</param>
		/// <param name="toContainer">Destination container, to which the object was moved.</param>
		void InventoryChanged(Thing obj,Thing fromContainer,Thing toContainer);

        /// <summary>
		/// Event, which is called, if the state of one or more zones had changes.
		/// </summary>
		/// <param name="zones">One or more zones, which was changed.</param>
        void ZoneStateChanged(List<Zone> zones);

		#endregion

		#region Player Dependent Informations

		/// <summary>
		/// Gets the device type.
		/// </summary>
		/// <returns>String with the device name.</returns>
        string GetDevice();

		/// <summary>
		/// Gets the device identifier.
		/// </summary>
		/// <returns>String with device identifier (like S/N).</returns>
        string GetDeviceId();

		/// <summary>
		/// Gets the version of the userinterface.
		/// </summary>
		/// <returns>Version of the userinterface.</returns>
        string GetVersion();

		#endregion

		#region Synchronizations

		/// <summary>
		/// Syncronize timer events with the userinterface.
		/// </summary>
		/// <param name="Tick">Function to call at userinterface main thread.</param>
		/// <param name="source">Object for which this function is called.</param>
		/// <param name="e">Arguments from event.</param>
		void Syncronize ( SyncronizeTick Tick, object source );

		#endregion

	}

}
