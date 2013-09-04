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
		/// Gets the type of the Timer, among "Countdown" or "Interval".
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
