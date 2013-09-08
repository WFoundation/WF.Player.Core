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
using NLua;


namespace WF.Player.Core
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
        private Lua luaState;
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

        #region Constructor

        public WIGInternalImpl( Engine engine, Lua luaState)
        {
			this.engine = engine;
            this.luaState = luaState;

			// Need this, because iPhone has problems at DoString with numbers, that have NumberDecimalSeparator other than "."
			nfi = new NumberFormatInfo();
			nfi.NumberDecimalSeparator = ".";
			nfi.NumberGroupSeparator = "";

			luaState.NewTable ("WIGInternal");
			LuaTable wiginternal = (LuaTable)luaState["WIGInternal"];

            // Internal functions
			wiginternal["IsPointInZone"] = luaState.RegisterFunction("IsPointInZone", this, this.GetType().GetMethod("IsPointInZone"));
            // TODO: Implement this functions in C# instead of Lua.
			wiginternal["VectorToZone"] = luaState.RegisterFunction("VectorToZone", this, this.GetType().GetMethod("VectorToZone"));
			wiginternal["VectorToSegment"] = luaState.RegisterFunction("VectorToSegment", this, this.GetType().GetMethod ("VectorToSegment"));
			wiginternal["VectorToPoint"] = luaState.RegisterFunction("VectorToPoint", this, this.GetType().GetMethod("VectorToPoint"));
			wiginternal["TranslatePoint"] = luaState.RegisterFunction("TranslatePoint", this, this.GetType().GetMethod("TranslatePoint"));

            // Interface for GUI
            wiginternal["LogMessage"] = luaState.RegisterFunction("LogMessage", this, this.GetType().GetMethod("LogMessage"));
			wiginternal["MessageBox"] = luaState.RegisterFunction("MessageBox", this, this.GetType().GetMethod("MessageBox"));
			wiginternal["GetInput"] = luaState.RegisterFunction("GetInput", this, this.GetType().GetMethod("GetInput"));
			wiginternal["ShowStatusText"] = luaState.RegisterFunction("ShowStatusText", this, this.GetType().GetMethod("ShowStatusText"));
			wiginternal["ShowScreen"] = luaState.RegisterFunction("ShowScreen", this, this.GetType().GetMethod("ShowScreen"));
			wiginternal["NotifyOS"] = luaState.RegisterFunction("NotifyOS", this, this.GetType().GetMethod("NotifyOS"));

            // Events
			wiginternal["MediaEvent"] = luaState.RegisterFunction("MediaEvent", this, this.GetType().GetMethod("MediaEvent"));
			wiginternal["ZoneStateChangedEvent"] = luaState.RegisterFunction("ZoneStateChangedEvent", this, this.GetType().GetMethod ("ZoneStateChangedEvent"));
			wiginternal["InventoryEvent"] = luaState.RegisterFunction("InventoryEvent", this, this.GetType().GetMethod("InventoryEvent"));
			wiginternal["TimerEvent"] = luaState.RegisterFunction("TimerEvent", this, this.GetType().GetMethod("TimerEvent"));
			wiginternal["CartridgeEvent"] = luaState.RegisterFunction("CartridgeEvent", this, this.GetType().GetMethod("CartridgeEvent"));
			wiginternal["CommandChangedEvent"] = luaState.RegisterFunction("CommandChangedEvent", this, this.GetType().GetMethod("CommandChangedEvent"));
			wiginternal["AttributeChangedEvent"] = luaState.RegisterFunction("AttributeChangedEvent", this, this.GetType().GetMethod("AttributeChangedEvent"));

            // Mark package WIGInternal as loaded
			LuaTable package = (LuaTable)luaState["package"];
			LuaTable loaded = (LuaTable)package["loaded"];
            loaded["WIGInternal"] = wiginternal;
			package["preload.WIGInternal"] = wiginternal;

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
        public void LogMessage(object param1, string param2)
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
            int idxMediaObj = param2 == null ? -1 : Convert.ToInt32 (param2);
            string btn1Label = param3 == null ? "" : (string)param3;
            string btn2Label = param4 == null ? "" : (string)param4;
            LuaFunction wrapper = (LuaFunction)param5;

			engine.HandleShowMessage(text, engine.GetMedia(idxMediaObj), btn1Label, btn2Label, (retValue) => wrapper.Call (new object[] { retValue }));
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
            int type = param1 == null ? 0 : Convert.ToInt32 (param1);
			Media mediaObj = param2 == null ? null : engine.Cartridge.Resources[Convert.ToInt32 ((double)((LuaTable)param2)["ObjIndex"])];

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
		public object[] CartridgeEvent(object param1)
		{
			string type = param1 == null ? "" : (string)param1;

			// Possible types
			// "complete"
			// "sync"

			if (OnCartridgeChanged != null)
				OnCartridgeChanged(type);

			return new object[0];
		}

		/// <summary>
		/// Event, which is called, if a command has changed.
		/// </summary>
		/// <returns>An empty object array.</returns>
		/// <param name="param1">LuaTable with the command that had be changed.</param>
		public object[] CommandChangedEvent(object param1)
		{
			LuaTable zcmd = (LuaTable)param1;

			if (OnCommandChanged != null)
				OnCommandChanged(zcmd);

			return new object[0];
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
		public object[] TimerEvent(object param1, object param2)
        {
			LuaTable timer = (LuaTable)param1;
			string type = param2 == null ? "" : (string)param2;

			if (type.ToLower().Equals("start") && OnTimerStarted != null)
                OnTimerStarted(timer);

            if (type.ToLower().Equals("stop") && OnTimerStopped != null)
                OnTimerStopped(timer);

            return new object[0];
        }

		/// <summary>
		/// Event, which is called, if the state of one or more zones had changed.
		/// </summary>
		/// <returns>An empty object array.</returns>
		/// <param name="param1">LuaTable with zones, that changed their state.</param>
		public object[] ZoneStateChangedEvent(object param1)
		{
			LuaTable zones = param1 == null ? (LuaTable)luaState.DoString ("return {}")[0] : (LuaTable)param1;

			if (OnZoneStateChanged != null)
				OnZoneStateChanged(zones);

			return new object[0];
		}

		#endregion

        #region WIGInternal Functions for location calculation

        /// <summary>
        /// Checks, if the given ZonePoint is in Zone.
        /// </summary>
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

            double lat = (double)zonePoint["latitude"];
            double lon = (double)zonePoint["longitude"];

            LuaTable points = (LuaTable)zone["Points"];
            int count = points.Keys.Count;

            double[] lats = new double[count];
            double[] lons = new double[count];

            int i;

            for (i = 0; i < count; i++)
            {
                lats[i] = (double)((LuaTable)points[i + 1])["latitude"];
                lons[i] = (double)((LuaTable)points[i + 1])["longitude"];
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
        public LuaTable VectorToZone(object pointObj, object zoneObj, out double bearing)
        {
            if (!(pointObj is LuaTable))
                throw new ArgumentException(String.Format("bad argument #1 to 'VectorToZone' (ZonePoint expected, got {0})", pointObj.GetType()));

			LuaTable zonePoint = (LuaTable)pointObj;

            if (!(zoneObj is LuaTable))
                throw new ArgumentException(String.Format("bad argument #2 to 'VectorToZone' (Zone expected, got {0})", zoneObj.GetType()));

            LuaTable zone = (LuaTable)zoneObj;

            // If the point is in the zone, the distance and bearing are 0.
			if (IsPointInZone(zonePoint, zone))
			{
				bearing = 0;
				return (LuaTable)luaState.DoString("return Wherigo.Distance(0)")[0];
			}

			// If the zone doesn't have points, the distance and bearing are null.
			if (zone["Points"] == null)
			{
				bearing = double.NaN;
				return null;
			}

			// Performs the computation.

			LuaTable points = (LuaTable)zone["Points"];

			double b, tb, k;
			LuaTable td, current = VectorToSegment(pointObj, points[points.Keys.Count], points[1], out b);
			var pairs = points.GetEnumerator();
			
			while (pairs.MoveNext())
			{
				k = (double) pairs.Key;
				if (k > 1)
				{
					td = VectorToSegment(pointObj, points[k - 1], points[k], out tb);
					if ((double)td["value"] < (double)current["value"])
					{
						current = td;
						b = tb % 360;
					}
				}
			}

			bearing = b;
			return current;
        }

        /// <summary>
        /// Calculate distance and bearing of ZonePoint to line between two points with shortest distance.
        /// </summary>
        /// <param name="pointObj">LuaTable for ZonePoint.</param>
        /// <param name="firstLinePointObj">LuaTable for first ZonePoint of line.</param>
        /// <param name="secondLinePointObj">LuaTable for second ZonePoint of line.</param>
        /// <param name="bearing">Double value for bearing to calculated point on line.</param>
        /// <returns>LuaTable for distance to calculated point on line.</returns>
        public LuaTable VectorToSegment(object pointObj, object firstLinePointObj, object secondLinePointObj, out double bearing)
        {
			/*
			 *   local d1, b1 = VectorToPoint (p1, point)
				  local d1 = math.rad (d1('nauticalmiles') / 60.)
				  local ds, bs = VectorToPoint (p1, p2)
				  local dist = math.asin (math.sin (d1) * math.sin (math.rad (b1 - bs)))
				  local dat = math.acos (math.cos (d1) / math.cos (dist))
				  if dat <= 0 then
					return VectorToPoint (point, p1)
				  elseif dat >= math.rad (ds('nauticalmiles') / 60.) then
					return VectorToPoint (point, p2) 
				  end
				  local intersect = TranslatePoint (p1, Distance (dat * 60, 'nauticalmiles'), bs)
				  return VectorToPoint (point, intersect)
			 * */

			double b1, bs;

			LuaTable d1 = VectorToPoint(firstLinePointObj, pointObj, out b1);
			double dd1 = PI_180 * ((Distance)engine.GetTable(d1)).ValueAs(DistanceUnit.NauticalMiles) / 60;

			LuaTable ds = VectorToPoint(firstLinePointObj, secondLinePointObj, out bs);
			double dds = PI_180 * ((Distance)engine.GetTable(ds)).ValueAs(DistanceUnit.NauticalMiles) / 60;

			var dist = Math.Asin(Math.Sin(dd1) * Math.Sin(PI_180 * (b1 - bs)));
			var dat = Math.Acos(Math.Cos(dd1) / Math.Cos(dist));
			if (dat <= 0)
			{
				return VectorToPoint(pointObj, firstLinePointObj, out bearing);
			}
			else if (dat >= PI_180 * dds)
			{
				return VectorToPoint(pointObj, secondLinePointObj, out bearing);
			}

			var intersect = TranslatePoint(firstLinePointObj, luaState.DoString(String.Format("return Wherigo.Distance({0}, 'nauticalmiles')", dat * 60))[0], bs);

			return VectorToPoint(pointObj, intersect, out bearing);
		}

        /// <summary>
        /// Calculate distance and bearing from one ZonePoint to another.
        /// </summary>
        /// <param name="param1">LuaTable for first ZonePoint.</param>
        /// <param name="param2">LuaTable for second ZonePoint</param>
        /// <param name="bearing">Double value for bearing from first point to second point.</param>
        /// <returns></returns>
        public LuaTable VectorToPoint(object param1, object param2, out double bearing)
        {
            LuaTable zonePoint1 = (LuaTable)param1;
            LuaTable zonePoint2 = (LuaTable)param2;

            double lat1 = (double)zonePoint1["latitude"];
            double lon1 = (double)zonePoint1["longitude"];
            double lat2 = (double)zonePoint2["latitude"];
            double lon2 = (double)zonePoint2["longitude"];

            double distance;

            double mx = Math.Abs(CoreLat2M(lat1 - lat2));
            double my = Math.Abs(CoreLon2M(lat2, lon1 - lon2));

            distance = Math.Sqrt(mx * mx + my * my);

            bearing = (Math.Atan2(CoreLat2M(lat2 - lat1), CoreLon2M(lat2, lon2 - lon1)) + Math.PI / 2) * (180.0 / Math.PI);

            return (LuaTable)luaState.DoString(String.Format("return Wherigo.Distance({0},'m')", distance.ToString(nfi)))[0];
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

            double lat = (double)zonePoint["latitude"];
            double lon = (double)zonePoint["longitude"];
            double alt = (double)zonePoint["altitude.value"];
            double dist = (double)distance["value"];

            double rad = CoreAzimuth2Angle(bearing);
            double x = CoreM2Lat(dist * Math.Sin(rad));
            double y = CoreM2Lon(lat, dist * Math.Cos(rad));

            return (LuaTable)luaState.DoString(String.Format("return Wherigo.ZonePoint({0},{1},{2})", (lat + x).ToString(nfi), (lon + y).ToString(nfi), alt.ToString(nfi)))[0];
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
