﻿/// WF.Player.Core - A Wherigo Player Core for different platforms.
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

using System;
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

	/// <summary>
	/// Defines extensions for enums in the model.
	/// </summary>
	public static class EnumExtensions
	{
		/// <summary>
		/// Gets the symbol of the unit, as defined in the international system of units.
		/// </summary>
		/// <param name="unit">Unit of distance to get the symbol of.</param>
		/// <returns>The standard unit symbol or abreviation.</returns>
		public static string ToSymbol(this DistanceUnit unit)
		{
			switch (unit)
			{
				case DistanceUnit.Meters:
					return "m";
					
				case DistanceUnit.Kilometers:
					return "km";
				
				case DistanceUnit.Miles:
					return "mi";

				case DistanceUnit.Feet:
					return "ft";

				case DistanceUnit.NauticalMiles:
					return "nmi";

				default:
					throw new NotImplementedException(String.Format("Unexpected unit {0} is not supported."));
			}
		}

		/// <summary>
		/// Gets a factor representing how many of this distance unit fits in one meter.
		/// </summary>
		/// <returns>A conversion factor, used to convert from meters to this unit.</returns>
		public static double GetConversionFactor(this DistanceUnit unit)
		{
			switch (unit)
			{
				case DistanceUnit.Meters:
					return 1d;

				case DistanceUnit.Kilometers:
					return 1d / 1000;

				case DistanceUnit.Miles:
					return 1d / 1609.344;

				case DistanceUnit.Feet:
					return 3.2808399d;

				case DistanceUnit.NauticalMiles:
					return 1d / 1852.216;

				default:
					throw new NotImplementedException(String.Format("Unexpected unit {0} is not supported."));
			}
		}
	}
}