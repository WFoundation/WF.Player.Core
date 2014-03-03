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
	/// Implements the Lua libary WIGInternal.
	/// </summary>
#if MONOTOUCH
	    [MonoTouch.Foundation.Preserve(AllMembers=true)]
#endif
	public class WIGInternalImpl
	{
		#region Fields

		private Engine _engine;

		private LuaDataFactory _dataFactory;

		private GeoMathHelper _mathHelper;

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

		#region Constructor

		internal WIGInternalImpl(Engine engine, LuaDataFactory dataFactory)
		{
			this._engine = engine;
			this._dataFactory = dataFactory;
			this._mathHelper = new GeoMathHelper(dataFactory);
			LuaDataContainer wiginternal = dataFactory.CreateContainerAt("WIGInternal");

			// WIGInternal Lua hooks: UI-related events
			AddMethodToTable(wiginternal, "LogMessage");
			AddMethodToTable(wiginternal, "MessageBox");
			AddMethodToTable(wiginternal, "GetInput");
			AddMethodToTable(wiginternal, "NotifyOS");
			AddMethodToTable(wiginternal, "ShowScreen");
			AddMethodToTable(wiginternal, "ShowStatusText");

			// WIGInternal Lua hooks: gameplay-related events
			AddMethodToTable(wiginternal, "AttributeChangedEvent");
			AddMethodToTable(wiginternal, "CartridgeEvent");
			AddMethodToTable(wiginternal, "CommandChangedEvent");
			AddMethodToTable(wiginternal, "InventoryEvent");
			AddMethodToTable(wiginternal, "MediaEvent");
			AddMethodToTable(wiginternal, "TimerEvent");
			AddMethodToTable(wiginternal, "ZoneStateChangedEvent");

			// WIGInternal Lua hooks: internal functions
			AddMethodToTable(wiginternal, "IsPointInZone", "IsPointInZoneLua");
			AddMethodToTable(wiginternal, "VectorToZone", "VectorToZoneLua");
			AddMethodToTable(wiginternal, "VectorToSegment", "VectorToSegmentLua");
			AddMethodToTable(wiginternal, "VectorToPoint", "VectorToPointLua");
			AddMethodToTable(wiginternal, "TranslatePoint", "TranslatePointLua");

			// Marks package WIGInternal as loaded
			dataFactory.SetContainerAt("package.loaded.WIGInternal", wiginternal);
			dataFactory.SetContainerAt("package.preload.WIGInternal", wiginternal);

			// Loads the Wherigo LUA engine.
			dataFactory.LoadAndRunEngine();
		}

		private void AddMethodToTable(LuaDataContainer container, string fieldName, string funcName = null)
		{
			// Gets the type of the delegate to create.
			Type dType = this.GetType().GetNestedType(fieldName + "Delegate", BindingFlags.NonPublic);

			// Gets the delegate for the method to create.
			Delegate d = Delegate.CreateDelegate(dType, this, funcName ?? fieldName);

			// Creates the function and sets it to the table.
			container.BindWithFunction(fieldName, d);
		}

		#endregion

		#region WIGInternals Functions for UI

		public void LogMessage(double level, string message)
		{
			int nlevel = Convert.ToInt32(level);
			string nmessage = message ?? "";

			_engine.HandleLogMessage(nlevel, nmessage);
		}

		public void MessageBox(string text, double idxMediaObj, string btn1Label, string btn2Label, LuaFunction provider)
		{
			string ntext = text == null ? "" : (string)text;
			int nidxMediaObj = Convert.ToInt32(idxMediaObj);
			string nbtn1Label = btn1Label == null ? "" : (string)btn1Label;
			string nbtn2Label = btn2Label == null ? "" : (string)btn2Label;
			IDataProvider nprovider = _dataFactory.GetProvider((LuaFunction)provider, protectFromGC: true);

			_engine.HandleShowMessage(
				ntext,
				_dataFactory.GetWherigoObject<Media>(nidxMediaObj),
				nbtn1Label,
				nbtn2Label,
				nprovider
			);
		}

		public void GetInput(LuaTable input)
		{
			if (input != null)
				_engine.HandleGetInput(_dataFactory.GetWherigoObject<Input>(input));
		}

		public void MediaEvent(double type, LuaTable media)
		{
			int ntype = Convert.ToInt32(type);

			Media mediaObj = media == null ? null : _dataFactory.GetWherigoObject<Media>(media);

			_engine.HandlePlayMedia(ntype, mediaObj);
		}

		public void ShowStatusText(string text)
		{
			string ntext = text == null ? "" : (string)text;

			_engine.HandleShowStatusText(ntext);
		}

		public void ShowScreen(double screen, double idxObj)
		{
			int nscreen = Convert.ToInt32(screen);
			int nidxObj = Convert.ToInt32(idxObj);

			_engine.HandleShowScreen(nscreen, nidxObj);
		}

		public void NotifyOS(string command)
		{
			string ncommand = command == null ? "" : (string)command;

			_engine.HandleNotifyOS(ncommand);
		}

		#endregion

		#region WIGInternals Functions for events

		public void AttributeChangedEvent(LuaTable obj, string type)
		{
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

			_engine.HandleAttributeChanged(_dataFactory.GetWherigoObject(obj), type);

		}

		public void CartridgeEvent(string type)
		{
			// Possible types
			// "complete"
			// "sync"

			_engine.HandleCartridgeChanged(type);
		}

		public void CommandChangedEvent(LuaTable zcmd)
		{
			_engine.HandleCommandChanged(_dataFactory.GetWherigoObject<Command>(zcmd));
		}

		public void InventoryEvent(LuaTable obj, LuaTable fromContainer, LuaTable toContainer)
		{
			_engine.HandleInventoryChanged(
				_dataFactory.GetWherigoObject<Thing>(obj),
				_dataFactory.GetWherigoObject<Thing>(fromContainer),
				_dataFactory.GetWherigoObject<Thing>(toContainer)
			);
		}

		public void TimerEvent(LuaTable timer, string type)
		{
			string ntype = type == null ? "" : (string)type;

			Timer timerObj = _dataFactory.GetWherigoObject<Timer>(timer);

			if ("start".Equals(ntype))
				_engine.HandleTimerStarted(timerObj);

			else if ("stop".Equals(ntype))
				_engine.HandleTimerStopped(timerObj);
		}

		public void ZoneStateChangedEvent(LuaTable zoneList)
		{
			IDataContainer zones = zoneList == null ? _dataFactory.CreateContainer() : _dataFactory.GetContainer(zoneList);

			_engine.HandleZoneStateChanged(_dataFactory.GetWherigoObjectList<Zone>(zones));
		}

		#endregion

		#region WIGInternal Functions for location calculation

		public bool IsPointInZoneLua(LuaTable zonePoint, LuaTable zone)
		{
			if (zonePoint == null || zone == null)
			{
				throw new ArgumentNullException();
			}

			// Gets the data model entities.
			ZonePoint zonePointEntity = _dataFactory.GetWherigoObject<ZonePoint>(zonePoint);
			Zone zoneEntity = _dataFactory.GetWherigoObject<Zone>(zone);

			// Computes and returns.
			return _mathHelper.IsPointInZone(zonePointEntity, zoneEntity);
		}

		public LuaVararg VectorToZoneLua(LuaTable zonePoint, LuaTable zone)
		{
			List<LuaValue> ret = new List<LuaValue>(2);

			if (zonePoint == null || zone == null)
			{
				throw new ArgumentNullException();
			}

			// Gets the data model entities.
			ZonePoint zonePointEntity = _dataFactory.GetWherigoObject<ZonePoint>(zonePoint);
			Zone zoneEntity = _dataFactory.GetWherigoObject<Zone>(zone);

			// Performs the computation.
			LocationVector vector = _mathHelper.VectorToZone(zonePointEntity, zoneEntity);

			// Prepares the lua return.
			ret.Add(vector.Distance != null ? (LuaValue)_dataFactory.GetNativeContainer(vector.Distance) : LuaNil.Instance);
			ret.Add(vector.Bearing);

			return new LuaVararg(ret, true);
		}

		public LuaVararg VectorToSegmentLua(LuaTable zonePoint, LuaTable firstLineZonePoint, LuaTable secondLineZonePoint)
		{
			List<LuaValue> ret = new List<LuaValue>(2);

			if (zonePoint == null || firstLineZonePoint == null || secondLineZonePoint == null)
			{
				ret.Add(LuaNil.Instance);
				ret.Add(LuaNil.Instance);

				return new LuaVararg(ret, true);
			}

			// Gets the data model entities.
			ZonePoint zonePointEntity = _dataFactory.GetWherigoObject<ZonePoint>(zonePoint);
			ZonePoint firstLinezonePointEntity = _dataFactory.GetWherigoObject<ZonePoint>(firstLineZonePoint);
			ZonePoint secondLinezonePointEntity = _dataFactory.GetWherigoObject<ZonePoint>(secondLineZonePoint);

			// Performs the computation.
			LocationVector lv = _mathHelper.VectorToSegment(
				_dataFactory.GetWherigoObject<ZonePoint>(zonePoint),
				_dataFactory.GetWherigoObject<ZonePoint>(firstLineZonePoint),
				_dataFactory.GetWherigoObject<ZonePoint>(secondLineZonePoint)
			);

			// Prepares the lua return.
			ret.Add(_dataFactory.GetNativeContainer(lv.Distance));
			ret.Add(lv.Bearing.GetValueOrDefault());
			return new LuaVararg(ret, true);
		}

		public LuaVararg VectorToPointLua(LuaTable zonePoint1, LuaTable zonePoint2)
		{
			List<LuaValue> ret = new List<LuaValue>(2);

			// Gets the data model entities.
			ZonePoint zonePoint1Entity = _dataFactory.GetWherigoObject<ZonePoint>(zonePoint1);
			ZonePoint zonePoint2Entity = _dataFactory.GetWherigoObject<ZonePoint>(zonePoint2);

			// Performs the computation.
			LocationVector lv = _mathHelper.VectorToPoint(
				_dataFactory.GetWherigoObject<ZonePoint>(zonePoint1),
				_dataFactory.GetWherigoObject<ZonePoint>(zonePoint2)
			);

			// Prepares the lua return.
			ret.Add(_dataFactory.GetNativeContainer(lv.Distance));
			ret.Add(lv.Bearing.GetValueOrDefault());
			return new LuaVararg(ret, true);
		}

		public LuaTable TranslatePointLua(LuaTable zonePoint, LuaTable distance, double bearing)
		{
			// Gets the data model entities.
			ZonePoint zonePointEntity = _dataFactory.GetWherigoObject<ZonePoint>(zonePoint);
			Distance distanceEntity = _dataFactory.GetWherigoObject<Distance>(distance);

			// Performs the computation.
			ZonePoint ret = _mathHelper.TranslatePoint(
				_dataFactory.GetWherigoObject<ZonePoint>(zonePoint),
				new LocationVector(
					_dataFactory.GetWherigoObject<Distance>(distance),
					bearing
				)
			);

			return _dataFactory.GetNativeContainer(ret);
		}

		#endregion
	}

}
