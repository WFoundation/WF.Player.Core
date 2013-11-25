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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using WF.Player.Core.Utils;
using Eluant;
using WF.Player.Core.Data;
using WF.Player.Core.Data.Lua;
using System.Linq;

namespace WF.Player.Core.Engines
{
    /// <summary>
    /// This class implements the Lua libary WIGInternal.
    /// </summary>
    #if MONOTOUCH
	    [MonoTouch.Foundation.Preserve(AllMembers=true)]
    #endif
    public class WIGInternalImpl
    {

        #region Private variables

		private Engine engine;
		//private LuaRuntime luaState;
		//private NumberFormatInfo nfi;
		//private SafeLua luaState;
		private LuaDataFactory dataFactory;

        #endregion

        #region Constants

        // Mathematical constants
        public const double LATITUDE_COEF = 110940.00000395167;
        public const double METER_COEF = 9.013881377e-6;
        public const double PI_180 = Math.PI / 180;
        public const double DEG_PI = 180 / Math.PI;
        public const double PI_2 = Math.PI / 2;

        #endregion

		#region Delegates

		private delegate void LogMessageDelegate(double level, string message);
		private delegate void MessageBoxDelegate(string text, double idxMediaObj, string btn1Label, string btn2Label, LuaFunction wrapper);
		private delegate void GetInputDelegate(LuaTable input);
		private delegate void NotifyOSDelegate(string command);
		private delegate void ShowScreenDelegate(double screen, double idxObj);
		private delegate void ShowStatusTextDelegate(string text);

		private delegate void AttributeChangedEventDelegate(LuaTable obj, string type);
		private delegate void CartridgeEventDelegate(string type);
		private delegate void CommandChangedEventDelegate(LuaTable zcmd);
		private delegate void InventoryEventDelegate(LuaTable obj, LuaTable fromContainer, LuaTable toContainer);
		private delegate void MediaEventDelegate(double type, LuaTable mo);
		private delegate void TimerEventDelegate(LuaTable timer, string type);
		private delegate void ZoneStateChangedEventDelegate(LuaTable zones);

		private delegate bool IsPointInZoneDelegate(LuaTable zonePoint, LuaTable zone);
		private delegate LuaVararg VectorToZoneDelegate(LuaTable zonePoint, LuaTable zone);
		private delegate LuaVararg VectorToSegmentDelegate(LuaTable point, LuaTable firstLinePoint, LuaTable secondLinePoint);
		private delegate LuaVararg VectorToPointDelegate(LuaTable from, LuaTable to);
		private delegate LuaTable TranslatePointDelegate(LuaTable zonePoint, LuaTable distance, double bearing);

		#endregion

		//#region Delegates

		//private delegate void Void1(object l1);
		//private delegate void VoidD1_1(double l1, object l2);
		//private delegate void Void2(object l1, object l2);
		//private delegate void Void3(object l1, object l2, object l3);
		//private delegate void Void1_D1_3(object l1, double l2, object l3, object l4, object l5);

		//private delegate bool Bool2(object l1, object l2);

		//private delegate LuaVararg Vararg2(object l1, object l2);
		//private delegate LuaVararg Vararg2i1(object l1, object l2, IConvertible l3);

		//private delegate LuaTable Table3(object l1, object l2, object l3);

		//#endregion

		#region Constructor

