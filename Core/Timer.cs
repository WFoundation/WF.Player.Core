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
using NLua;
using WF.Player.Core.Engines;

namespace WF.Player.Core
{
	/// <summary>
	/// A timer clock relevant to the game.
	/// </summary>
	public class Timer : Table
	{		
		#region Constructor

		internal Timer(Engine e, LuaTable t)
			: base(e, t)
		{
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the type of the Timer.
		/// </summary>
		public TimerType Type
		{
			get
			{
				return GetEnum<TimerType>("Type");
			}
		}

		/// <summary>
		/// Gets how much time remains before this Timer's OnElapsed event will be
		/// raised next.
		/// </summary>
		public TimeSpan Remaining
		{
			get
			{
				return SecondsFieldToTimeSpan("Remaining");
			}
		}

		/// <summary>
		/// Gets how much time has elapsed since the start of the timer.
		/// </summary>
		public TimeSpan Elapsed
		{
			get
			{
				return SecondsFieldToTimeSpan("Elapsed");
			}
		}

		/// <summary>
		/// Gets how much time the timer runs before it elapses.
		/// </summary>
		public TimeSpan Duration
		{
			get
			{
				return SecondsFieldToTimeSpan("Duration");
			}
		}

		#endregion

		private TimeSpan SecondsFieldToTimeSpan(string field)
		{
			return new TimeSpan(0, 0, Convert.ToInt32(GetDouble(field)));
		}
	}
}
