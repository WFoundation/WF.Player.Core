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
using System.Threading;

namespace WF.Player.Core.Utils.Threading
{
	/// <summary>
	/// A thread-safe queue that sequentializes access to a Lua execution environment.
	/// </summary>
	internal class LuaExecutionQueue : JobQueue
	{
		#region Members

		private SafeLua luaState;

		#endregion

		#region Constructors and Destructor
		/// <summary>
		/// Creates a new execution queue for a Lua state.
		/// </summary>
		/// <param name="lua"></param>
		public LuaExecutionQueue(SafeLua lua)
		{
			luaState = lua;

			// JobQueue configuration.
			IsActive = true;
			ContinuesOnCompletion = true;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Executes asynchronously a call to a Lua function on the thread this LuaExecutionQueue is associated with.
		/// </summary>
		/// <remarks>This method returns once the call job is queued.</remarks>
		/// <param name="obj">LuaTable that contains the function.</param>
		/// <param name="func">Field name in <paramref name="obj"/> that corresponds to the function to call.</param>
		/// <param name="parameters">Optional parameters to pass to the function.</param>
		public void BeginCall(LuaTable obj, string func, params object[] parameters)
		{
			// Conforms the parameters and enqueues a job.
			AcceptJob(GetJob(obj, func, ConformParameters(parameters)));
		}

		/// <summary>
		/// Executes asynchronously a call to a Lua function on the thread this LuaExecutionQueue is associated with.
		/// </summary>
		/// <remarks>This method returns once the call job is queued.</remarks>
		/// <param name="obj">Table entity that contains the function.</param>
		/// <param name="func">Field name in the underlying LuaTable of <paramref name="obj"/> that corresponds to the 
		/// function to call.</param>
		/// <param name="parameters">Optional parameters to pass to the function.</param>
		public void BeginCall(Table obj, string func, params object[] parameters)
		{
			// Conforms the parameters and enqueues a job.
			AcceptJob(GetJob(obj.WIGTable, func, ConformParameters(parameters)));
		}

		/// <summary>
		/// Executes asynchronously a call to a Lua function on the thread this LuaExecutionQueue is associated with.
		/// </summary>
		/// <remarks>This method returns once the call job is queued.</remarks>
		/// <param name="func">LuaFunction to call.</param>
		/// <param name="parameters">Optional parameters to pass to the function.</param>
		public void BeginCall(LuaFunction func, params object[] parameters)
		{
			// Conforms the parameters and enqueues a job.
			AcceptJob(GetJob(func, ConformParameters(parameters)));
		}

		/// <summary>
		/// Executes asynchronously a call to a Lua self-function on the thread this LuaExecutionQueue is associated with.
		/// </summary>
		/// <remarks>This method returns once the call job is queued.</remarks>
		/// <param name="obj">LuaTable that contains the self-function.</param>
		/// <param name="func">Field name in <paramref name="obj"/> that corresponds to the function to call.</param>
		/// <param name="parameters">Optional parameters to pass to the function. <paramref name="obj"/> is automatically
		/// added as first parameter.</param>
		public void BeginCallSelf(LuaTable obj, string func, params object[] parameters)
		{
			// Conforms the parameters and enqueues a job.
			AcceptJob(GetJob(obj, func, ConformParameters(parameters, obj)));
		}

		/// <summary>
		/// Executes asynchronously a call to a Lua self-function on the thread this LuaExecutionQueue is associated with.
		/// </summary>
		/// <remarks>This method returns once the call job is queued.</remarks>
		/// <param name="obj">Table entity that contains the self-function.</param>
		/// <param name="func">Field name in underlying LuaTable of <paramref name="obj"/> that corresponds to the 
		/// self-function to call.</param>
		/// <param name="parameters">Optional parameters to pass to the function. <paramref name="obj"/> is automatically
		/// added as first parameter.</param>
		public void BeginCallSelf(Table obj, string func, params object[] parameters)
		{
			// Conforms the parameters and enqueues a job.
			AcceptJob(GetJob(obj.WIGTable, func, ConformParameters(parameters, obj.WIGTable)));
		}

		/// <summary>
		/// Blocks the caller thread until this LuaExecutionQueue has completed all the jobs 
		/// in the queue and goes to sleep.
		/// </summary>
		public void WaitEmpty()
		{
			// Sanity check.
			if (IsSameThread)
			{
				throw new InvalidOperationException("Cannot use WaitEmpty() from the same LuaExecutionQueue thread.");
			}

			// Creates a reset event for blocking the caller thread.
			using (ManualResetEvent re = new ManualResetEvent(false))
			{
				// Defines an event handler for IsBusyChanged.
				// This will be executed in the lua execution queue.
				EventHandler onBusyChanged = new EventHandler((o, e) =>
				{
					// Wakes the thread.
					LuaExecutionQueue leq = (LuaExecutionQueue)o;
					if (!leq.IsBusy && leq.QueueCount == 0)
					{
						// Time to wake the thread up!
						re.Set();
					}
				});

				// Adds the event handler.
				this.IsBusyChanged += onBusyChanged;

				// Don't wait if the thread is not busy.
				if (IsBusy || QueueCount > 0)
				{
					// Waits!
					re.WaitOne();
				}

				// Once we wake up: removes the event handler.
				this.IsBusyChanged -= onBusyChanged; 
			}
		}
		
		/// <summary>
		/// Executes asynchronously an action.
		/// </summary>
		/// <remarks>
		/// Beware of concurrency. Callers should ensure that the action is not going to perform
		/// cross-thread operations, especially on the Lua state that is used by this instance.
		/// </remarks>
		/// <param name="action">Action to execute.</param>
		internal void BeginAction(Action action)
		{
			AcceptJob(action);
		}

		#endregion

		#region Job Creation

		private Action GetJob(LuaTable obj, string func, object[] parameters)
		{
			return new Action(() => RunCall(obj, func, parameters));
		}

		private Action GetJob(LuaFunction func, object[] parameters)
		{
			return new Action(() => RunCall(func, parameters));
		}

		private object[] ConformParameters(object[] parameters)
		{
			// Null parameters are replaced with an empty array.
			return parameters ?? new object[] { };
		}

		private object[] ConformParameters(object[] parameters, object firstParam)
		{
			// Conforms the parameters and then concats the first param.
			return ConformParameters(parameters).ConcatBefore(firstParam);
		}

		#endregion

		#region Job Processing

		private void RunCall(LuaTable obj, string func, object[] parameters)
		{
			// This executes in the job thread.

			// Checks if this is still alive. 
			if (IsDisposed)
				return;

			// Checks if the function still exists.
			LuaFunction lf = luaState.SafeGetField<LuaFunction>(obj, func);

			if (lf == null)
				return;

			// Calls the function.
			luaState.SafeCallRaw(lf, parameters);
		}

		private void RunCall(LuaFunction func, object[] parameters)
		{
			// This executes in the job thread.

			// Checks if this is still alive. 
			if (IsDisposed)
				return;

			// Calls the function.
			luaState.SafeCallRaw(func, parameters);
		}

		#endregion
	}
}