		internal WIGInternalImpl(Engine engine, LuaDataFactory dataFactory)
        {
			this.engine = engine;
			//this.luaState = luaState;
			this.dataFactory = dataFactory;

			//LuaTable wiginternal = luaState.SafeCreateTable();
			//luaState.SafeSetGlobal("WIGInternal", wiginternal);
			LuaDataContainer wiginternal = dataFactory.CreateContainerAt("WIGInternal");


			// Interface for GUI
			//using (var fn = luaState.CreateFunctionFromDelegate(new LogMessageDelegate(LogMessage))) {
			//    wiginternal["LogMessage"] = fn;
			//}
			//using (var fn = luaState.CreateFunctionFromDelegate(new MessageBoxDelegate(MessageBox))) {
			//    wiginternal["MessageBox"] = fn;
			//}
			//using (var fn = luaState.CreateFunctionFromDelegate(new GetInputDelegate(GetInput))) {
			//    wiginternal["GetInput"] = fn;
			//}
			//using (var fn = luaState.CreateFunctionFromDelegate(new NotifyOSDelegate(NotifyOS))) {
			//    wiginternal["NotifyOS"] = fn;
			//}
			//using (var fn = luaState.CreateFunctionFromDelegate(new ShowScreenDelegate(ShowScreen))) {
			//    wiginternal["ShowScreen"] = fn;
			//}
			//using (var fn = luaState.CreateFunctionFromDelegate(new ShowStatusTextDelegate(ShowStatusText))) {
			//    wiginternal["ShowStatusText"] = fn;
			//}
			//AddMethodToTable<Void2>(wiginternal, "LogMessage");
			//AddMethodToTable<Void5>(wiginternal, "MessageBox");
			//AddMethodToTable<Void1>(wiginternal, "GetInput");
			//AddMethodToTable<Void1>(wiginternal, "NotifyOS");
			//AddMethodToTable<Void2>(wiginternal, "ShowScreen");
			//AddMethodToTable<Void1>(wiginternal, "ShowStatusText");
			AddMethodToTable(wiginternal, "LogMessage");
			AddMethodToTable(wiginternal, "MessageBox");
			AddMethodToTable(wiginternal, "GetInput");
			AddMethodToTable(wiginternal, "NotifyOS");
			AddMethodToTable(wiginternal, "ShowScreen");
			AddMethodToTable(wiginternal, "ShowStatusText");

			// Events
			//using (var fn = luaState.CreateFunctionFromDelegate(new AttributeChangedEventDelegate(AttributeChangedEvent))) {
			//    wiginternal["AttributeChangedEvent"] = fn;
			//}
			//using (var fn = luaState.CreateFunctionFromDelegate(new CartridgeEventDelegate(CartridgeEvent))) {
			//    wiginternal["CartridgeEvent"] = fn;
			//}
			//using (var fn = luaState.CreateFunctionFromDelegate(new CommandChangedEventDelegate(CommandChangedEvent))) {
			//    wiginternal["CommandChangedEvent"] = fn;
			//}
			//using (var fn = luaState.CreateFunctionFromDelegate(new InventoryEventDelegate(InventoryEvent))) {
			//    wiginternal["InventoryEvent"] = fn;
			//}
			//using (var fn = luaState.CreateFunctionFromDelegate(new MediaEventDelegate(MediaEvent))) {
			//    wiginternal["MediaEvent"] = fn;
			//}
			//using (var fn = luaState.CreateFunctionFromDelegate(new TimerEventDelegate(TimerEvent))) {
			//    wiginternal["TimerEvent"] = fn;
			//}
			//using (var fn = luaState.CreateFunctionFromDelegate(new ZoneStateChangedEventDelegate(ZoneStateChangedEvent))) {
			//    wiginternal["ZoneStateChangedEvent"] = fn;
			//}
			//AddMethodToTable<Void2>(wiginternal, "AttributeChangedEvent");
			//AddMethodToTable<Void1>(wiginternal, "CartridgeEvent");
			//AddMethodToTable<Void1>(wiginternal, "CommandChangedEvent");
			//AddMethodToTable<Void3>(wiginternal, "InventoryEvent");
			//AddMethodToTable<Void2>(wiginternal, "MediaEvent");
			//AddMethodToTable<Void2>(wiginternal, "TimerEvent");
			//AddMethodToTable<Void1>(wiginternal, "ZoneStateChangedEvent");
			AddMethodToTable(wiginternal, "AttributeChangedEvent");
			AddMethodToTable(wiginternal, "CartridgeEvent");
			AddMethodToTable(wiginternal, "CommandChangedEvent");
			AddMethodToTable(wiginternal, "InventoryEvent");
			AddMethodToTable(wiginternal, "MediaEvent");
			AddMethodToTable(wiginternal, "TimerEvent");
			AddMethodToTable(wiginternal, "ZoneStateChangedEvent");

			// Internal functions
			//using (var fn = luaState.CreateFunctionFromDelegate(new IsPointInZoneDelegate(IsPointInZone))) {
			//    wiginternal["IsPointInZone"] = fn;
			//}
			//using (var fn = luaState.CreateFunctionFromDelegate(new VectorToZoneDelegate(VectorToZone))) {
			//    wiginternal["VectorToZone"] = fn;
			//}
			//using (var fn = luaState.CreateFunctionFromDelegate(new VectorToSegmentDelegate(VectorToSegment))) {
			//    wiginternal["VectorToSegment"] = fn;
			//}
			//using (var fn = luaState.CreateFunctionFromDelegate(new VectorToPointDelegate(VectorToPoint))) {
			//    wiginternal["VectorToPoint"] = fn;
			//}
			//using (var fn = luaState.CreateFunctionFromDelegate(new TranslatePointDelegate(TranslatePoint))) {
			//    wiginternal["TranslatePoint"] = fn;
			//}
			AddMethodToTable(wiginternal, "IsPointInZone", "IsPointInZoneLua");
			AddMethodToTable(wiginternal, "VectorToZone", "VectorToZoneLua");
			AddMethodToTable(wiginternal, "VectorToSegment", "VectorToSegmentLua");
			AddMethodToTable(wiginternal, "VectorToPoint", "VectorToPointLua");
			AddMethodToTable(wiginternal, "TranslatePoint", "TranslatePointLua");

            // Mark package WIGInternal as loaded
			//luaState.SafeSetGlobal("package.loaded.WIGInternal", wiginternal);
			//luaState.SafeSetGlobal("package.preload.WIGInternal", wiginternal);
			dataFactory.SetContainerAt("package.loaded.WIGInternal", wiginternal);
			dataFactory.SetContainerAt("package.preload.WIGInternal", wiginternal);

            // Loads the Wherigo LUA engine.
			//using (BinaryReader bw = new BinaryReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("WF.Player.Core.Resources.Wherigo.luac")))
			//{
			//    byte[] binChunk = bw.ReadBytes ((int)bw.BaseStream.Length);
			//    //luaState.SafeDoString(binChunk, "Wherigo LUA Engine");
			//    dataFactory.RunScript(binChunk);
			//}
			dataFactory.LoadAndRunEngine();
		}

		/// <summary>
		/// Adds a public method from this class to a table.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="fieldName"></param>
		/// <param name="funcName"></param>
		//private void AddMethodToTable(LuaTable table, string fieldName, string funcName = null)
		//private void AddMethodToTable<D>(LuaDataContainer container, string fieldName, string funcName = null)
		//{
		//	// Gets the delegate for the method to create.
		//	Delegate d = Delegate.CreateDelegate(typeof(D), this, funcName ?? fieldName);
			
		//	// Creates the function and sets it to the table.
		//	//luaState.SafeSetField(table, fieldName, luaState.SafeCreateFunction(d));
		//	container.BindWithFunction(fieldName, d);
		//}

