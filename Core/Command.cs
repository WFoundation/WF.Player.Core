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
using WF.Player.Core.Data;

namespace WF.Player.Core
{
	/// <summary>
	/// An action of the game that can be executed.
	/// </summary>
	public class Command : WherigoObject
	{
		#region Delegates

		internal delegate WherigoCollection<Thing> CalcTargetObjects();

		internal delegate void ExecuteCommand(Thing target);

		#endregion

		#region Fields

		private CalcTargetObjects _calcTargetObjects;

		private ExecuteCommand _executeCommand;

		#endregion

		#region Constructor

		internal Command(IDataContainer data, CalcTargetObjects calcTargetObjs, ExecuteCommand execCommand)
			: base (data)
		{
			_calcTargetObjects = calcTargetObjs;
			_executeCommand = execCommand;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets a value indicating whether this <see cref="WF.Player.Core.Command"/> works with other objects.
		/// </summary>
		/// <value><c>true</c> if command works with other objects; otherwise, <c>false</c>.</value>
		public bool CmdWith {
			get {
                return DataContainer.GetBool("CmdWith").Value;
			}
		}

		/// <summary>
		/// Gets the empty target list text to show, when no targets availible.
		/// </summary>
		/// <value>The empty target list text.</value>
		public string EmptyTargetListText {
			get {
				return DataContainer.GetString("EmptyTargetListText");
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="WF.Player.Core.Command"/> is enabled.
		/// </summary>
		/// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
		public bool Enabled {
			get {
                return DataContainer.GetBool("Enabled").Value;
			}
		}

		/// <summary>
		/// Gets the owner.
		/// </summary>
		/// <value>The owner.</value>
		public Thing Owner {
			get 
            {
				return DataContainer.GetWherigoObject<Thing>("Owner");
            }
		}

		/// <summary>
		/// Gets a list if objects that this command can use as targets.
		/// </summary>
		public WherigoCollection<Thing> TargetObjects {
			get 
			{
				// Works this command with targets?
				return CmdWith ? _calcTargetObjects() : new WherigoCollection<Thing>();
			}
		}

		/// <summary>
		/// Gets the text.
		/// </summary>
		/// <value>The text.</value>
		public string Text {
			get 
			{
				return DataContainer.GetString("Text");
			}
		}

		/// <summary>
		/// Gets the works with list objects for this commands, regardless if active or inactive.
		/// </summary>
		/// <value>The works with list objects.</value>
        public WherigoCollection<Thing> WorksWithList
		{
			get
			{
                return CmdWith ? DataContainer.GetWherigoObjectListFromProvider<Thing>("WorksWithList") : new WherigoCollection<Thing>();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Executes this command on an optional target.
		/// </summary>
		/// <param name="target">The thing to execute this command on. If null, the command
		/// is executed without target. This parameter is mandatory if <code>CmdWith</code>
		/// is true.</param>
		public void Execute(Thing target = null)
		{
			//if (target == null)
			//    engine.LuaExecQueue.BeginCallSelf(this, "exec");
			//else
			//    engine.LuaExecQueue.BeginCallSelf(this, "exec", target.WIGTable);
			_executeCommand(target);
		}

		#endregion

	}

}

