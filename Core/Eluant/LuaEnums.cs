//
// WF.Player.Core - A Wherigo Player Core for different platforms.
// Copyright (C) 2012-2013  Dirk Weltz <web@weltz-online.de>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
//
using System;

namespace WF.Player.Core.Lua
{
	public enum LuaType : int
	{
		None = -1,

		Nil = 0,
		Boolean = 1,
		LightUserdata = 2,
		Number = 3,
		String = 4,
		Table = 5,
		Function = 6,
		Userdata = 7,
		Thread = 8,
	}

	public enum LuaGcOperation : int
	{
		Stop = 0,
		Restart = 1,
		Collect = 2,
		Count = 3,
		Countb = 4,
		Step = 5,
		SetPause = 6,
		SetStepMul = 7,
	}


	/// <summary>
	/// Enumeration of basic lua globals.
	/// </summary>
	public enum LuaEnums : int
	{
		/// <summary>
		/// Option for multiple returns in `lua_pcall' and `lua_call'
		/// </summary>
		MultiRet        = -1,

		/// <summary>
		/// Everything is OK.
		/// </summary>
		Ok              = 0,

		/// <summary>
		/// Thread status, Ok or Yield
		/// </summary>
		Yield           = 1,

		/// <summary>
		/// A Runtime error.
		/// </summary>
		ErrorRun        = 2,

		/// <summary>
		/// A syntax error.
		/// </summary>
		ErrorSyntax     = 3,

		/// <summary>
		/// A memory allocation error. For such errors, Lua does not call the error handler function. 
		/// </summary>
		ErrorMemory     = 4,

		/// <summary>
		/// An error in the error handling function.
		/// </summary>
		ErrorError      = 5,

		/// <summary>
		/// An extra error for file load errors when using luaL_loadfile.
		/// </summary>
		ErrorFile       = 6
	}
}

