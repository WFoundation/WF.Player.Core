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

namespace WF.Player.Core
{
	/// <summary>
	/// A kind of screen that displays game-related information to the players.
	/// </summary>
	public enum ScreenType
	{
		Main = 0,
		Locations,
		Items,
		Inventory,
		Tasks,
		Details,
		Dialog
	}

	/// <summary>
	/// A level of importance associated to log messages.
	/// </summary>
	public enum LogLevel
	{
		Debug = 0,
		Cartridge,
		Info,
		Warning,
		Error
	}

	/// <summary>
	/// A type of media.
	/// </summary>
	public enum MediaType
	{
		BMP = 1,
		PNG = 2,
		JPG = 3,
		GIF = 4,
		WAV = 17,
		MP3 = 18,
		FDL = 19
	}

	/// <summary>
	/// Units of distance.
	/// </summary>
	public enum DistanceUnit
	{
		Meters,
		Kilometers,
		Miles,
		Feet,
		NauticalMiles
	}

	/// <summary>
	/// A type of input requested to the player.
	/// </summary>
	public enum InputType
	{
		Text,
		MultipleChoice,
		Unknown
	}

	/// <summary>
	/// A type of timer run by a cartridge.
	/// </summary>
	public enum TimerType
	{
		Countdown,
		Interval
	}

	/// <summary>
	/// A kind of result a message box can have.
	/// </summary>
	public enum MessageBoxResult
	{
		FirstButton,
		SecondButton,
		Cancel
	}
}
