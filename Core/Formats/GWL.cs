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
using System.IO;
using WF.Player.Core.Engines;

namespace WF.Player.Core.Formats
{
	/// <summary>
	/// A generator of Groundspeak Wherigo log files.
	/// </summary>
	public class GWL : IDisposable
	{
		/*
		 * Groundspeak Wherigo Log file format
		 * 
		 * One line per log entry.
		 * Each line follows the format:
		 * TIMESTAMP|LATITUDE|LONGITUDE|ALTITUDE|ACCURACY|MESSAGE
		 * 
		 * where
		 * 
		 * TIMESTAMP is the local time of the message occurence, formatted as yyyyMMddhhmmss
		 * LATITUDE and LONGITUDE are formatted as 0.00000 floating numbers with sign
		 * ALTITUDE and ACCURACY are formatted as 0.000 floating numbers with sign
		 * MESSAGE is the UTF-8 encoded string of the message
		 * 
		 * Each line ends with a platform-specific new line character.
		 */

		#region Properties

		/// <summary>
		/// Gets or sets the minimal log level of entries allowed to be written
		/// to this GWL stream.
		/// </summary>
		public LogLevel MinimalLogLevel { get; set; }

		#endregion

		#region Fields

		private StreamWriter _writer;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructs a GWL writer instance, capable of writing GWL
		/// log entries to stream.
		/// </summary>
		/// <param name="stream"></param>
		public GWL(Stream stream)
		{
			_writer = new StreamWriter(stream);
		}

		#endregion

		#region Destructors

		public void Dispose()
		{
			Dispose(true);

			// Requests the GC to not finalize this object (best practice).
			GC.SuppressFinalize(this);
		}

		~GWL()
		{
			Dispose(false);
		}

		private void Dispose(bool disposeManagedResources)
		{
			if (disposeManagedResources)
			{
				_writer.Dispose();
			}
		}

		#endregion

		/// <summary>
		/// Writes a log entry in the GWL stream if the level is greater than
		/// <code>MinimalLogLevel</code>
		/// </summary>
		/// <param name="level">Level of the log entry.</param>
		/// <param name="message">Message of the log entry.</param>
		/// <param name="engine">Engine instance used to geotag the log entry.</param>
		/// <returns>True if the log entry's level was above or equal to the minimal required
		/// level and the entry was successfully written on the stream, false otherwise.</returns>
		public bool TryWriteLogEntry(LogLevel level, string message, Engine engine)
		{
			// Checks if the entry is allowed to be displayed at all.
			if (level.CompareTo(MinimalLogLevel) < 0)
			{
				return false;
			}

			// Tries to write the message.
			try
			{
				_writer.WriteLine(engine.CreateLogMessage(message));

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
