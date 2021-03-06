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

namespace WF.Player.Core.Data.Lua
{
    /// <summary>
    /// Provides extensions for Lua-related data structures.
    /// </summary>
    internal static class LuaDataExtensions
    {
		internal static void CallSelf(this WherigoObject wo, string funcName, params object[] parameters)
        {
            // Gets the self-provider and calls it.
            ((LuaDataContainer)wo.DataContainer).CallSelf(funcName, parameters);
        }
    }
}
