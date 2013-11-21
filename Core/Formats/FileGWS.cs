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
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using WF.Player.Core.Utils;
using WF.Player.Core.Engines;
using WF.Player.Core.Lua;

namespace WF.Player.Core.Formats
{
	internal class FileGWS
	{
		
		 /*
		 Save File Data Format Emulator (GWS File)
		 
		 Signature: 02 0A 53 59 4E 43 00
		 Length of header in bytes: 4 byte
		 Name of cartridge as zero terminated string
		 Date of creation of cartridge: 8 byte (long in seconds since 2004-02-10 01:00)
		 Name of downloader? as zero terminated string
		 Name of device as zero terminated string
		 Name of player? as zero terminated string
		 Date of saving the cartridge: 8 byte (long in seconds since 2004-02-10 01:00)
		 Name of the save file as zero terminated string
		 Latitude: 8 byte (double)
		 Longitude: 8 byte (double)
		 Altitude: 8 byte (double)
		 
		 Number of objects + 1 (#AllZObjects + 1 for player): 4 byte
		 
		 For number of objects
		   Object name length: 4 bytes
		   Object name: length byte
		 End
		 
		 Start of table (05)
		   Player as ZCharacter
		 End of table (06)
		 
		 For number of objects
		   Object name length: 4 bytes
		   Object name: length byte
		   Start of table
		     Data for each object
		   End of table
		 End
		 
		 
		 Type of entry (first byte):
		 
		 01: bool + 1 byte (0 = false / 1 = true), also nil as false (01 00)
		 02: number + 8 Byte for value (double)
		 03: string + 4 byte length + characters without \0
		 04: function + 4 byte length + bytes of dump
		 05: start of table
		 06: end of table
		 07: reference + 2 byte for ObjIndex
		 08: object + 4 byte length of type name string 
		 */

		private Engine engine;
		private SafeLua luaState;
		private Cartridge cartridge;

		internal FileGWS(Engine engine)
		{
			this.engine = engine;
			this.luaState = engine.SafeLuaState;
			this.cartridge = engine.Cartridge;
		}
		
		public void Load(Stream stream)
		{
			string objectType;
			byte[] signatureGWS = new byte[] { 0x02, 0x0A, 0x53, 0x59, 0x4E, 0x43, 0x00 };

			BinaryReader input = new BinaryReader(stream);

			// Read signature and version
			byte[] signature = input.ReadBytes(7);

			// Check, if signature is of GWS file
			if (!signature.Equals(signatureGWS))
				throw new Exception("Trying to load a file that is not a GWS.");

			int lengthOfHeader = input.ReadInt32();
			string cartName = readCString(input);
			DateTime cartCreateDate = new DateTime(2004, 02, 10, 01, 00, 00).AddSeconds(input.ReadInt64());

			// Belongs this GWS file to the cartridge
			if (!cartCreateDate.Equals(cartridge.CreateDate))
				throw new Exception("Tring to load a GWS file with different creation date of cartridge.");

			string cartPlayerName = readCString(input);
			string cartDeviceName = readCString(input);
			string cartDeviceID = readCString(input);
			DateTime cartSaveDate = new DateTime(2004, 02, 10, 01, 00, 00).AddSeconds(input.ReadInt64());
			string cartSaveName = readCString(input);
			input.ReadDouble();	// Latitude of last position
			input.ReadDouble();	// Longitude of last position
			input.ReadDouble();	// Altitude of last position

			// TODO
			// Check, if all fields are the same as the fields from the GWC cartridge.
			// If not, than ask, if we should go on, even it could get problems.

			int numOfObjects = input.ReadInt32();
			int numAllZObjects = luaState.SafeCount(luaState.SafeGetField<LuaTable>(cartridge.WIGTable, "AllZObjects"));

			for (int i = 1; i < numOfObjects; i++)
			{
				objectType = readString(input);
				if (i > numAllZObjects)
				{
					// Create new objects
					// TODO: Cartridge=
					luaState.SafeDoString("Wherigo." + objectType + "()", "");
				}
				else
				{
					// TODO: Check, if objectType and real type of object are the same
				}
			}

			LuaTable obj = engine.Player.WIGTable;
			objectType = readString(input);

			byte b = input.ReadByte();

			readTable(input, obj);

			LuaTable allZObjects = luaState.SafeGetField<LuaTable>(cartridge.WIGTable, "AllZObjects");
			for (int i = 0; i < numAllZObjects; i++)
			{
				objectType = readString(input);
				b = input.ReadByte();
				if (b != 5)
				{
					// error
					throw new InvalidOperationException();
				}
				else
				{
					obj = luaState.SafeGetField<LuaTable>(allZObjects, i);
					readTable(input, obj);
					luaState.SafeCallSelf(obj, "deserialize");
					
				}
			}

			input.Close();
		}

