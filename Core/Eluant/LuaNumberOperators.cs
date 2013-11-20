using System;

namespace WF.Player.Core.Lua
{
    public partial class LuaNumber
    {
        public static double? operator+(LuaNumber a, double? b)
        {
            return (double?)a + b;
        }
        
        public static double? operator-(LuaNumber a, double? b)
        {
            return (double?)a - b;
        }
        
        public static double? operator*(LuaNumber a, double? b)
        {
            return (double?)a * b;
        }
        
        public static double? operator/(LuaNumber a, double? b)
        {
            return (double?)a / b;
        }
        
        public static double? operator%(LuaNumber a, double? b)
        {
            return (double?)a % b;
        }
        
        public static bool operator==(LuaNumber a, double? b)
        {
            return (double?)a == b;
        }
        
        public static bool operator!=(LuaNumber a, double? b)
        {
            return (double?)a != b;
        }
        
        public static bool operator<=(LuaNumber a, double? b)
        {
            return (double?)a <= b;
        }
        
        public static bool operator>=(LuaNumber a, double? b)
        {
            return (double?)a >= b;
        }
        
        public static bool operator<(LuaNumber a, double? b)
        {
            return (double?)a < b;
        }
        
        public static bool operator>(LuaNumber a, double? b)
        {
            return (double?)a > b;
        }
        
    }
}
