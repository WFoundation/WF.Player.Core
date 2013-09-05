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
		Details
		//DialogScreen // TODO: Not part of the specification?
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