		/// <summary>
		/// Read the table obj from the binary reader input. 
		/// </summary>
		/// <param name="output">BinaryReader to read the table from.</param>
		/// <param name="obj">Table to read from binary writer.</param>
		private void readTable(BinaryReader input, LuaTable obj)
		{
			string className = "unknown";
			LuaFunction rawset = null;
			LuaTable tab;
			LuaValue key = 1;

			if (obj != null)
			{
				className = luaState.SafeGetField<string>(obj, "ClassName");
				if (className != null)
					rawset = luaState.SafeGetField<LuaFunction>(obj, "rawset");
			}

			byte b = input.ReadByte();

			while (b != 6)
			{
				// Key
				switch (b)
				{
					case 1:
						key = input.ReadByte() == 0 ? false : true;
						break;
					case 2:
						key = input.ReadDouble();
						break;
					case 3:
						key = readString(input);
						break;
					default:
						throw new Exception(String.Format("Unsupported table key: {0} at byte {1}", key, input.BaseStream.Position));
				}

				b = input.ReadByte();

				// Value
				switch (b)
				{
					case 1:
						if (className != null)
							luaState.SafeCallRaw(rawset, obj, key, input.ReadByte() == 0 ? false : true);
						else
							luaState.SafeSetField(obj, (LuaValue)key, input.ReadByte() == 0 ? false : true);
						break;
					case 2:
						if (className != null)
							luaState.SafeCallRaw(rawset, obj, key, input.ReadDouble());
						else
							luaState.SafeSetField(obj, (LuaValue)key, input.ReadDouble());
						break;
					case 3:
						if (className != null)
							luaState.SafeCallRaw(rawset, obj, key, readString(input));
						else
							luaState.SafeSetField(obj, (LuaValue)key, readString(input));
						break;
					case 4:
						byte[] chunk = input.ReadBytes(input.ReadInt32());
						if (className != null)
							luaState.SafeCallRaw(rawset, obj, key, (LuaFunction)luaState.SafeLoadString(chunk, key.ToString()));
						else
							luaState.SafeSetField(obj, (LuaValue)key, (LuaFunction)luaState.SafeLoadString(chunk, key.ToString()));
						break;
					case 5:
						tab = luaState.SafeEmptyTable();
						if (className != null)
							luaState.SafeCallRaw(rawset, obj, key, tab);
						else
							luaState.SafeSetField(obj, (LuaValue)key, tab);
						readTable(input, luaState.SafeGetField<LuaTable>(obj, (LuaValue)key));
						break;
					case 7:
						if (className != null)
							luaState.SafeCallRaw(rawset, obj, key, luaState.SafeGetInnerField<LuaTable>(cartridge.WIGTable, "AllZObjects", input.ReadInt16()));
						else
							luaState.SafeSetField(obj, (LuaValue)key, luaState.SafeGetInnerField<LuaTable>(cartridge.WIGTable, "AllZObjects", input.ReadInt16()));
						break;
					case 8:
						tab = (LuaTable)luaState.SafeDoString("return Wherigo." + readString(input) + "()", "")[0];
						if (className != null)
							luaState.SafeCallRaw(rawset, obj, key, tab);
						else
							luaState.SafeSetField(obj, (LuaValue)key, tab);

						// After an object, there is always a table with the content
						input.ReadByte();
						readTable(input, tab);
						break;
				}

				b = input.ReadByte();
			}
		}