		private void AddMethodToTable(LuaDataContainer container, string fieldName, string funcName = null)
		{
			// Gets the type of the delegate to create.
			Type dType = this.GetType().GetNestedType(fieldName + "Delegate", BindingFlags.NonPublic);
			
			// Gets the delegate for the method to create.
			Delegate d = Delegate.CreateDelegate(dType, this, funcName ?? fieldName);

			// Creates the function and sets it to the table.
			//luaState.SafeSetField(table, fieldName, luaState.SafeCreateFunction(d));
			container.BindWithFunction(fieldName, d);
		}

        #endregion

        #region WIGInternals Functions for UI

        /// <summary>
        /// Save message with level and text to the log file.
        /// </summary>
        /// <param name="param1">Level for message. For possible values see Engine.cs in region Constants.</param>
        /// <param name="param2">Text for the message, that should be saved to the file.</param>
        /// <returns></returns>
		//public void LogMessage(object param1, object param2)
		//public void LogMessage(LuaNumber param1, LuaString param2)
		public void LogMessage(double param1, string param2)
        {
			//int level = param1 == null ? 0 : (int)dataFactory.GetValueFromNativeValue((LuaValue)param1); //Convert.ToInt32 ((LuaNumber)param1.ToNumber().Value);
			//string message = param2 == null ? "" : (string)dataFactory.GetValueFromNativeValue((LuaValue)param2);

            //int level = param1 == null ? 0 : (int)param1;
            //string message = param2 == null ? "" : (string)param2;

            int level = Convert.ToInt32(param1);
            string message = param2 ?? "";

            engine.HandleLogMessage(level, message);
        }

        /// <summary>
        /// Show a message on the screen with given text, media and buttons. If checked by user, run callback function.
        /// </summary>
        /// <param name="param1">Text to show in the message.</param>
        /// <param name="param2">ObjIndex of media object to show or -1 if no media should be displayed.</param>
        /// <param name="param3">Caption of button1.</param>
        /// <param name="param4">Caption of button2.</param>
        /// <param name="param5">Callback LuaFunction, which is called, if the user selected a button (returns text) or canceled (returns nil) the message.</param>
		//public void MessageBox(object param1, object param2, object param3, object param4, object param5)
		//public void MessageBox(LuaString param1, LuaNumber param2, LuaString param3, LuaString param4, LuaFunction param5)
		public void MessageBox(string param1, double param2, string param3, string param4, LuaFunction param5)
        {
			//string text = param1 == null ? "" : (string)dataFactory.GetValueFromNativeValue((LuaValue)param1);
			//int idxMediaObj = param2 == null ? -1 : (int)dataFactory.GetValueFromNativeValue((LuaValue)param2); //Convert.ToInt32 ((LuaNumber)param2.ToNumber().Value);
			//string btn1Label = param3 == null ? "" : (string)dataFactory.GetValueFromNativeValue((LuaValue)param3);
			//string btn2Label = param4 == null ? "" : (string)dataFactory.GetValueFromNativeValue((LuaValue)param4);
			//IDataProvider provider = (IDataProvider)dataFactory.GetValueFromNativeValue((LuaValue)param5, true);

			string text = param1 == null ? "" : (string)param1;
            //int idxMediaObj = param2 == null ? -1 : (int)param2;
            int idxMediaObj = Convert.ToInt32(param2);
			string btn1Label = param3 == null ? "" : (string)param3;
			string btn2Label = param4 == null ? "" : (string)param4;
			IDataProvider provider = dataFactory.GetProvider((LuaFunction)param5, protectFromGC: true);

			engine.HandleShowMessage(
				text, 
				dataFactory.GetWherigoObject<Media>(idxMediaObj), 
				btn1Label, 
				btn2Label,
                provider
			);
        }

        /// <summary>
        /// Show a dialog to input text or a multiple choice.
        /// </summary>
        /// <param name="param1">LuaTable for ZInput to use for this input.</param>
		public void GetInput(LuaTable param1)
        {
			if (param1 != null)
            	//engine.HandleGetInput((Input)engine.GetTable((LuaTable)param1));
				engine.HandleGetInput(dataFactory.GetWherigoObject<Input>((LuaTable) param1));
        }

        /// <summary>
        /// Show a media on the screen.
        /// </summary>
        /// <param name="param1">Type of media, that should be shown.</param>
        /// <param name="param2">LuaTable for media object.</param>
		//public void MediaEvent(LuaNumber param1, LuaTable param2)
		public void MediaEvent(double param1, LuaTable param2)
        {
			//int type = param1 == null ? 0 : (int)dataFactory.GetValueFromNativeValue((LuaValue)param1); //Convert.ToInt32 ((LuaNumber)param1.ToNumber().Value);
			////double oi = luaState.SafeGetField<LuaValue>((LuaTable)param2, "ObjIndex").ToNumber().Value;

			//Media mediaObj = param2 == null ? null : dataFactory.GetWherigoObject<Media>((LuaTable)param2); //dataFactory.GetMedia(dataFactory.GetContainer((LuaTable)param2).GetInt("ObjIndex").Value);

            //int type = param1 == null ? 0 : (int)param1;
            int type = Convert.ToInt32(param1);
			//double oi = luaState.SafeGetField<LuaValue>((LuaTable)param2, "ObjIndex").ToNumber().Value;

			Media mediaObj = param2 == null ? null : dataFactory.GetWherigoObject<Media>((LuaTable)param2);

            engine.HandlePlayMedia(type, mediaObj);
        }

