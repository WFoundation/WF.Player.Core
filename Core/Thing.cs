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
using WF.Player.Core.Engines;

namespace WF.Player.Core
{
	/// <summary>
	/// An entity of the game that can be interacted with.
	/// </summary>
	public class Thing : UIObject
	{

		#region Constructor

		internal Thing(WF.Player.Core.Data.IDataContainer data, RunOnClick runOnClick)
			: base(data, runOnClick)
		{
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the commands for this item.
		/// </summary>
		/// <value>The list of commands.</value>
        public WherigoCollection<Command> Commands
        {
			get 
			{
				return DataContainer.GetWherigoObjectList<Command>("Commands");
			}
		}

		/// <summary>
		/// Gets the container.
		/// </summary>
		/// <value>The container.</value>
		public Thing Container {
			get 
			{
				return DataContainer.GetWherigoObject<Thing>("Container");
			}
		}

		/// <summary>
		/// Gets a list of available commands.
		/// </summary>
		public WherigoCollection<Command> ActiveCommands {
			get 
			{
				return DataContainer.GetWherigoObjectListFromProvider<Command>("GetActiveCommands");
			}
		}

		/// <summary>
		/// Gets the inventory.
		/// </summary>
		/// <value>The inventory.</value>
        public WherigoCollection<Thing> Inventory
        {
			get 
			{
				return DataContainer.GetWherigoObjectList<Thing>("Inventory");
			}
		}

		/// <summary>
		/// Gets the distance and bearing between the player and this Thing.
		/// </summary>
		public LocationVector VectorFromPlayer
		{
			get;
			internal set;
		}

		#endregion

	}

}