		/// <summary>
		/// Save active cartridge to a gws file.
		/// </summary>
		/// <param name="stream">Stream to write the data to.</param>
		/// <param name="saveName">Description for the save file, which is put into the file.</param>
		public void Save(Stream stream, string saveName = "UI initiated sync")
		{
			BinaryWriter output = new BinaryWriter(stream);
			byte[] className;
			Cartridge cartridge = engine.Cartridge;
			SafeLua luaState = engine.SafeLuaState;

			// Write signature and version
			output.Write(new byte[] { 0x02, 0x0A, 0x53, 0x59, 0x4E, 0x43, 0x00 });
			output.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 });
			int lengthOfHeader = 0;
			lengthOfHeader += writeCString(output, cartridge.Name);
			output.Write(BitConverter.GetBytes((long)((cartridge.CreateDate.Ticks - new DateTime(2004, 02, 10, 01, 00, 00).Ticks) / TimeSpan.TicksPerSecond)));
			lengthOfHeader += 8;
			lengthOfHeader += writeCString(output, cartridge.Player);
			// MUST be "Windows PPC" for Emulator
			lengthOfHeader += writeCString(output, "Windows PPC"); // TODO: Replace with ui.GetDevice ());
			// MUST be "Desktop" for Emulator
			lengthOfHeader += writeCString(output, "Desktop"); // TODO: Replace with ui.GetDeviceId());
			output.Write(BitConverter.GetBytes((long)((DateTime.Now.Ticks - new DateTime(2004, 02, 10, 01, 00, 00).Ticks) / TimeSpan.TicksPerSecond)));
			lengthOfHeader += 8;
			lengthOfHeader += writeCString(output, saveName);
			output.Write(BitConverter.GetBytes(engine.Latitude));
			lengthOfHeader += 8;
			output.Write(BitConverter.GetBytes(engine.Longitude));
			lengthOfHeader += 8;
			output.Write(BitConverter.GetBytes(engine.Altitude));
			lengthOfHeader += 8;

			var pos = output.BaseStream.Position;
			output.BaseStream.Position = 7;
			output.Write(BitConverter.GetBytes(lengthOfHeader));
			output.BaseStream.Position = pos;

			int numAllZObjects = luaState.SafeCount(luaState.SafeGetField<LuaTable>(cartridge.WIGTable, "AllZObjects"));
			output.Write(numAllZObjects);

			for (int i = 1; i < numAllZObjects; i++)
			{
				className = Encoding.UTF8.GetBytes(luaState.SafeGetInnerField<LuaString>(cartridge.WIGTable, "AllZObjects", i, "ClassName").ToString());
				output.Write(className.Length);
				output.Write(className);
			}

			LuaTable obj = engine.Player.WIGTable;
			className = Encoding.UTF8.GetBytes(luaState.SafeGetField<string>(obj, "ClassName"));
			output.Write(className.Length);
			output.Write(className);
			LuaTable data = (LuaTable)luaState.SafeCallSelf(obj, "serialize")[0];
			writeTable(output, data);

			for (int i = 0; i < numAllZObjects; i++)
			{
				obj = luaState.SafeGetInnerField<LuaTable>(cartridge.WIGTable, "AllZObjects", i);
				className = Encoding.UTF8.GetBytes(luaState.SafeGetField<string>(obj, "ClassName"));
				output.Write(className.Length);
				output.Write(className);
				data = (LuaTable)luaState.SafeCallSelf(obj, "serialize")[0];
				writeTable(output, data);
			}