        /// <summary>
        /// Show a text in the status line.
        /// </summary>
        /// <param name="param1">Text to show in the status line.</param>
		//public void ShowStatusText(LuaString param1)
		public void ShowStatusText(string param1)
        {
			//string text = param1 == null ? "" : (string)dataFactory.GetValueFromNativeValue((LuaValue)param1);
			string text = param1 == null ? "" : (string)param1;

            engine.HandleShowStatusText(text);
        }

        /// <summary>
        /// Show a special screen.
        /// </summary>
        /// <param name="param1">Screen to show. For possible values see Engine.cs in region Constants</param>
        /// <param name="param2">LuaTable for object, which should used for detail screen.</param>
		//public void ShowScreen(LuaNumber param1, LuaNumber param2)
		public void ShowScreen(double param1, double param2)
        {
			//int screen = param1 == null ? 0 : (int)dataFactory.GetValueFromNativeValue((LuaValue)param1); //Convert.ToInt32 ((LuaNumber)param1.ToNumber());
			//int idxObj = param2 == null ? -1 : (int)dataFactory.GetValueFromNativeValue((LuaValue)param2); //Convert.ToInt32 ((LuaNumber)param2.ToNumber());

            //int screen = param1 == null ? 0 : (int)param1;
            //int idxObj = param2 == null ? -1 : (int)param2;

            int screen = Convert.ToInt32(param1);
            int idxObj = Convert.ToInt32(param2);

            engine.HandleShowScreen(screen, idxObj);
        }

        /// <summary>
        /// Notify the system to run a special command.
        /// </summary>
        /// <param name="param1">Text for the command to call.</param>
		//public void NotifyOS(LuaString param1)
		public void NotifyOS(string param1)
        {
			//string command = param1 == null ? "" : (string)dataFactory.GetValueFromNativeValue((LuaValue)param1); //param1.ToString();

			string command = param1 == null ? "" : (string)param1;

            engine.HandleNotifyOS(command);
        }

		#endregion

		#region WIGInternals Functions for events

		/// <summary>
		/// Event, which is called, if the attribute of a object has changed.
		/// </summary>
		/// <param name="param1">LuaTable with object and a string, describing which attribute has changed.</param>
		/// <param name="param2">Name of the attribute that changed.</param>
		/// <returns>An empty object array.</returns>
		//public void AttributeChangedEvent(LuaTable param1, LuaString param2)
		public void AttributeChangedEvent(LuaTable param1, string param2)
		{
			LuaTable obj = (LuaTable)param1;
			//string type = param2 == null ? "" : ((LuaString)param2).ToString();

			//string type = (string)dataFactory.GetValueFromNativeValue((LuaValue)param2);
			string type = (string)param2;

			// Possible types
			// "Name"
			// "Description"
			// "Visible"
			// "Media"
			// "Icon"
			// "Active"
			// "Gender"
			// "Type"
			// "Complete"
			// "CorrectState"

			//if (OnAttributeChanged != null)
			//    OnAttributeChanged(obj,type);
			engine.HandleAttributeChanged(dataFactory.GetWherigoObject(obj), type);
			
		}

		/// <summary>
		/// Event, which is called, if attribute of a cartridge has changed.
		/// </summary>
		/// <param name="param1">String, describing which attribute has changed.</param>
		/// <returns>An empty object array.</returns>
		//public void CartridgeEvent(LuaString param1)
		public void CartridgeEvent(string param1)
		{
            //string type = param1 == null ? "" : ((LuaString)param1).ToString();
			//string type = (string)dataFactory.GetValueFromNativeValue((LuaString)param1);
			string type = (string)param1;

			// Possible types
			// "complete"
			// "sync"

			//if (OnCartridgeChanged != null)
			//    OnCartridgeChanged(type);
			engine.HandleCartridgeChanged(type);
		}

		/// <summary>
		/// Event, which is called, if a command has changed.
		/// </summary>
		/// <returns>An empty object array.</returns>
		/// <param name="param1">LuaTable with the command that had be changed.</param>
		public void CommandChangedEvent(LuaTable param1)
		{
			LuaTable zcmd = (LuaTable)param1;

            //engine.HandleCommandChanged(zcmd);
            engine.HandleCommandChanged(dataFactory.GetWherigoObject<Command>(zcmd));
		}

		/// <summary>
		/// Event, which is called, if the inventory of the player has changed.
		/// </summary>
		/// <returns>An empty object array.</returns>
		public void InventoryEvent(LuaTable param1, LuaTable param2, LuaTable param3)
        {
			LuaTable obj = (LuaTable)param1;
			LuaTable fromContainer = (LuaTable)param2;
			LuaTable toContainer = (LuaTable)param3;

            //engine.HandleInventoryChanged((LuaTable)obj, (LuaTable)fromContainer, (LuaTable)toContainer);
            engine.HandleInventoryChanged(
                dataFactory.GetWherigoObject<Thing>(obj), 
                dataFactory.GetWherigoObject<Thing>(fromContainer),
                dataFactory.GetWherigoObject<Thing>(toContainer)
            );
        }

		/// <summary>
		/// Event, which is called, if the state of a timer has changed.
		/// </summary>
		/// <returns>An empty object array.</returns>
		//public void TimerEvent(LuaTable param1, LuaString param2)
		public void TimerEvent(LuaTable param1, string param2)
        {
			LuaTable timer = (LuaTable)param1;
            //string type = param2 == null ? "" : ((LuaString)param2).ToString().ToLower();
			//string type = param2 == null ? "" : (string)dataFactory.GetValueFromNativeValue((LuaString)param2);
			string type = param2 == null ? "" : (string)param2;

            Timer timerObj = dataFactory.GetWherigoObject<Timer>(timer);

			if ("start".Equals(type))
                //engine.HandleTimerStarted(timer);
                engine.HandleTimerStarted(timerObj);

			if ("stop".Equals(type))
                //engine.HandleTimerStopped(timer);
                engine.HandleTimerStopped(timerObj);
        }

