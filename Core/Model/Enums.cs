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
}
