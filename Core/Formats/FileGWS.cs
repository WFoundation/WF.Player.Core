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
using Eluant;
using WF.Player.Core.Data;
using WF.Player.Core.Data.Lua;

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

        //private SafeLua _luaState;
		private Cartridge _cartridgeEntity;
        //private LuaTable _cartridge;
        //private LuaTable _player;
        private LuaDataContainer _cartridge;
        private LuaDataContainer _player;
		private IPlatformHelper _platformHelper;
		private LuaDataFactory _dataFactory;
		private double _latitude;
		private double _longitude;
		private double _altitude;

		private LuaDataContainer _allZObjects;

		internal FileGWS(
			Cartridge cart, 
			Character player, 
			IPlatformHelper platformHelper, 
			LuaDataFactory dataFactory)
		{
            //this._luaState = safeLuaState;
			this._dataFactory = dataFactory;
			this._cartridgeEntity = cart;
            //this._cartridge = dataFactory.GetNativeContainer(cart);
            //this._player = dataFactory.GetNativeContainer(player);
            this._cartridge = (LuaDataContainer)cart.DataContainer;
            this._player = (LuaDataContainer)player.DataContainer;
			this._platformHelper = platformHelper;
            ZonePoint pos = player.ObjectLocation;
			this._latitude = pos.Latitude;
			this._longitude = pos.Longitude;
			this._altitude = pos.Latitude;
		}

        #region Loading
        public void Load(Stream stream)
        {
            string objectType;
            byte[] signatureGWS = new byte[] { 0x02, 0x0A, 0x53, 0x59, 0x4E, 0x43, 0x00 };

            BinaryReader input = new BinaryReader(stream);

            // Read signature and version
            byte[] signature = input.ReadBytes(7);

            // Check, if signature is of GWS file
            if (!signatureGWS.SequenceEqual(signature))
                throw new Exception("Trying to load a file that is not a GWS.");

            int lengthOfHeader = input.ReadInt32();
            string cartName = readCString(input);
            DateTime cartCreateDate = new DateTime(2004, 02, 10, 01, 00, 00).AddSeconds(input.ReadInt64());

            // Belongs this GWS file to the cartridge
            if (!cartCreateDate.Equals(_cartridgeEntity.CreateDate))
				throw new Exception("Trying to load a GWS file with different creation date of cartridge.");

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
            //int numAllZObjects = _luaState.SafeCount(_luaState.SafeGetField<LuaTable>(_cartridge, "AllZObjects"));
			_allZObjects = _cartridge.GetContainer("AllZObjects"); //.GetWherigoObjectList<WherigoObject>("AllZObjects");
            int numAllZObjects = _allZObjects.Count;

			for (int i = 1; i < numOfObjects; i++)
            {
                objectType = readString(input);
				if (i > numAllZObjects - 1)
                {
                    // Create new objects
                    // TODO: Cartridge=
                    ////_luaState.SafeDoString("Wherigo." + objectType + "()", "");

                    // TODO: Object creation can be done using:
					WherigoObject wo = _dataFactory.CreateWherigoObject(objectType, new object[] {_cartridge});
                }
                else
                {
                    // TODO: Check, if objectType and real type of object are the same
                }
            }

			// Now update allZObjects, because it could be, that new ones are created
			_allZObjects = _cartridge.GetContainer ("AllZObjects");
			numAllZObjects = _allZObjects.Count;

            //LuaTable obj = _player;
            LuaDataContainer obj = _player;
            objectType = readString(input);

			// Read begin table (5) for player
            byte b = input.ReadByte();

			// Read data for player
            readTable(input, obj);

            //LuaTable allZObjects = _luaState.SafeGetField<LuaTable>(_cartridge, "AllZObjects");
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
                    //obj = _luaState.SafeGetField<LuaTable>(allZObjects, i);
					obj = (LuaDataContainer)_allZObjects.GetContainer(i);
                    readTable(input, obj);
                    //_luaState.SafeCallSelf(obj, "deserialize");
                    //obj.GetProvider("deserialize", true).Execute();
					// obj.CallSelf("deserialize");
                }
            }

            input.Close();

			// Now deserialize all ZObjects
			for (int i = 0; i < numAllZObjects; i++)
			{
				obj = (LuaDataContainer)_allZObjects.GetContainer(i);
				obj.CallSelf ("deserialize");
			}

			// TODO: Update all lists
        }

        /// <summary>
        /// Read the table obj from the binary reader input. 
        /// </summary>
        /// <param name="output">BinaryReader to read the table from.</param>
        /// <param name="obj">Table to read from binary writer.</param>
        private void readTable(BinaryReader input, LuaDataContainer obj)
        {
            string className = "unknown";
            IDataProvider rawset = null;
            LuaDataContainer tab;
            object key = 1;

            if (obj != null)
            {
                //className = _luaState.SafeGetField<LuaString>(obj, "ClassName").ToString();
                className = obj.GetString("ClassName");
                if (className != null)
                    //rawset = _luaState.SafeGetField<LuaFunction>(obj, "rawset");
                    rawset = obj.GetProvider("rawset", false);
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
                        throw new Exception(String.Format("Unsupported table key: {0} at byte {1}", b, input.BaseStream.Position));
                }

                b = input.ReadByte();

                // Value
                switch (b)
                {
                    case 1:
                        SetField(obj, key, input.ReadBoolean(), rawset);
                        break;

                    case 2:
                        SetField(obj, key, input.ReadDouble(), rawset);
                        break;

                    case 3:
                        SetField(obj, key, readString(input), rawset);
                        break;

                    case 4:
                        byte[] chunk = input.ReadBytes(input.ReadInt32());
                        //if (className != null)
                        //    //_luaState.SafeCallRaw(rawset, obj, key, (LuaFunction)_luaState.SafeLoadString(chunk, key.ToString()));
                        //    rawset.Execute(obj, key, _dataFactory.LoadProvider(chunk, key.ToString()));
                        //else
                        //    //_luaState.SafeSetField(obj, (LuaValue)key, (LuaFunction)_luaState.SafeLoadString(chunk, key.ToString()));
                        SetField(obj, key, _dataFactory.LoadProvider(chunk, key.ToString()), rawset);
                        break;
                    case 5:
                        //tab = _luaState.SafeCreateTable();
                        tab = _dataFactory.CreateContainer();
                        //if (className != null)
                        //    _luaState.SafeCallRaw(rawset, obj, key, tab);
                        //else
                        //    _luaState.SafeSetField(obj, (LuaValue)key, tab);
                        SetField(obj, key, tab, rawset);
                        //readTable(input, _luaState.SafeGetField<LuaTable>(obj, (LuaValue)key));
                        readTable(input, tab);
                        break;
					case 6:
						// End of table
						return;
						break;
					case 7:
                        //if (className != null)
                        //    _luaState.SafeCallRaw(rawset, obj, key, _luaState.SafeGetInnerField<LuaTable>(_cartridge, "AllZObjects", input.ReadInt16()));
                        //else
                        //    _luaState.SafeSetField(obj, (LuaValue)key, _luaState.SafeGetInnerField<LuaTable>(_cartridge, "AllZObjects", input.ReadInt16()));
						var objIndex = input.ReadInt16 ();
						if (objIndex == -21555)
							SetField (obj, key, _player, rawset);
						else
						SetField(obj, key, _allZObjects.GetContainer(objIndex), rawset);
                        break;
                    case 8:
                        //tab = (LuaTable)_luaState.SafeDoString("return Wherigo." + readString(input) + "()", "")[0];
                        tab = (LuaDataContainer)_dataFactory.CreateWherigoObject(readString(input)).DataContainer;
                        //if (className != null)
                        //    _luaState.SafeCallRaw(rawset, obj, key, tab);
                        //else
                        //    _luaState.SafeSetField(obj, (LuaValue)key, tab);
                        SetField(obj, key, tab, rawset);

                        // After an object, there is always a table with the content
                        input.ReadByte();
                        readTable(input, tab);
                        break;
                }

                b = input.ReadByte();
            }
        }

        private void SetField(LuaDataContainer obj, object key, object value, IDataProvider rawset)
        {
            if (rawset != null)
                rawset.Execute(obj, key, value);
            else
                obj[key] = value;
        } 
        #endregion

		/// <summary>
		/// Save active cartridge to a gws file.
		/// </summary>
		/// <param name="stream">Stream to write the data to.</param>
		/// <param name="saveName">Description for the save file, which is put into the file.</param>
		public void Save(Stream stream, string saveName = "UI initiated sync")
		{
			BinaryWriter output = new BinaryWriter(stream);
			byte[] className;

			// Write signature and version
			output.Write(new byte[] { 0x02, 0x0A, 0x53, 0x59, 0x4E, 0x43, 0x00 });
			output.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 });
			int lengthOfHeader = 0;
			lengthOfHeader += writeCString(output, _cartridgeEntity.Name);
			output.Write(BitConverter.GetBytes((long)((_cartridgeEntity.CreateDate.Ticks - new DateTime(2004, 02, 10, 01, 00, 00).Ticks) / TimeSpan.TicksPerSecond)));
			lengthOfHeader += 8;
			lengthOfHeader += writeCString(output, _cartridgeEntity.Player);
			// MUST be "Windows PPC" for Emulator
			lengthOfHeader += writeCString(output, _platformHelper.Device);
			// MUST be "Desktop" for Emulator
			lengthOfHeader += writeCString(output, _platformHelper.DeviceId);
			output.Write(BitConverter.GetBytes((long)((DateTime.Now.Ticks - new DateTime(2004, 02, 10, 01, 00, 00).Ticks) / TimeSpan.TicksPerSecond)));
			lengthOfHeader += 8;
			lengthOfHeader += writeCString(output, saveName);
			output.Write(BitConverter.GetBytes(_latitude));
			lengthOfHeader += 8;
			output.Write(BitConverter.GetBytes(_longitude));
			lengthOfHeader += 8;
			output.Write(BitConverter.GetBytes(_altitude));
			lengthOfHeader += 8;

			var pos = output.BaseStream.Position;
			output.BaseStream.Position = 7;
			output.Write(BitConverter.GetBytes(lengthOfHeader));
			output.BaseStream.Position = pos;

            //int numAllZObjects = _luaState.SafeCount(_luaState.SafeGetField<LuaTable>(_cartridge, "AllZObjects"));
			_allZObjects = _cartridge.GetContainer ("AllZObjects");
            int numAllZObjects = _allZObjects.Count;
			output.Write(numAllZObjects);

			for (int i = 1; i < numAllZObjects; i++)
			{
                //className = Encoding.UTF8.GetBytes(_luaState.SafeGetInnerField<LuaString>(_cartridge, "AllZObjects", i, "ClassName").ToString());
				className = Encoding.UTF8.GetBytes(_allZObjects.GetContainer(i).GetString("ClassName"));
				output.Write(className.Length);
				output.Write(className);
			}

			className = Encoding.UTF8.GetBytes(_allZObjects.GetContainer(numAllZObjects-1).GetString("ClassName"));

			LuaDataContainer obj = _player;
            //className = Encoding.UTF8.GetBytes(_luaState.SafeGetField<LuaString>(obj, "ClassName").ToString());
            className = Encoding.UTF8.GetBytes(obj.GetString("ClassName"));
			output.Write(className.Length);
			output.Write(className);
            //LuaTable data = (LuaTable)_luaState.SafeCallSelf(obj, "serialize")[0];
            //LuaDataContainer data = obj.GetProvider("serialize", true).FirstContainerOrDefault();
            LuaDataContainer data = obj.CallSelf("serialize");
			writeTable(output, data);

			for (int i = 0; i < numAllZObjects; i++)
			{
                //obj = _luaState.SafeGetInnerField<LuaTable>(_cartridge, "AllZObjects", i);
				obj = (LuaDataContainer)_allZObjects.GetContainer(i);
                //className = Encoding.UTF8.GetBytes(_luaState.SafeGetField<LuaString>(obj, "ClassName").ToString());
                className = Encoding.UTF8.GetBytes(obj.GetString("ClassName"));
				output.Write(className.Length);
				output.Write(className);
                //data = (LuaTable)_luaState.SafeCallSelf(obj, "serialize")[0];
                //data = obj.GetProvider("serialize", true).FirstContainerOrDefault();
                data = obj.CallSelf("serialize");
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
		private void writeTable(BinaryWriter output, LuaDataContainer obj)
		{
			output.Write((byte)5);

            //var entry = _luaState.SafeGetEnumerator(obj);
            var entry = obj.GetEnumerator();
            while (entry.MoveNext())
            {
				// Save key
				if (entry.Key is bool)
				{
					output.Write((byte)1);
					output.Write((bool)entry.Key ? (byte)1 : (byte)0);
				}
				if (entry.Key is double)
				{
					output.Write((byte)2);
					output.Write((double)entry.Key);
				}
				if (entry.Key is string)
				{
					output.Write((byte)3);
					byte[] array = Encoding.UTF8.GetBytes((string)entry.Key);
					output.Write(array.Length);
					output.Write(array);
				}

				// Save value
				if (entry.Value == null)
				{
					// Write false for null
					output.Write((byte)1);
					output.Write((byte)0);
				}
				if (entry.Value is bool)
				{
					output.Write((byte)1);
					output.Write((bool)entry.Value ? (byte)1 : (byte)0);
				}
				if (entry.Value is double || entry.Value is int)
				{
					output.Write((byte)2);
					output.Write((double)entry.Value);
				}
				if (entry.Value is string)
				{
					output.Write((byte)3);
					byte[] array = toArray((string)entry.Value);
					output.Write(array.Length);
					output.Write(array);
				}
				if (entry.Value is LuaFunction)  // New: LuaDataProvider)
				{
					output.Write((byte)4);
                    ////byte[] array = toArray((string)luaState.GetFunction("string.dump").Call((LuaFunction)entry.Value)[0]);
                    //byte[] array = toArray((string)_luaState.SafeCallRaw("string.dump", (LuaFunction)entry.Current.Value)[0].ToString());
//					var str = strDumpFunc.FirstOrDefault<string> ((LuaFunction)entry.Value);
//					str = _dataFactory.RunScript ("string.dump(cartWherigoTutorial.OnStart())");
					byte[] array = toArray(_dataFactory.GetProviderAt("string.dump").FirstOrDefault<string>((LuaFunction)entry.Value));
					// TODO: Delete
					//					string path = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
					//					string filePath = Path.Combine(path, "out.txt");
					//					BinaryWriter bw = new BinaryWriter (new FileStream(filePath,FileMode.Create));
					//					bw.Write (array);
					//					bw.Close ();
					output.Write(array.Length);
					output.Write(array);
				}
				if (entry.Value is LuaTable)  // New: LuaDataContainer)
				{
                    //string className = _luaState.SafeGetField<LuaString>((LuaTable)entry.Current.Value, "ClassName").ToString();
					LuaDataContainer dc = (LuaDataContainer)_dataFactory.GetContainer((LuaTable)entry.Value);
                    string className = dc.GetString("ClassName");

					if (className != null && (className.Equals("Distance") || className.Equals("ZonePoint") || className.Equals("ZCommand") || className.Equals("ZReciprocalCommand")))
					{
						output.Write((byte)8);
						byte[] array = Encoding.UTF8.GetBytes(className);
						output.Write(array.Length);
						output.Write(array);
                        //LuaTable data = (LuaTable)_luaState.SafeCallSelf((LuaTable)entry.Current.Value, "serialize")[0];
                        //LuaDataContainer data = dc.GetProvider("serialize", true).FirstContainerOrDefault();
                        LuaDataContainer data = dc.CallSelf("serialize");
						writeTable(output, data);
					}
					else if (className != null && (className.Equals("ZCartridge") || className.Equals("ZCharacter") || className.Equals("ZInput") || className.Equals("ZItem") ||
						className.Equals("ZMedia") || className.Equals("Zone") || className.Equals("ZTask") || className.Equals("ZTimer")))
					{
						output.Write((byte)7);
                        //output.Write(Convert.ToInt16(_luaState.SafeGetField<LuaNumber>((LuaTable)entry.Current.Value, "ObjIndex")));
                        output.Write(Convert.ToInt16(dc.GetInt("ObjIndex").Value));
					}
					else
					{
						// New: It is a normal LuaTable or an unknown new ZObject type
                        //LuaTable data = (LuaTable)entry.Current.Value;
                        LuaDataContainer data = dc;
						if (className != null) {
							// New: If we are here, than this is a new ZObject class, so call serialize before.
							// New: That means, that it is  a normal LuaTable, so save it
							//LuaFunction lf = _luaState.SafeGetField<LuaFunction>((LuaTable)entry.Current.Value, "serialize");
							LuaDataProvider lf = dc.GetProvider ("serialize", true);
							if (lf != null)
            	                //data = (LuaTable)_luaState.SafeCallSelf((LuaTable)entry.Current.Value, "serialize")[0]; //().CallSelf("serialize");
                	            data = lf.FirstContainerOrDefault ();
						}
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