		/// <summary>
		/// Event, which is called, if the state of one or more zones had changed.
		/// </summary>
		/// <returns>An empty object array.</returns>
		/// <param name="param1">LuaTable with zones, that changed their state.</param>
		public void ZoneStateChangedEvent(LuaTable param1)
		{
            //LuaTable zones = param1 == null ? luaState.SafeCreateTable() : (LuaTable)param1;
            IDataContainer zones = param1 == null ? dataFactory.CreateContainer() : dataFactory.GetContainer((LuaTable)param1);

			engine.HandleZoneStateChanged(dataFactory.GetWherigoObjectList<Zone>(zones));
		}

		#endregion

        #region WIGInternal Functions for location calculation

        /// <summary>
        /// Checks, if the given ZonePoint is in Zone.
        /// </summary>
		/// <remarks>
		/// Found at http://alienryderflex.com/polygon/
		/// </remarks>
        /// <param name="param1">LuaTable for ZonePoint to check.</param>
        /// <param name="param2">LuaTable with Zone, which Points are used for the check.</param>
        /// <returns></returns>
		public bool IsPointInZoneLua(LuaTable param1, LuaTable param2)
        {
            LuaTable zonePoint;
            LuaTable zone;

            if (param1 == null || param2 == null)
                return false;

            if (!(param1 is LuaTable))
				throw new ArgumentException(String.Format("bad argument #1 to 'IsPointInZoneLua' (ZonePoint expected, got {0})", param1.GetType()));

            zonePoint = (LuaTable)param1;

            if (!(param2 is LuaTable))
				throw new ArgumentException(String.Format("bad argument #2 to 'IsPointInZoneLua' (Zone expected, got {0})", param2.GetType()));

            zone = (LuaTable)param2;

			//double lat, lon;
			//LuaTable points;
			//int count;

			//lock (luaState)
			//{
			//    lat = (double)zonePoint["latitude"].ToNumber();
			//    lon = (double)zonePoint["longitude"].ToNumber();
			//    points = (LuaTable)zone["Points"];
			//    count = points.Keys.Count;
			//}

			//double[] lats = new double[count];
			//double[] lons = new double[count];

			//int i;

			//for (i = 0; i < count; i++)
			//{
			//    lock (luaState)
			//    {
			//        lats[i] = (double)((LuaTable)points[i + 1])["latitude"].ToNumber();
			//        lons[i] = (double)((LuaTable)points[i + 1])["longitude"].ToNumber(); 
			//    }
			//}

			//int j = count - 1;
			//bool oddNodes = false;

			//for (i = 0; i < count; i++)
			//{
			//    if ((lats[i] < lat && lats[j] >= lat || lats[j] < lat && lats[i] >= lat) && (lons[i] <= lon || lons[j] <= lon))
			//    {
			//        oddNodes ^= (lons[i] + (lat - lats[i]) / (lats[j] - lats[i]) * (lons[j] - lons[i]) < lon);
			//    }
			//    j = i;
			//}

			//return oddNodes;

			return IsPointInZone(
				dataFactory.GetWherigoObject<ZonePoint>(zonePoint),
				dataFactory.GetWherigoObject<Zone>(zone));
        }

		public bool IsPointInZone(ZonePoint point, Zone target)
		{
			double lat = point.Latitude;
			double lon = point.Longitude;
			
			ZonePoint[] points = target.Points.ToArray();
			int count = points.Length;

			double[] lats = new double[count];
			double[] lons = new double[count];

			int i;
			ZonePoint cur;

			for (i = 0; i < count; i++)
			{
				cur = points[i];
				lats[i] = cur.Latitude;
				lons[i] = cur.Longitude;
			}

			int j = count - 1;
			bool oddNodes = false;

			for (i = 0; i < count; i++)
			{
				if ((lats[i] < lat && lats[j] >= lat || lats[j] < lat && lats[i] >= lat) && (lons[i] <= lon || lons[j] <= lon))
				{
					oddNodes ^= (lons[i] + (lat - lats[i]) / (lats[j] - lats[i]) * (lons[j] - lons[i]) < lon);
				}
				j = i;
			}

			return oddNodes;
		}

        /// <summary>
        /// Calculates the distance and bearing between a ZonePoint to the nearest point of a Zone.
        /// </summary>
        /// <param name="pointObj">LuaTable for ZonePoint.</param>
        /// <param name="zoneObj">LuaTable with Zone, which Points are used for the check.</param>
        /// <returns>LuaTable for distance to calculated nearest point of zone.</returns>
		public LuaVararg VectorToZoneLua(LuaTable pointObj, LuaTable zoneObj)
        {
			List<LuaValue> ret = new List<LuaValue>(2);

            if (!(pointObj is LuaTable))
                throw new ArgumentException(String.Format("bad argument #1 to 'VectorToZone' (ZonePoint expected, got {0})", pointObj.GetType()));

			LuaTable zonePoint = (LuaTable)pointObj;

            if (!(zoneObj is LuaTable))
                throw new ArgumentException(String.Format("bad argument #2 to 'VectorToZone' (Zone expected, got {0})", zoneObj.GetType()));

            LuaTable zone = (LuaTable)zoneObj;

			// Performs the computation.
			LocationVector lv = VectorToZone(
				dataFactory.GetWherigoObject<ZonePoint>(zonePoint),
				dataFactory.GetWherigoObject<Zone>(zone));

			// Prepares the lua return.
			ret.Add(dataFactory.GetNativeContainer(lv.Distance));
			ret.Add(lv.Bearing.GetValueOrDefault());

			return new LuaVararg(ret,true);
        }

