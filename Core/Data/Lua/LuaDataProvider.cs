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
using Eluant;
using System.Collections.Generic;
using System.Linq;

namespace WF.Player.Core.Data.Lua
{
    /// <summary>
    /// A Lua implementation of a data provider wrapping a Lua function.
    /// </summary>
	internal class LuaDataProvider : IDataProvider
    {
        #region Members

        protected LuaFunction _luaFunction;
        private SafeLua _luaState;
        private LuaDataFactory _dataFactory;
        private LuaTable _selfTable;

        #endregion

        #region Constructors

        internal LuaDataProvider(
            LuaFunction function, 
            SafeLua luaState, 
            LuaDataFactory dataFactory,
            LuaTable self = null)
        {
            _luaFunction = function;
            _luaState = luaState;
            _dataFactory = dataFactory;
            _selfTable = self;
        } 

        #endregion

        #region Methods
        internal LuaDataContainer FirstContainerOrDefault(params object[] args)
        {
            return ExecuteCore(args).OfType<LuaDataContainer>().FirstOrDefault();
        } 
        #endregion

        #region IDataProvider
        public void Execute(params object[] args)
        {
            ExecuteCore(args);
        }

        IDataContainer IDataProvider.FirstContainerOrDefault(params object[] args)
        {
            return ExecuteCore(args).OfType<IDataContainer>().FirstOrDefault();
        }

        public T FirstOrDefault<T>(params object[] args)
        {
            return ExecuteCore(args).OfType<T>().FirstOrDefault();
        } 
        #endregion


        private IList<object> ExecuteCore(params object[] args)
        {
            // Conforms the parameters: wrappers from the Data layer
            // are converted to their native equivalents.
            // The self table is added if it is specified.
            List<LuaValue> parameters = new List<LuaValue>();
            if (_selfTable != null)
            {
                parameters.Add(_selfTable);
            }
            foreach (var item in args)
            {
                parameters.Add(_dataFactory.GetNativeValueFromValue(item));
            }

            // Runs the provider and gets the list of objects.
            IList<object> retValues;
			try
			{
				retValues = _luaState.SafeCallRaw(_luaFunction, parameters.ToArray());
			}
			catch (Exception e)
			{
				throw new InvalidOperationException("An exception occured while executing the provider.", e); 
			}

            // Converts the different values to types from the data layer.
            List<object> ret = new List<object>();
            foreach (var item in retValues)
            {
                if (item is LuaValue)
                {
                    ret.Add(_dataFactory.GetValueFromNativeValue((LuaValue)item));
                }
                else
                {
                    ret.Add(item);
                }
            }

            // Returns the list.
            return ret;
        }
    }
}
