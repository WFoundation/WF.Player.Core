using System;

namespace WF.Player.Core.Lua
{
    public class LuaException : Exception
    {
        public LuaException(string message) : base(message) { }

        public LuaException(string message, Exception inner) : base(message, inner) { }
    }
}