		public LocationVector VectorToZone(ZonePoint point, Zone target)
		{
			// If the point is in the zone, the distance and bearing are 0.
			if (IsPointInZone(point, target))
			{
				return new LocationVector(dataFactory.CreateWherigoObject<Distance>(), 0d);
			}

			// If the zone doesn't have points, the distance and bearing are null.
			IEnumerable<ZonePoint> points = target.Points;
			//lock (luaState)
			//{
			//    points = zone["Points"] as LuaTable;
			//}
			if (points == null)
			{
				//ret.Add(null);
				//ret.Add(0);

				//return new LuaVararg(ret, true);
				return new LocationVector(null, null);
			}

			// Performs the computation.

			//double k;
			//LuaVararg td, current;
			LocationVector td, current;

			//lock (luaState)
			//{
			//    current = VectorToSegment(pointObj, points[points.Keys.Count], points[1]);
			//    var pairs = points.GetEnumerator();

			//    while (pairs.MoveNext())
			//    {
			//        k = (double)pairs.Current.Key.ToNumber();
			//        if (k > 1)
			//        {
			//            td = VectorToSegment(pointObj, points[k - 1], points[k]);
			//            if ((double)((LuaTable)td[0])["value"].ToNumber() < (double)((LuaTable)current[0])["value"].ToNumber())
			//            {
			//                current = td;
			//            }
			//        }
			//    }
			//}
			current = VectorToSegment(point, points.Last(), points.First());

			IEnumerable<ZonePoint> zpe = points;
			ZonePoint previous = zpe.First();
			zpe = zpe.Skip(1);

			while(zpe.Count() > 0)
			{
				td = VectorToSegment(point, previous, zpe.First());
				if (td.Distance < current.Distance)
				{
					current = td;
				}

				zpe = zpe.Skip(1);
			}

			//ret.Add(current[0]);
			//ret.Add((double)current[1].ToNumber() % 360);
			return new LocationVector(current.Distance, current.Bearing.GetValueOrDefault() % 360);
		}

        /// <summary>
        /// Calculate distance and bearing of ZonePoint to line between two points with shortest distance.
        /// </summary>
        /// <param name="pointObj">LuaTable for ZonePoint.</param>
        /// <param name="firstLinePointObj">LuaTable for first ZonePoint of line.</param>
        /// <param name="secondLinePointObj">LuaTable for second ZonePoint of line.</param>
        /// <returns>LuaTable for distance to calculated point on line.</returns>
		public LuaVararg VectorToSegmentLua(LuaTable pointObj, LuaTable firstLinePointObj, LuaTable secondLinePointObj)
        {
			List<LuaValue> ret = new List<LuaValue>(2);
			
			LuaTable zonePoint = pointObj as LuaTable;
			LuaTable firstLineZonePoint = firstLinePointObj as LuaTable;
			LuaTable secondLineZonePoint = secondLinePointObj as LuaTable;

			if (pointObj == null || firstLinePointObj == null || secondLinePointObj == null) 
			{	
				ret.Add (LuaNil.Instance);
				ret.Add (LuaNil.Instance);

				return new LuaVararg(ret, true);
			}

			//LuaVararg d1 = VectorToPoint(firstLinePointObj, pointObj);
			//double b1 = (double)d1[1].ToNumber();
			//double dd1 = PI_180 * engine.DataFactory.GetWherigoObject<Distance>((LuaTable)d1[0]).ValueAs(DistanceUnit.NauticalMiles) / 60;

			//LuaVararg ds = VectorToPoint(firstLinePointObj, secondLinePointObj);
			//double bs = (double)ds[1].ToNumber();
			//double dds = PI_180 * engine.DataFactory.GetWherigoObject<Distance>((LuaTable)ds[0]).ValueAs(DistanceUnit.NauticalMiles) / 60;

			//var dist = Math.Asin(Math.Sin(dd1) * Math.Sin(PI_180 * (b1 - bs)));
			//var dat = Math.Acos(Math.Cos(dd1) / Math.Cos(dist));
			//if (dat <= 0)
			//{
			//    return VectorToPoint(pointObj, firstLinePointObj);
			//}
			//else if (dat >= PI_180 * dds)
			//{
			//    return VectorToPoint(pointObj, secondLinePointObj);
			//}

			//LuaTable intersect;

			//lock (luaState)
			//{
			//    intersect = TranslatePoint(firstLinePointObj, luaState.DoString(String.Format("return Wherigo.Distance({0}, 'nauticalmiles')", (dat * 60).ToString(nfi)))[0], bs); 
			//}

			//return VectorToPoint(pointObj, intersect);

			LocationVector lv = VectorToSegment(
				dataFactory.GetWherigoObject<ZonePoint>(zonePoint),
				dataFactory.GetWherigoObject<ZonePoint>(firstLineZonePoint),
				dataFactory.GetWherigoObject<ZonePoint>(secondLineZonePoint)
			);

			ret.Add(dataFactory.GetNativeContainer(lv.Distance));
			ret.Add(lv.Bearing.GetValueOrDefault());

			return new LuaVararg(ret, true);
		}

