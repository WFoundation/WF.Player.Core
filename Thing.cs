///
/// WF.Player.Core - A Wherigo Player Core for different platforms.
/// Copyright (C) 2012-2013  Dirk Weltz <web@weltz-online.de>
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

namespace WF.Player.Core
{

	public class Thing : UIObject
	{

		#region Constructor

		public Thing (Engine e, LuaTable t) : base (e, t)
		{
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the commands for this item.
		/// </summary>
		/// <value>The list of commands.</value>
		public List<Command> Commands {
			get {
				List<Command> result = new List<Command> ();

				var c = ((LuaTable)wigTable ["Commands"]).GetEnumerator ();
				while(c.MoveNext())
					result.Add ((Command)GetTable ((LuaTable)c.Value));

				return result;
			}
		}

		/// <summary>
		/// Gets the container.
		/// </summary>
		/// <value>The container.</value>
		public Thing Container {
			get {
				return (Thing)GetTable ((LuaTable)wigTable["Container"]);
			}
		}

		public List<Command> ActiveCommands {
			get {
				List<Command> result = new List<Command> ();

				var c = ((LuaTable)((LuaFunction)wigTable["GetActiveCommands"]).Call (new object[] { wigTable })[0]).GetEnumerator ();
				while(c.MoveNext())
					result.Add ((Command)GetTable ((LuaTable)c.Value));

				return result;
			}
		}

		/// <summary>
		/// Gets the inventory.
		/// </summary>
		/// <value>The inventory.</value>
		public List<Thing> Inventory {
			get {
				List<Thing> result = new List<Thing> ();

				var t = ((LuaTable)wigTable ["Inventory"]).GetEnumerator ();
				while (t.MoveNext())
					result.Add ((Thing)GetTable ((LuaTable)t.Value));

				return result;
			}
		}

		#endregion

		#region Methods

		#endregion

	}

}
