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
using NLua;
using WF.Player.Core.Engines;

namespace WF.Player.Core
{
	/// <summary>
	/// An entity of the game that can be interacted with.
	/// </summary>
	public class Thing : UIObject
	{

		#region Constructor

		internal Thing (Engine e, LuaTable t) : base (e, t)
		{
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the commands for this item.
		/// </summary>
		/// <value>The list of commands.</value>
		public List<Command> Commands {
			get 
			{
				return GetTableList<Command>("Commands");
			}
		}

		/// <summary>
		/// Gets the container.
		/// </summary>
		/// <value>The container.</value>
		public Thing Container {
			get 
			{
				return GetTable("Container") as Thing;
			}
		}

		/// <summary>
		/// Gets a list of available commands.
		/// </summary>
		public List<Command> ActiveCommands {
			get 
			{
				return GetTableFuncList<Command>("GetActiveCommands");
			}
		}

		/// <summary>
		/// Gets the inventory.
		/// </summary>
		/// <value>The inventory.</value>
		public List<Thing> Inventory {
			get 
			{
				return GetTableList<Thing>("Inventory");
			}
		}

		/// <summary>
		/// Gets the distance and bearing between the player and this Thing.
		/// </summary>
		public LocationVector VectorFromPlayer
		{
			get
			{
				return engine.GetVectorFromPlayer(this);
			}
		}

		#endregion

	}

}