		public LocationVector VectorToSegment(ZonePoint point, ZonePoint firstLinePoint, ZonePoint secondLinePoint)
		{
			//LuaVararg d1 = VectorToPoint(firstLinePointObj, pointObj);
			//double b1 = (double)d1[1].ToNumber();
			//double dd1 = PI_180 * engine.DataFactory.GetWherigoObject<Distance>((LuaTable)d1[0]).ValueAs(DistanceUnit.NauticalMiles) / 60;
			LocationVector d1 = VectorToPoint(firstLinePoint, point);
			double b1 = d1.Bearing.GetValueOrDefault();
			double dd1 = PI_180 * d1.Distance.ValueAs(DistanceUnit.NauticalMiles) / 60;

			//LuaVararg ds = VectorToPoint(firstLinePointObj, secondLinePointObj);
			//double bs = (double)ds[1].ToNumber();
			//double dds = PI_180 * engine.DataFactory.GetWherigoObject<Distance>((LuaTable)ds[0]).ValueAs(DistanceUnit.NauticalMiles) / 60;
			LocationVector ds = VectorToPoint(firstLinePoint, secondLinePoint);
			double bs = ds.Bearing.GetValueOrDefault();
			double dds = PI_180 * ds.Distance.ValueAs(DistanceUnit.NauticalMiles) / 60;

			var dist = Math.Asin(Math.Sin(dd1) * Math.Sin(PI_180 * (b1 - bs)));
			var dat = Math.Acos(Math.Cos(dd1) / Math.Cos(dist));
			if (dat <= 0)
			{
				return VectorToPoint(point, firstLinePoint);
			}
			else if (dat >= PI_180 * dds)
			{
				return VectorToPoint(point, secondLinePoint);
			}

			//LuaTable intersect;

			//lock (luaState)
			//{
			//    intersect = TranslatePoint(firstLinePointObj, luaState.DoString(String.Format("return Wherigo.Distance({0}, 'nauticalmiles')", (dat * 60).ToString(nfi)))[0], bs);
			//}

			ZonePoint intersect = TranslatePoint(
				firstLinePoint,
				new LocationVector(
					dataFactory.CreateWherigoObject<Distance>(dat * 60, DistanceUnit.NauticalMiles),
					bs
				)
			);

			//return VectorToPoint(pointObj, intersect);
			return VectorToPoint(point, intersect);
		}



        /// <summary>
        /// Calculate distance and bearing from one ZonePoint to another.
        /// </summary>
        /// <param name="param1">LuaTable for first ZonePoint.</param>
        /// <param name="param2">LuaTable for second ZonePoint</param>
        /// <returns></returns>
		public LuaVararg VectorToPointLua(LuaTable param1, LuaTable param2)
        {
			List<LuaValue> ret = new List<LuaValue>(2);

            //if (param1 is LuaNil || param2 is LuaNil) {
            //    ret.Add (LuaNil.Instance);
            //    ret.Add (LuaNil.Instance);

            //    return new LuaVararg(ret, true);
            //}

			LuaTable zonePoint1 = (LuaTable)param1;
			LuaTable zonePoint2 = (LuaTable)param2;

			//double lat1, lon1, lat2, lon2;
			//lock (luaState)
			//{
			//    lat1 = (double)zonePoint1["latitude"].ToNumber();
			//    lon1 = (double)zonePoint1["longitude"].ToNumber();
			//    lat2 = (double)zonePoint2["latitude"].ToNumber();
			//    lon2 = (double)zonePoint2["longitude"].ToNumber(); 
			//}

			//double distance, bearing;

			//double mx = Math.Abs(CoreLat2M(lat1 - lat2));
			//double my = Math.Abs(CoreLon2M(lat2, lon1 - lon2));

			//distance = Math.Sqrt(mx * mx + my * my);
			//bearing = (Math.Atan2(CoreLat2M(lat2 - lat1), CoreLon2M(lat2, lon2 - lon1)) + Math.PI / 2) * (180.0 / Math.PI);

			//lock (luaState)
			//{
			//    ret.Add((LuaTable)luaState.DoString(String.Format("return Wherigo.Distance({0},'m')", distance.ToString(nfi)))[0]); 
			//    ret.Add(bearing);

			//    return new LuaVararg(ret, true);
			//}

			LocationVector lv = VectorToPoint(
				dataFactory.GetWherigoObject<ZonePoint>(zonePoint1),
				dataFactory.GetWherigoObject<ZonePoint>(zonePoint2)
			);

			ret.Add(dataFactory.GetNativeContainer(lv.Distance));
			ret.Add(lv.Bearing.GetValueOrDefault());

			return new LuaVararg(ret, true);
        }

		public LocationVector VectorToPoint(ZonePoint location, ZonePoint target)
		{
			//LuaTable zonePoint1 = (LuaTable)param1;
			//LuaTable zonePoint2 = (LuaTable)param2;

			//double lat1, lon1, lat2, lon2;
			//lock (luaState)
			//{
			//    lat1 = (double)zonePoint1["latitude"].ToNumber();
			//    lon1 = (double)zonePoint1["longitude"].ToNumber();
			//    lat2 = (double)zonePoint2["latitude"].ToNumber();
			//    lon2 = (double)zonePoint2["longitude"].ToNumber(); 
			//}
			double lat1 = location.Latitude;
			double lon1 = location.Longitude;
			double lat2 = target.Latitude;
			double lon2 = target.Longitude;

			//double distance, bearing;

			double mx = Math.Abs(CoreLat2M(lat1 - lat2));
			double my = Math.Abs(CoreLon2M(lat2, lon1 - lon2));

			double distance = Math.Sqrt(mx * mx + my * my);
			double bearing = (Math.Atan2(CoreLat2M(lat2 - lat1), CoreLon2M(lat2, lon2 - lon1)) + Math.PI / 2) * (180.0 / Math.PI);

			//lock (luaState)
			//{
			//    ret.Add((LuaTable)luaState.DoString(String.Format("return Wherigo.Distance({0},'m')", distance.ToString(nfi)))[0]); 
			//    ret.Add(bearing);

			//    return new LuaVararg(ret, true);
			//}

			return new LocationVector(
				dataFactory.CreateWherigoObject<Distance>(distance),
				bearing
			);
		}