			output.Flush();
			output.Close();
		}

		/// <summary>
		/// Write the table obj to the binary writer output. 
		/// </summary>
		/// <param name="output">BinaryWriter to write the table to.</param>
		/// <param name="obj">Table to write to binary writer.</param>
		private void writeTable(BinaryWriter output, LuaTable obj)
		{
			output.Write((byte)5);

			var entry = luaState.SafeGetEnumerator(obj);
			while (entry.MoveNext())
			{
				// Save key
				if (entry.Current.Key is LuaBoolean)
				{
					output.Write((byte)1);
					output.Write((bool)entry.Current.Key.ToBoolean() ? (byte)1 : (byte)0);
				}
				if (entry.Current.Key is LuaNumber)
				{
					output.Write((byte)2);
					output.Write((double)entry.Current.Key.ToNumber());
				}
				if (entry.Current.Key is LuaString)
				{
					output.Write((byte)3);
					byte[] array = Encoding.UTF8.GetBytes((string)entry.Current.Key.ToString());
					output.Write(array.Length);
					output.Write(array);
				}

				// Save value
				if (entry.Current.Value is LuaBoolean)
				{
					output.Write((byte)1);
					output.Write((bool)entry.Current.Value.ToBoolean() ? (byte)1 : (byte)0);
				}
				if (entry.Current.Value is LuaNumber)
				{
					output.Write((byte)2);
					output.Write((double)entry.Current.Value.ToNumber());
				}
				if (entry.Current.Value is LuaString)
				{
					output.Write((byte)3);
					byte[] array = toArray((string)entry.Current.Value.ToString());
					output.Write(array.Length);
					output.Write(array);
				}
				if (entry.Current.Value is LuaFunction)
				{
					output.Write((byte)4);
					//byte[] array = toArray((string)luaState.GetFunction("string.dump").Call((LuaFunction)entry.Value)[0]);
					byte[] array = toArray((string)luaState.SafeCallRaw("string.dump", (LuaFunction)entry.Current.Value)[0].ToString());
					// TODO: Delete
					//					string path = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
					//					string filePath = Path.Combine(path, "out.txt");
					//					BinaryWriter bw = new BinaryWriter (new FileStream(filePath,FileMode.Create));
					//					bw.Write (array);
					//					bw.Close ();
					output.Write(array.Length);
					output.Write(array);
				}
				if (entry.Current.Value is LuaTable)
				{
					string className = luaState.SafeGetField<string>((LuaTable)entry.Current.Value, "ClassName");

					if (className != null && (className.Equals("Distance") || className.Equals("ZonePoint") || className.Equals("ZCommand") || className.Equals("ZReciprocalCommand")))
					{
						output.Write((byte)8);
						byte[] array = Encoding.UTF8.GetBytes(className);
						output.Write(array.Length);
						output.Write(array);
						LuaTable data = (LuaTable)luaState.SafeCallSelf((LuaTable)entry.Current.Value, "serialize")[0];
						writeTable(output, data);
					}
					else if (className != null && (className.Equals("ZCartridge") || className.Equals("ZCharacter") || className.Equals("ZInput") || className.Equals("ZItem") ||
						className.Equals("ZMedia") || className.Equals("Zone") || className.Equals("ZTask") || className.Equals("ZTimer")))
					{
						output.Write((byte)7);
						output.Write(Convert.ToInt16(luaState.SafeGetField<object>((LuaTable)entry.Current.Value, "ObjIndex")));
					}
					else
					{
						LuaTable data = (LuaTable)entry.Current.Value;
						LuaFunction lf = luaState.SafeGetField<LuaFunction>((LuaTable)entry.Current.Value, "serialize");
						if (lf != null)
							data = (LuaTable)luaState.SafeCallSelf((LuaTable)entry.Current.Value, "serialize")[0]; //().CallSelf("serialize");
						writeTable(output, data);
					}
				}
			}

			output.Write((byte)6);
		}

		/// <summary>
		/// Read a null terminated string from binary stream.
		/// </summary>
		/// <param name="reader">Binary stream with file as input.</param>
		/// <returns>String, which represents the C# string.</returns>
		private string readCString(BinaryReader input)
		{
			var bytes = new List<byte>();
			byte b;

			while ((b = input.ReadByte()) != 0)
			{
				bytes.Add(b);
			}

			return Encoding.UTF8.GetString(bytes.ToArray(), 0, bytes.ToArray().Length);
		}

		/// <summary>
		/// Write a null terminated string to binary stream.
		/// </summary>
		/// <param name="reader">Binary stream with file as output.</param>
		/// <returns>String, which represents the C# string.</returns>	
		private int writeCString(BinaryWriter output, string str)
		{
			byte[] array = Encoding.UTF8.GetBytes(str);

			output.Write(array);
			output.Write((byte)0);

			return array.Length + 1;
		}

		/// <summary>
		/// Read string from binary reader. First four bytes are the length, the next length bytes are the string. 
		/// </summary>
		/// <param name="input">BinaryReader to read from.</param>
		/// <returns>String readed from binary reader.</returns>
		private string readString(BinaryReader input)
		{
			var b = input.ReadBytes(input.ReadInt32()).ToArray();

			return Encoding.UTF8.GetString(b, 0, b.Length);
		}

		/// <summary>
		/// Convert byte array to a string.
		/// </summary>
		/// <param name="array">Byte array to convert to string.</param>
		/// <returns>String with converted byte array.</returns>
		private string toString(byte[] array)
		{
			StringBuilder s = new StringBuilder(array.Length);

			foreach (byte b in array)
				s.Append(b);

			return s.ToString();
		}

		/// <summary>
		/// Convert string to byte array.
		/// </summary>
		/// <param name="str">String to convert to byte array.</param>
		/// <returns>Byte array with converted string.</returns>
		private byte[] toArray(string str)
		{
			if (str == null)
				return null;

			byte[] result = new byte[str.Length];

			for (int i = 0; i < str.Length; i++)
				result[i] = (byte)(str[i] & 0xff);

			return result;
		}
	}
}
