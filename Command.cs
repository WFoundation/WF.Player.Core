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

	public class Command : Table
	{

		#region Constructor

		public Command (Engine e, LuaTable t) : base (e, t)
		{
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets a value indicating whether this <see cref="WF.Player.Core.Command"/> works with other objects.
		/// </summary>
		/// <value><c>true</c> if command works with other objects; otherwise, <c>false</c>.</value>
		public bool CmdWith {
			get {
				return GetBool ("CmdWith");
			}
		}

		/// <summary>
		/// Gets the empty target list text to show, when no targets availible.
		/// </summary>
		/// <value>The empty target list text.</value>
		public string EmptyTargetListText {
			get {
				return GetString ("EmptyTargetListText");
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="WF.Player.Core.Command"/> is enabled.
		/// </summary>
		/// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
		public bool Enabled {
			get {
				return GetBool ("Enabled");
			}
		}

		/// <summary>
		/// Gets the owner.
		/// </summary>
		/// <value>The owner.</value>
		public Thing Owner {
			get {
				return (Thing)GetTable ((LuaTable)wigTable["Owner"]);
			}
		}

		public List<Thing> TargetObjects {
			get {
				List<Thing> result = new List<Thing> ();

				var t = ((LuaTable)((LuaFunction)wigTable["CalcTargetObjects"]).Call (new object[] { wigTable, engine.Cartridge.WIGTable, engine.Player })[0]).GetEnumerator();
				while (t.MoveNext())
					result.Add ((Thing)GetTable ((LuaTable)t.Value));

				return result;
			}
		}

		/// <summary>
		/// Gets the text.
		/// </summary>
		/// <value>The text.</value>
		public string Text {
			get {
				return GetString ("Text");
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="WF.Player.Core.Command"/> works only with special objects.
		/// </summary>
		/// <value><c>true</c> if works with list; otherwise, <c>false</c>.</value>
		public bool WorksWithList {
			get {
				return GetBool ("WorksWithList");
			}
		}

		/// <summary>
		/// Gets the works with list objects for this commands, which are in the list and visible.
		/// </summary>
		/// <value>The works with list objects.</value>
		public List<Thing> WorksWithListObjects {
			get {
				List<Thing> result = new List<Thing> ();

				// Works this command with targets?
				if (WorksWithList) {
					// Get all tables
					var t = ((LuaTable)engine.Call (wigTable, "CalcTargetObjects", new object[] { wigTable, engine.Cartridge.WIGTable, engine.Player }) [0]).GetEnumerator();
					while (t.MoveNext())
						result.Add ((Thing)GetTable ((LuaTable)t.Value));
				}

				return result;
			}
		}

		#endregion

		#region Methods

		public void Execute(Thing t = null)
		{
			if (t == null)
				((LuaFunction)wigTable["exec"]).Call (new object[] { wigTable });
			else
				((LuaFunction)wigTable["exec"]).Call (new object[] { wigTable, t });
		}

		#endregion

	}

}