        /// <summary>
        /// Calculate new point with distance and bearing from old point.
        /// </summary>
        /// <param name="param1">ZonePoint to use for calculation.</param>
        /// <param name="param2">LuaTable for distance.</param>
        /// <param name="param3">Double value for bearing</param>
        /// <returns></returns>
		//public LuaTable TranslatePointLua(LuaTable param1, LuaTable param2, LuaNumber param3)
		public LuaTable TranslatePointLua(LuaTable param1, LuaTable param2, double param3)
        {
            LuaTable zonePoint = (LuaTable)param1;
            LuaTable distance = (LuaTable)param2;
			//double bearing = (double)dataFactory.GetValueFromNativeValue((LuaValue)param3);
			double bearing = (double)param3;

			//double lat, lon, alt, dist;
			//lock (luaState)
			//{
			//    lat = (double)zonePoint["latitude"].ToNumber();
			//    lon = (double)zonePoint["longitude"].ToNumber();
			//    alt = (double)((LuaTable)zonePoint["altitude"])["value"].ToNumber();
			//    dist = (double)distance["value"].ToNumber(); 
			//}

			//double rad = CoreAzimuth2Angle(bearing);
			//double x = CoreM2Lat(dist * Math.Sin(rad));
			//double y = CoreM2Lon(lat, dist * Math.Cos(rad));

			//lock (luaState)
			//{
			//    return (LuaTable)luaState.DoString(String.Format("return Wherigo.ZonePoint({0},{1},{2})", (lat + x).ToString(nfi), (lon + y).ToString(nfi), alt.ToString(nfi)))[0]; 
			//}

			ZonePoint ret = TranslatePoint(
				dataFactory.GetWherigoObject<ZonePoint>(zonePoint),
				new LocationVector(
					dataFactory.GetWherigoObject<Distance>(distance),
					bearing
				)
			);

			return dataFactory.GetNativeContainer(ret);
        }

		public ZonePoint TranslatePoint(ZonePoint point, LocationVector vector)
		{
			//double lat, lon, alt, dist;
			//lock (luaState)
			//{
			//    lat = (double)zonePoint["latitude"].ToNumber();
			//    lon = (double)zonePoint["longitude"].ToNumber();
			//    alt = (double)((LuaTable)zonePoint["altitude"])["value"].ToNumber();
			//    dist = (double)distance["value"].ToNumber(); 
			//}
			double lat = point.Latitude;
			double lon = point.Longitude;
			double alt = point.Altitude;
			double dist = vector.Distance.Value;

			double rad = CoreAzimuth2Angle(vector.Bearing.GetValueOrDefault());
			double x = CoreM2Lat(dist * Math.Sin(rad));
			double y = CoreM2Lon(lat, dist * Math.Cos(rad));

			//lock (luaState)
			//{
			//    return (LuaTable)luaState.DoString(String.Format("return Wherigo.ZonePoint({0},{1},{2})", (lat + x).ToString(nfi), (lon + y).ToString(nfi), alt.ToString(nfi)))[0]; 
			//}
			return dataFactory.CreateWherigoObject<ZonePoint>(lat + x, lon + y, alt);
		}

        #endregion

        #region C# Private Functions for location calculation

        /// <summary>
        /// Convert angle in degree for latitude into distance in meters.
        /// </summary>
        /// <param name="angle">Angle for latitude in degree.</param>
        /// <returns>Distance in meters.</returns>
        private double CoreLat2M(double degrees)
        {
            return degrees * LATITUDE_COEF;
        }

        /// <summary>
        /// Convert angle in degree for longitude into distance in meters.
        /// </summary>
        /// <param name="angle">Angle for longitude in degree.</param>
        /// <returns>Distance in meters.</returns>
        private double CoreLon2M(double latitude, double degrees)
        {
            return degrees * PI_180 * Math.Cos(latitude * PI_180) * 6367449;
        }

        /// <summary>
        /// Convert distance in meters to a latitude of a coordinate.
        /// </summary>
        /// <param name="meters">Distance in meters.</param>
        /// <returns>Degree in latitude direction.</returns>
        private double CoreM2Lat(double meters)
        {
            return meters * METER_COEF;
        }

        /// <summary>
        /// Convert distance in meters to a longitude of a coordinate.
        /// </summary>
        /// <param name="meters">Distance in meters.</param>
        /// <returns>Degree in longitude direction.</returns>
        private double CoreM2Lon(double latitude, double meters)
        {
            return meters / (PI_180 * Math.Cos(latitude * PI_180) * 6367449);
        }

        /// <summary>
        /// Convert radiant to angle.
        /// </summary>
        /// <param name="angle">Angle in radiant.</param>
        /// <returns>Angle in degree.</returns>
        private double CoreAzimuth2Angle(double azim)
        {
            double ret = -(azim * PI_180) + PI_2;

            while (ret > Math.PI) ret -= Math.PI * 2;
            while (ret <= -Math.PI) ret += Math.PI * 2;

            return ret;
        }

        #endregion

    }

}
