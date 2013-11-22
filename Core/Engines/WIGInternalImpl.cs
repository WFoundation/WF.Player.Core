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
using WF.Player.Core.Lua;

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
		private LuaRuntime luaState;
		private NumberFormatInfo nfi;

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

		private delegate void LogMessageDelegate(LuaNumber level, LuaString message);
		private delegate void MessageBoxDelegate(LuaValue text, LuaValue idxMediaObj, LuaValue btn1Label, LuaValue btn2Label, LuaValue wrapper);
		private delegate void GetInputDelegate(LuaValue input);
		private delegate void NotifyOSDelegate(LuaValue command);
		private delegate void ShowScreenDelegate(LuaValue screen, LuaValue idxObj);
		private delegate void ShowStatusTextDelegate(LuaValue text);

		private delegate void AttributeChangedEventDelegate(LuaValue obj, LuaValue type);
		private delegate void CartridgeEventDelegate(LuaValue type);
		private delegate void CommandChangedEventDelegate(LuaValue zcmd);
		private delegate void InventoryEventDelegate(LuaValue obj, LuaValue fromContainer, LuaValue toContainer);
		private delegate void MediaEventDelegate(LuaValue type, LuaValue mo);
		private delegate void TimerEventDelegate(LuaValue timer, LuaValue type);
		private delegate void ZoneStateChangedEventDelegate(LuaValue zones);

		private delegate bool IsPointInZoneDelegate(LuaValue zonePoint, LuaValue zone);
		private delegate LuaVararg VectorToZoneDelegate(LuaValue zonePoint, LuaValue zone);
		private delegate LuaVararg VectorToSegmentDelegate(LuaValue point, LuaValue firstLinePoint, LuaValue secondLinePoint);
		private delegate LuaVararg VectorToPointDelegate(LuaValue from, LuaValue to);
		private delegate LuaTable TranslatePointDelegate(LuaValue zonePoint, LuaValue distance, LuaValue bearing);

		#endregion

        #region Constructor

		/// <summary>
		/// Constructs a new instance of WIGInternalImpl.
		/// </summary>
		/// <remarks>
		/// This method is not thread-safe.
		/// </remarks>
		/// <param name="engine"></param>
		/// <param name="luaState"></param>
		internal WIGInternalImpl( Engine engine, LuaRuntime luaState)
        {
			this.engine = engine;
            this.luaState = luaState;

			// Need this, because iPhone has problems at DoString with numbers, that have NumberDecimalSeparator other than "."
			nfi = new NumberFormatInfo();
			nfi.NumberDecimalSeparator = ".";
			nfi.NumberGroupSeparator = "";

			luaState.Globals["WIGInternal"] = luaState.CreateTable();
			LuaTable wiginternal = (LuaTable)luaState.Globals["WIGInternal"];

			// Interface for GUI
			using (var fn = luaState.CreateFunctionFromDelegate(new LogMessageDelegate(LogMessage))) {
				wiginternal["LogMessage"] = fn;
			}
			using (var fn = luaState.CreateFunctionFromDelegate(new MessageBoxDelegate(MessageBox))) {
				wiginternal["MessageBox"] = fn;
			}
			using (var fn = luaState.CreateFunctionFromDelegate(new GetInputDelegate(GetInput))) {
				wiginternal["GetInput"] = fn;
			}
			using (var fn = luaState.CreateFunctionFromDelegate(new NotifyOSDelegate(NotifyOS))) {
				wiginternal["NotifyOS"] = fn;
			}
			using (var fn = luaState.CreateFunctionFromDelegate(new ShowScreenDelegate(ShowScreen))) {
				wiginternal["ShowScreen"] = fn;
			}
			using (var fn = luaState.CreateFunctionFromDelegate(new ShowStatusTextDelegate(ShowStatusText))) {
				wiginternal["ShowStatusText"] = fn;
			}

			// Events
			using (var fn = luaState.CreateFunctionFromDelegate(new AttributeChangedEventDelegate(AttributeChangedEvent))) {
				wiginternal["AttributeChangedEvent"] = fn;
			}
			using (var fn = luaState.CreateFunctionFromDelegate(new CartridgeEventDelegate(CartridgeEvent))) {
				wiginternal["CartridgeEvent"] = fn;
			}
			using (var fn = luaState.CreateFunctionFromDelegate(new CommandChangedEventDelegate(CommandChangedEvent))) {
				wiginternal["CommandChangedEvent"] = fn;
			}
			using (var fn = luaState.CreateFunctionFromDelegate(new InventoryEventDelegate(InventoryEvent))) {
				wiginternal["InventoryEvent"] = fn;
			}
			using (var fn = luaState.CreateFunctionFromDelegate(new MediaEventDelegate(MediaEvent))) {
				wiginternal["MediaEvent"] = fn;
			}
			using (var fn = luaState.CreateFunctionFromDelegate(new TimerEventDelegate(TimerEvent))) {
				wiginternal["TimerEvent"] = fn;
			}
			using (var fn = luaState.CreateFunctionFromDelegate(new ZoneStateChangedEventDelegate(ZoneStateChangedEvent))) {
				wiginternal["ZoneStateChangedEvent"] = fn;
			}

			// Internal functions
			using (var fn = luaState.CreateFunctionFromDelegate(new IsPointInZoneDelegate(IsPointInZone))) {
				wiginternal["IsPointInZone"] = fn;
			}
			using (var fn = luaState.CreateFunctionFromDelegate(new VectorToZoneDelegate(VectorToZone))) {
				wiginternal["VectorToZone"] = fn;
			}
			using (var fn = luaState.CreateFunctionFromDelegate(new VectorToSegmentDelegate(VectorToSegment))) {
				wiginternal["VectorToSegment"] = fn;
			}
			using (var fn = luaState.CreateFunctionFromDelegate(new VectorToPointDelegate(VectorToPoint))) {
				wiginternal["VectorToPoint"] = fn;
			}
			using (var fn = luaState.CreateFunctionFromDelegate(new TranslatePointDelegate(TranslatePoint))) {
				wiginternal["TranslatePoint"] = fn;
			}

            // Mark package WIGInternal as loaded
			LuaTable package = (LuaTable)luaState.Globals["package"];
			LuaTable loaded = (LuaTable)package["loaded"];
            loaded["WIGInternal"] = wiginternal;
			((LuaTable)package["preload"])["WIGInternal"] = wiginternal;

            // Now load Wherigo.luac
			using (BinaryReader bw = new BinaryReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("WF.Player.Core.Resources.Wherigo.luac")))
			{
				byte[] binChunk = bw.ReadBytes ((int)bw.BaseStream.Length);
				luaState.DoString (binChunk, "Wherigo.lua");
			}
		}

        #endregion

        #region Events stub for handlers

        public delegate void ZoneStateChangedHandler(LuaTable zones);
        public event ZoneStateChangedHandler OnZoneStateChanged;

        public delegate void InventoryChangedHandler(LuaTable obj, LuaTable fromContainer, LuaTable toContainer);
        public event InventoryChangedHandler OnInventoryChanged;

        public delegate void TimerStartedHandler(LuaTable timer);
        public event TimerStartedHandler OnTimerStarted;

        public delegate void TimerStoppedHandler(LuaTable timer);
        public event TimerStoppedHandler OnTimerStopped;

        // Possible types are "complete"/"sync"
        public delegate void CartridgeChangedHandler(string type); 
        public event CartridgeChangedHandler OnCartridgeChanged;

        public delegate void CommandChangedHandler(LuaTable zcmd);
        public event CommandChangedHandler OnCommandChanged;

        // Possible types are "Name"/"Description"/"Visible"/"Media"/"Icon"/"Active"/"Gender"/"Type"/"Complete"/"CorrectState"
        public delegate void AttributeChangedHandler(LuaTable obj, string type); 
        public event AttributeChangedHandler OnAttributeChanged;

        #endregion

        #region WIGInternals Functions for UI

        /// <summary>
        /// Save message with level and text to the log file.
        /// </summary>
        /// <param name="param1">Level for message. For possible values see Engine.cs in region Constants.</param>
        /// <param name="param2">Text for the message, that should be saved to the file.</param>
        /// <returns></returns>
		public void LogMessage(object param1, object param2)
        {
			int level = param1 == null ? 0 : Convert.ToInt32 ((double)param1);
			string message = param2 == null ? "" : (string)param2;

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
		public void MessageBox(object param1, object param2, object param3, object param4, object param5)
        {
			string text = param1 == null ? "" : (string)param1;
			int idxMediaObj = param2 == null ? -1 : Convert.ToInt32 ((double)param2);
			string btn1Label = param3 == null ? "" : (string)param3;
			string btn2Label = param4 == null ? "" : (string)param4;
			LuaFunction wrapper = (LuaFunction)((LuaValue)param5).CopyReference();

			engine.HandleShowMessage(
				text, 
				engine.GetMedia(idxMediaObj), 
				btn1Label, 
				btn2Label,
 
				// The callback will run in the engine's execution queue.
				(retValue) => engine.LuaExecQueue.BeginCall(wrapper, retValue)
			);
        }

        /// <summary>
        /// Show a dialog to input text or a multiple choice.
        /// </summary>
        /// <param name="param1">LuaTable for ZInput to use for this input.</param>
		public void GetInput(object param1)
        {
			if (param1 != null)
            	engine.HandleGetInput((Input)engine.GetTable((LuaTable)param1));
        }

        /// <summary>
        /// Show a media on the screen.
        /// </summary>
        /// <param name="param1">Type of media, that should be shown.</param>
        /// <param name="param2">LuaTable for media object.</param>
		public void MediaEvent(object param1, object param2)
        {
			int type = param1 == null ? 0 : Convert.ToInt32 ((double)param1);
			double oi;
			lock (luaState)
			{
				oi = (double)((LuaTable)param2)["ObjIndex"].ToNumber();
			}
			Media mediaObj = param2 == null ? null : engine.Cartridge.Resources[Convert.ToInt32 (oi)];

            engine.HandlePlayMedia(type, mediaObj);
        }

        /// <summary>
        /// Show a text in the status line.
        /// </summary>
        /// <param name="param1">Text to show in the status line.</param>
		public void ShowStatusText(object param1)
        {
			string text = param1 == null ? "" : (string)param1;

            engine.HandleShowStatusText(text);
        }

        /// <summary>
        /// Show a special screen.
        /// </summary>
        /// <param name="param1">Screen to show. For possible values see Engine.cs in region Constants</param>
        /// <param name="param2">LuaTable for object, which should used for detail screen.</param>
		public void ShowScreen(object param1, object param2)
        {
            int screen = param1 == null ? 0 : Convert.ToInt32 ((double)param1);
			int idxObj = param2 == null ? -1 : Convert.ToInt32 ((double)param2);

            engine.HandleShowScreen(screen, idxObj);
        }

        /// <summary>
        /// Notify the system to run a special command.
        /// </summary>
        /// <param name="param1">Text for the command to call.</param>
		public void NotifyOS(object param1)
        {
			string command = param1 == null ? "" : (string)param1;

            engine.HandleNotifyOS(command);
        }

		#endregion

		#region WIGInternals Functions for events

		/// <summary>
		/// Event, which is called, if the attribute of a object has changed.
		/// </summary>
		/// <returns>An empty object array.</returns>
		/// <param name="param1">LuaTable with object and a string, describing which attribute has changed.</param>
		public void AttributeChangedEvent(object param1, object param2)
		{
			LuaTable obj = (LuaTable)param1;
			string type = param2 == null ? "" : (string)param2;

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

			if (OnAttributeChanged != null)
				OnAttributeChanged(obj,type);
		}

		/// <summary>
		/// Event, which is called, if attribute of a cartridge has changed.
		/// </summary>
		/// <returns>An empty object array.</returns>
		/// <param name="param1">LuaTable with a string, describing which attribute has changed.</param>
		public void CartridgeEvent(object param1)
		{
			string type = param1 == null ? "" : (string)param1;

			// Possible types
			// "complete"
			// "sync"

			if (OnCartridgeChanged != null)
				OnCartridgeChanged(type);
		}

		/// <summary>
		/// Event, which is called, if a command has changed.
		/// </summary>
		/// <returns>An empty object array.</returns>
		/// <param name="param1">LuaTable with the command that had be changed.</param>
		public void CommandChangedEvent(object param1)
		{
			LuaTable zcmd = (LuaTable)param1;

			if (OnCommandChanged != null)
				OnCommandChanged(zcmd);
		}

		/// <summary>
		/// Event, which is called, if the inventory of the player has changed.
		/// </summary>
		/// <returns>An empty object array.</returns>
		/// <param name="param1">LuaTable with object and containers, where the object comes from and where it goes to.</param>
		public void InventoryEvent(object param1, object param2, object param3)
        {
			LuaTable obj = (LuaTable)param1;
			LuaTable fromContainer = (LuaTable)param2;
			LuaTable toContainer = (LuaTable)param3;

            if (OnInventoryChanged != null)
                OnInventoryChanged((LuaTable)obj,(LuaTable)fromContainer,(LuaTable)toContainer);
        }

		/// <summary>
		/// Event, which is called, if the state of a timer has changed.
		/// </summary>
		/// <returns>An empty object array.</returns>
		/// <param name="param1">LuaTable with the timer and a string, describing the event.</param>
		public void TimerEvent(object param1, object param2)
        {
			LuaTable timer = (LuaTable)param1;
			string type = param2 == null ? "" : (string)param2;

			if (type.ToLower().Equals("start") && OnTimerStarted != null)
                OnTimerStarted(timer);

            if (type.ToLower().Equals("stop") && OnTimerStopped != null)
                OnTimerStopped(timer);
        }

		/// <summary>
		/// Event, which is called, if the state of one or more zones had changed.
		/// </summary>
		/// <returns>An empty object array.</returns>
		/// <param name="param1">LuaTable with zones, that changed their state.</param>
		public void ZoneStateChangedEvent(object param1)
		{
			LuaTable zones;
			lock (luaState)
			{
				zones = param1 == null ? luaState.EmptyTable() : (LuaTable)param1; 
			}

			if (OnZoneStateChanged != null)
				OnZoneStateChanged(zones);
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
		public bool IsPointInZone(object param1, object param2)
        {
            LuaTable zonePoint;
            LuaTable zone;

            if (param1 == null || param2 == null)
                return false;

            if (!(param1 is LuaTable))
                throw new ArgumentException(String.Format("bad argument #1 to 'IsPointInZone' (ZonePoint expected, got {0})", param1.GetType()));

            zonePoint = (LuaTable)param1;

            if (!(param2 is LuaTable))
                throw new ArgumentException(String.Format("bad argument #2 to 'IsPointInZone' (Zone expected, got {0})", param2.GetType()));

            zone = (LuaTable)param2;

            double lat, lon;
			LuaTable points;
			int count;

			lock (luaState)
			{
				lat = (double)zonePoint["latitude"].ToNumber();
				lon = (double)zonePoint["longitude"].ToNumber();
				points = (LuaTable)zone["Points"];
				count = points.Keys.Count;
			}

            double[] lats = new double[count];
            double[] lons = new double[count];

            int i;

            for (i = 0; i < count; i++)
            {
				lock (luaState)
				{
					lats[i] = (double)((LuaTable)points[i + 1])["latitude"].ToNumber();
					lons[i] = (double)((LuaTable)points[i + 1])["longitude"].ToNumber(); 
				}
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
        /// <param name="bearing">Double value for bearing to calculated point on line.</param>
        /// <returns>LuaTable for distance to calculated nearest point of zone.</returns>
		public LuaVararg VectorToZone(object pointObj, object zoneObj)
        {
			List<LuaValue> ret = new List<LuaValue>(2);

            if (!(pointObj is LuaTable))
                throw new ArgumentException(String.Format("bad argument #1 to 'VectorToZone' (ZonePoint expected, got {0})", pointObj.GetType()));

			LuaTable zonePoint = (LuaTable)pointObj;

            if (!(zoneObj is LuaTable))
                throw new ArgumentException(String.Format("bad argument #2 to 'VectorToZone' (Zone expected, got {0})", zoneObj.GetType()));

            LuaTable zone = (LuaTable)zoneObj;

            // If the point is in the zone, the distance and bearing are 0.
			if (IsPointInZone(zonePoint, zone))
			{
				lock (luaState)
				{
					ret.Add((LuaTable)luaState.DoString("return Wherigo.Distance(0)")[0]); 
					ret.Add(0);

					return new LuaVararg(ret, true);
				}
			}

			// If the zone doesn't have points, the distance and bearing are null.
			LuaTable points;
			lock (luaState)
			{
				points = zone["Points"] as LuaTable;
			}
			if (points == null)
			{
				ret.Add(null); 
				ret.Add(0);

				return new LuaVararg(ret, true);
			}

			// Performs the computation.

			double k;
			LuaVararg td, current;

			lock (luaState)
			{
				current = VectorToSegment(pointObj, points[points.Keys.Count], points[1]);
				var pairs = points.GetEnumerator();

				while (pairs.MoveNext())
				{
					k = (double)pairs.Current.Key.ToNumber();
					if (k > 1)
					{
						td = VectorToSegment(pointObj, points[k - 1], points[k]);
						if ((double)((LuaTable)td[0])["value"].ToNumber() < (double)((LuaTable)current[0])["value"].ToNumber())
						{
							current = td;
						}
					}
				} 
			}

			ret.Add(current[0]);
			ret.Add((double)current[1].ToNumber() % 360);

			return new LuaVararg(ret,true);
        }

        /// <summary>
        /// Calculate distance and bearing of ZonePoint to line between two points with shortest distance.
        /// </summary>
        /// <param name="pointObj">LuaTable for ZonePoint.</param>
        /// <param name="firstLinePointObj">LuaTable for first ZonePoint of line.</param>
        /// <param name="secondLinePointObj">LuaTable for second ZonePoint of line.</param>
        /// <param name="bearing">Double value for bearing to calculated point on line.</param>
        /// <returns>LuaTable for distance to calculated point on line.</returns>
		public LuaVararg VectorToSegment(object pointObj, object firstLinePointObj, object secondLinePointObj)
        {
			if (pointObj is LuaNil || firstLinePointObj is LuaNil || secondLinePointObj is LuaNil) {
				List<LuaValue> ret = new List<LuaValue>(2);

				ret.Add (LuaNil.Instance);
				ret.Add (LuaNil.Instance);

				return new LuaVararg(ret, true);
			}

			LuaVararg d1 = VectorToPoint(firstLinePointObj, pointObj);
			double b1 = (double)d1[1].ToNumber();
			double dd1 = PI_180 * ((Distance)engine.GetTable((LuaTable)d1[0])).ValueAs(DistanceUnit.NauticalMiles) / 60;

			LuaVararg ds = VectorToPoint(firstLinePointObj, secondLinePointObj);
			double bs = (double)ds[1].ToNumber();
			double dds = PI_180 * ((Distance)engine.GetTable((LuaTable)ds[0])).ValueAs(DistanceUnit.NauticalMiles) / 60;

			var dist = Math.Asin(Math.Sin(dd1) * Math.Sin(PI_180 * (b1 - bs)));
			var dat = Math.Acos(Math.Cos(dd1) / Math.Cos(dist));
			if (dat <= 0)
			{
				return VectorToPoint(pointObj, firstLinePointObj);
			}
			else if (dat >= PI_180 * dds)
			{
				return VectorToPoint(pointObj, secondLinePointObj);
			}

			LuaTable intersect;

			lock (luaState)
			{
				intersect = TranslatePoint(firstLinePointObj, luaState.DoString(String.Format("return Wherigo.Distance({0}, 'nauticalmiles')", (dat * 60).ToString(nfi)))[0], bs); 
			}

			return VectorToPoint(pointObj, intersect);
		}

        /// <summary>
        /// Calculate distance and bearing from one ZonePoint to another.
        /// </summary>
        /// <param name="param1">LuaTable for first ZonePoint.</param>
        /// <param name="param2">LuaTable for second ZonePoint</param>
        /// <param name="bearing">Double value for bearing from first point to second point.</param>
        /// <returns></returns>
		public LuaVararg VectorToPoint(object param1, object param2)
        {
			List<LuaValue> ret = new List<LuaValue>(2);

			if (param1 is LuaNil || param2 is LuaNil) {
				ret.Add (LuaNil.Instance);
				ret.Add (LuaNil.Instance);

				return new LuaVararg(ret, true);
			}

            LuaTable zonePoint1 = (LuaTable)param1;
            LuaTable zonePoint2 = (LuaTable)param2;

			double lat1, lon1, lat2, lon2;
			lock (luaState)
			{
				lat1 = (double)zonePoint1["latitude"].ToNumber();
				lon1 = (double)zonePoint1["longitude"].ToNumber();
				lat2 = (double)zonePoint2["latitude"].ToNumber();
				lon2 = (double)zonePoint2["longitude"].ToNumber(); 
			}

			double distance, bearing;

            double mx = Math.Abs(CoreLat2M(lat1 - lat2));
            double my = Math.Abs(CoreLon2M(lat2, lon1 - lon2));

            distance = Math.Sqrt(mx * mx + my * my);
            bearing = (Math.Atan2(CoreLat2M(lat2 - lat1), CoreLon2M(lat2, lon2 - lon1)) + Math.PI / 2) * (180.0 / Math.PI);

			lock (luaState)
			{
				ret.Add((LuaTable)luaState.DoString(String.Format("return Wherigo.Distance({0},'m')", distance.ToString(nfi)))[0]); 
				ret.Add(bearing);

				return new LuaVararg(ret, true);
			}
        }

        /// <summary>
        /// Calculate new point with distance and bearing from old point.
        /// </summary>
        /// <param name="param1">ZonePoint to use for calculation.</param>
        /// <param name="param2">LuaTable for distance.</param>
        /// <param name="param3">Double value for bearing</param>
        /// <returns></returns>
		public LuaTable TranslatePoint(object param1, object param2, object param3)
        {
            LuaTable zonePoint = (LuaTable)param1;
            LuaTable distance = (LuaTable)param2;
			double bearing = (double)param3;

			double lat, lon, alt, dist;
			lock (luaState)
			{
				lat = (double)zonePoint["latitude"].ToNumber();
				lon = (double)zonePoint["longitude"].ToNumber();
				alt = (double)((LuaTable)zonePoint["altitude"])["value"].ToNumber();
				dist = (double)distance["value"].ToNumber(); 
			}

            double rad = CoreAzimuth2Angle(bearing);
            double x = CoreM2Lat(dist * Math.Sin(rad));
            double y = CoreM2Lon(lat, dist * Math.Cos(rad));

			lock (luaState)
			{
				return (LuaTable)luaState.DoString(String.Format("return Wherigo.ZonePoint({0},{1},{2})", (lat + x).ToString(nfi), (lon + y).ToString(nfi), alt.ToString(nfi)))[0]; 
			}
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
