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

using System;
using NLua;

namespace WF.Player.Core
{
	public class Timer : Table
	{		
		#region Constructor

		public Timer(Engine e, LuaTable t)
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
				return (TimerType)Enum.Parse(typeof(TimerType), GetString("Type"), true);
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
