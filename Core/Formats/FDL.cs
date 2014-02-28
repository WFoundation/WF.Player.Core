///
/// WF.Player.Core - A Wherigo Player Core for different platforms.
/// Copyright (C) 2012-2014  Dirk Weltz <web@weltz-online.de>
/// Copyright (C) 2012-2014  Brice Clocher <contact@cybisoft.net>
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
/// 

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace WF.Player.Core.Formats
{
	/// <summary>
	/// A loader and converter of Garmin FDL sound files.
	/// </summary>
	public class FDL
	{
		/*
		 * Garmin FDL File Format
		 * 
		 * All fields are 2-bytes, Little-endian, SHORTs.
		 * 
		 * [Tone Block]
		 * 
		 * SHORT Silence: 0 = the tone is not silent, 1 = the tone is silent.
		 *				 Regardless of the value of Silence, the following
		 *				 two fields are present.
		 *				 e.g. 01 00, gives 01, gives 1.
		 *				 Other values than 0 or 1 are UNEXPECTED.
		 *				 
		 * SHORT Frequency: Frequency of the tone, in Hz.
		 *				 e.g. 4A 01, gives 14A, gives 330 Hz.
		 *				 If Silence==1, this should be 0.
		 *				 Behavior is UNKNOWN if Silence==1 && Frequency!=0.
		 *	
		 * SHORT Duration: Duration of the tone, in milliseconds.
		 *				e.g. 5E 01, gives 15E, gives 350 ms.
		 *				If Silence==1, this indicates how long
		 *				the silence should last. If Silence==0,
		 *				this indicates how long the tone should play.
		 */
		
		#region Nested Classes

		/// <summary>
		/// Describes a single tone of sound.
		/// </summary>
		public class Tone
		{
			/// <summary>
			/// Gets or sets if this tone is a silence.
			/// </summary>
			public bool IsSilence { get; set; }

			/// <summary>
			/// Gets or sets the frequency of this tone.
			/// </summary>
			/// <value>A frequency in Hertz, greater than or
			/// equals to 0.</value>
			public int Frequency { get; set; }

			/// <summary>
			/// Gets or sets the duration of this tone.
			/// </summary>
			public TimeSpan Duration { get; set; }

			/// <summary>
			/// Gets or sets the volume of this tone.
			/// </summary>
			/// <value>A number between 0 (no sound) and
			/// 1 (maximum sound).</value>
			public double Volume { get; set; }
		}

		#endregion
		
		#region Constants
		public const double WAV_FREQ = 44100.0;
		public const int MAX_AMPLITUDE = 1000;

		private readonly double TWO_15_1000 = Math.Pow(2, 15) / 1000;
		#endregion

		/// <summary>
		/// Converts an FDL stream to a WAV stream.
		/// </summary>
		/// <param name="inputFdl">A stream of a Garmin FDL file.</param>
		/// <returns>A memory stream of a WAV file corresponding to the
		/// FDL file.</returns>
		public Stream ConvertToWav(Stream inputFdl)
		{
			// Loads the file.
			IEnumerable<Tone> tones = Load(inputFdl);

			// Converts the tones to Wav.
			return CreateBeepWav(tones);
		}

		/// <summary>
		/// Loads a FDL file as a sequence of tones.
		/// </summary>
		/// <param name="inputFdl">A stream of a Garmin FDL file.</param>
		/// <returns>An enumerable of Tones represented by the FDL
		/// file. May be empty, but never null.</returns>
		/// <exception cref="InvalidOperationException">Invalid file format.
		/// Read exception message for more details.</exception>
		public IEnumerable<Tone> Load(Stream inputFdl)
		{
			// Garmin FDL files are header-less.
			// For this reason, we cannot pre-determine if this particular
			// FDL file is of the right file format.
			// Therefore, we'll just crash whenever unexpected values appear,
			// but this does not guarantee that non-Garmin FDL files will
			// make this method crash.

			List<Tone> tones = new List<Tone>();

			// Resets the stream.
			inputFdl.Seek(0, SeekOrigin.Begin);

			while (inputFdl.Position < inputFdl.Length)
			{
				// A wild tone block has appeared! :)

				// Reads the 3 consecutive little-endian shorts.
				short silence = ReadShort(inputFdl);
				short freq = ReadShort(inputFdl);
				short duration = ReadShort(inputFdl);

				// File format checks.
				if (silence < 0 || silence > 1)
					throw new InvalidOperationException(String.Format("Invalid file format: Expected Silence 0 or 1, got {0}.", silence));
				if (silence == 1 && freq != 0)
					throw new InvalidOperationException(String.Format("Invalid file format: Tone is silence with non-zero frequency: {0}.", freq));

				// Conforms the values and makes a tone.
				tones.Add(new Tone()
				{
					IsSilence = silence == 1,
					Frequency = (int) freq,
					Duration = TimeSpan.FromMilliseconds((double) duration),
					Volume = 1d
				});
			}

			return tones;
		}

		#region Wave Beep Generation
		private Stream CreateBeepWav(IEnumerable<Tone> tones)
		{
			// Adapted from http://tech.reboot.pro/showthread.php?tid=2866

			// Creates the Wave stream.
			MemoryStream ms = CreateWavStream(Convert.ToInt32(tones.Sum(t => t.Duration.TotalMilliseconds)));

			// Writes each tone.
			foreach (var tone in tones)
			{
				WriteWavBeep(tone, ms);
			}

			// Finishes writing and resets the stream.
			ms.Flush();
			ms.Seek(0, SeekOrigin.Begin);

			return ms;
		}

		private void WriteWavBeep(Tone tone, MemoryStream ms)
		{
			// Adapted from http://tech.reboot.pro/showthread.php?tid=2866

			// Sanity checks.
			if (tone.Volume > 1 || tone.Volume < 0)
			{
				throw new ArgumentOutOfRangeException("tone.Volume", "tone volume must be between 0 and 1.");
			}
			else if (tone.Frequency < 0)
			{
				throw new ArgumentOutOfRangeException("freq", "frequency should be 0 or more.");
			}

			// Computes intermediate values.
			int smp = Convert.ToInt32(WAV_FREQ * tone.Duration.TotalMilliseconds / 1000); // Amount of samples.
			int amp = Convert.ToInt32(MAX_AMPLITUDE * tone.Volume); // Amplitude

			// Writes each sample.
			for (int i = 0; i < smp; i++)
			{
				Int16 s = Convert.ToInt16(((amp * TWO_15_1000) - 1) * Math.Sin((2 * Math.PI * tone.Frequency / WAV_FREQ) * i));
				WriteShort(ms, s);
				WriteShort(ms, s); // We write it twice (stereo?).
			}
		}

		private MemoryStream CreateWavStream(int duration)
		{
			// Adapted from http://tech.reboot.pro/showthread.php?tid=2866

			// Computes the header.
			int smp = (int)WAV_FREQ * duration / 1000; // Amount of samples.
			int bytes = smp * 4; // Total amount of bytes.
			int[] h = { 0X46464952, 0x24 + bytes, 0X45564157, 0X20746D66, 0x10, 0X20001, 0xAC44, 0x2B110, 0X100004, 0X61746164, bytes };

			// Writes the header.
			MemoryStream ms = new MemoryStream(44 + bytes);

			foreach (int item in h)
			{
				WriteInt(ms, item);
			}

			return ms;
		}

		#endregion

		#region Stream Little-endian Read and Write
		/// <summary>
		/// Writes a 4 bytes, little-endian int to a stream.
		/// </summary>
		/// <param name="ms"></param>
		/// <param name="value"></param>
		private void WriteInt(MemoryStream ms, int value)
		{
			// Gets the bytes for this integer dependending on the endianness
			// of the system.
			byte[] bytes = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian)
			{
				// We need the array to be little endian, and we got
				// big endian.
				bytes = bytes.Reverse().ToArray();
			}

			// Writes the array.
			ms.Write(bytes, 0, bytes.Length);
		}

		/// <summary>
		/// Writes a 2 bytes, little-endian short to a stream.
		/// </summary>
		/// <param name="ms"></param>
		/// <param name="value"></param>
		private void WriteShort(MemoryStream ms, short value)
		{
			// Gets the bytes for this integer dependending on the endianness
			// of the system.
			byte[] bytes = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian)
			{
				// We need the array to be little endian, and we got
				// big endian.
				bytes = bytes.Reverse().ToArray();
			}

			// Writes the array.
			ms.Write(bytes, 0, bytes.Length);
		}

		/// <summary>
		/// Reads a little-endian, 2-bytes short from a StreamReader.
		/// </summary>
		/// <param name="sr"></param>
		/// <returns></returns>
		private short ReadShort(Stream sr)
		{
			// Reads two consecutive bytes.
			int b1 = sr.ReadByte();
			int b2 = sr.ReadByte();
			if (b1 == -1 || b2 == -1)
				throw new InvalidOperationException("Invalid file format: Unexpected end of stream.");

			// Makes the array of bytes in the right order for conversion.
			byte[] bytes;
			if (BitConverter.IsLittleEndian)
			{
				// Keep little endian order.
				bytes = new byte[] { (byte)b1, (byte)b2 };
			}
			else
			{
				// Reverse the order to big endian because the BitConverter
				// order is big endian.
				bytes = new byte[] { (byte)b2, (byte)b1 };
			}

			// Converts the byte array to a short.
			return BitConverter.ToInt16(bytes, 0);
		} 
		#endregion
	}
}
