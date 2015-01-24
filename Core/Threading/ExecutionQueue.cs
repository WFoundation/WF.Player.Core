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
using System.Threading;
using WF.Player.Core.Data;
using WF.Player.Core.Data.Lua;

namespace WF.Player.Core.Threading
{
	/// <summary>
	/// A thread-safe queue that sequentializes access to an execution environment.
	/// </summary>
	internal class ExecutionQueue : JobQueue
	{
		#region Delegates

		/// <summary>
		/// An action that can be scheduled to run when a job has failed.
		/// </summary>
		/// <param name="ex">The exception that made the job fail.</param>
		public delegate void FallbackAction(Exception ex);

		#endregion
		
		#region Fields

        private List<ManualResetEvent> _waitEmptyResetEvents = new List<ManualResetEvent>();
        private object _syncRoot = new object();

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets if this execution queue is running.
		/// </summary>
		public bool IsRunning
		{
			get
			{
				return base.IsActive;
			}

			set
			{
				base.IsActive = value;
			}
		}

		/// <summary>
		/// Gets or sets the default fallback action, to execute when
		/// a job fails because of an exception.
		/// </summary>
		public FallbackAction DefaultFallbackAction { get; set; }

		#endregion

		#region Constructors and Destructor
		/// <summary>
		/// Creates a new execution queue that is initially active and
        /// continues on completion.
		/// </summary>
		public ExecutionQueue()
		{
			// JobQueue configuration.
			IsActive = true;
			ContinuesOnCompletion = true;
		}

        protected override void DisposeOverride()
        {
            // Copies and unregisters the active reset events.
            List<ManualResetEvent> activeResetEvents;
            lock (_syncRoot)
            {
                activeResetEvents = new List<ManualResetEvent>(_waitEmptyResetEvents);
            }

            // Wakes up and disposes all active reset events.
            foreach (ManualResetEvent re in activeResetEvents)
            {
                DisposeWaitEmptyResetEvent(re);
            }

            // Deletes managed resources.
            _waitEmptyResetEvents = null;
            _syncRoot = null;
        }
		#endregion

		#region Begin Actions

		/// <summary>
		/// Executes asynchronously a call to a Lua function on the thread this ExecutionQueue is associated with.
		/// </summary>
		/// <remarks>This method returns once the call job is queued.</remarks>
		/// <param name="obj">IDataContainer that contains the function.</param>
		/// <param name="func">Field name in <paramref name="obj"/> that corresponds to the function to call.</param>
		/// <param name="parameters">Optional parameters to pass to the function.</param>
		public void BeginCall(IDataContainer obj, string func, params object[] parameters)
		{
			// Conforms the parameters and enqueues a job.
			AcceptJob(GetJob(obj, func, ConformParameters(parameters)));
		}

		/// <summary>
		/// Executes asynchronously a call to a Lua function on the thread this ExecutionQueue is associated with.
		/// </summary>
		/// <remarks>This method returns once the call job is queued.</remarks>
		/// <param name="obj">Table entity that contains the function.</param>
		/// <param name="func">Field name in the underlying IDataContainer of <paramref name="obj"/> that corresponds to the 
		/// function to call.</param>
		/// <param name="parameters">Optional parameters to pass to the function.</param>
		public void BeginCall(WherigoObject obj, string func, params object[] parameters)
		{
			// Conforms the parameters and enqueues a job.
			AcceptJob(GetJob(obj.DataContainer, func, ConformParameters(parameters)));
		}

        /// <summary>
        /// Executes asynchronously a call to a Lua function on the thread this ExecutionQueue is associated with.
        /// </summary>
        /// <remarks>This method returns once the call job is queued.</remarks>
        /// <param name="func">Function to call.</param>
        /// <param name="parameters">Optional parameters to pass to the function.</param>
        public void BeginCall(IDataProvider func, params object[] parameters)
        {
            // Conforms the parameters and enqueues a job.
            AcceptJob(GetJob(func, ConformParameters(parameters)));
        }

		/// <summary>
		/// Executes asynchronously a call to a Lua self-function on the thread this ExecutionQueue is associated with.
		/// </summary>
		/// <remarks>This method returns once the call job is queued.</remarks>
		/// <param name="obj">IDataContainer that contains the self-function.</param>
		/// <param name="func">Field name in <paramref name="obj"/> that corresponds to the function to call.</param>
		/// <param name="parameters">Optional parameters to pass to the function. <paramref name="obj"/> is automatically
		/// added as first parameter.</param>
		public void BeginCallSelf(IDataContainer obj, string func, params object[] parameters)
		{
			// Conforms the parameters and enqueues a job.
			AcceptJob(GetJob(obj, func, ConformParameters(parameters), true));
		}

		/// <summary>
		/// Executes asynchronously a call to a Lua self-function on the thread this ExecutionQueue is associated with.
		/// </summary>
		/// <remarks>This method returns once the call job is queued.</remarks>
		/// <param name="obj">IDataContainer that contains the self-function.</param>
		/// <param name="func">Field name in <paramref name="obj"/> that corresponds to the function to call.</param>
		/// <param name="fallback">Action that is executed if an exception occurs during the
		/// execution of the job. If this parameter is null, any exception occuring will be
		/// rethrown.</param>
		/// <param name="parameters">Optional parameters to pass to the function. <paramref name="obj"/> is automatically
		/// added as first parameter.</param>
		public void BeginCallSelf(IDataContainer obj, string func, FallbackAction fallback, params object[] parameters)
		{
			// Conforms the parameters and enqueues a job.
			AcceptJob(GetJob(obj, func, ConformParameters(parameters), true, fallback));
		}

		/// <summary>
		/// Executes asynchronously a call to a Lua self-function on the thread this ExecutionQueue is associated with.
		/// </summary>
		/// <remarks>This method returns once the call job is queued.</remarks>
		/// <param name="obj">Entity that contains the self-function.</param>
		/// <param name="func">Field name in underlying LuaTable of <paramref name="obj"/> that corresponds to the 
		/// self-function to call.</param>
		/// <param name="parameters">Optional parameters to pass to the function. <paramref name="obj"/> is automatically
		/// added as first parameter.</param>
		public void BeginCallSelf(WherigoObject obj, string func, params object[] parameters)
		{
			// Conforms the parameters and enqueues a job.
            IDataContainer cont = obj.DataContainer;
            AcceptJob(GetJob(cont, func, ConformParameters(parameters), true));
		}

		/// <summary>
		/// Executes asynchronously a call to a Lua self-function on the thread this ExecutionQueue is associated with,
		/// after making sure that calls of the same function that are queued but yet to execute
		/// are being removed from the queue.
		/// </summary>
		/// <remarks>This method returns once the call job is queued.</remarks>
		/// <param name="obj">Entity that contains the self-function.</param>
		/// <param name="func">Field name in underlying LuaTable of <paramref name="obj"/> that corresponds to the 
		/// self-function to call. Cannot be null.</param>
		/// <param name="parameters">Optional parameters to pass to the function. <paramref name="obj"/> is automatically
		/// added as first parameter.</param>
		public void BeginCallSelfUnique(WherigoObject obj, string func, params object[] parameters)
		{
			if (func == null)
			{
				throw new ArgumentNullException("func");
			}
			
			// Removes all job for this function.
			RemoveJobsWithTag(func);

			// Conforms the parameters and enqueues a job.
			IDataContainer cont = obj.DataContainer;
			AcceptJob(GetJob(cont, func, ConformParameters(parameters), true), tag: func);
		}

        /// <summary>
        /// Executes asynchronously an action.
        /// </summary>
        /// <remarks>
        /// Beware of concurrency. Callers should ensure that the action is not going to perform
        /// cross-thread operations.
        /// </remarks>
        /// <param name="action">Action to execute.</param>
        internal void BeginAction(Action action)
        {
            AcceptJob(action);
        }

        #endregion

        #region Wait for Completion

        /// <summary>
        /// Blocks the caller thread until this ExecutionQueue has completed all the jobs 
        /// in the queue and goes to sleep.
        /// </summary>
        public void WaitEmpty()
        {
            // Sanity check.
            if (IsSameThread)
            {
                throw new InvalidOperationException("Cannot use WaitEmpty() from the same thread as this ExecutionQueue.");
            }

            // Creates a reset event for blocking the caller thread.
            ManualResetEvent re = CreateWaitEmptyResetEvent();

            // Defines an event handler for IsBusyChanged.
            EventHandler onBusyChanged = new EventHandler((o, e) =>
            {
                // This is executed in the execution queue thread.

                // Checks if the object is still registered as active and
                // not disposed. If not, returns.
                if (IsWaitEmptyResetEventInvalid(re))
                {
                    return;
                }

                // Wakes the thread.
                ExecutionQueue leq = (ExecutionQueue)o;
                if (!leq.IsBusy && leq.QueueCount == 0)
                {
                    // Time to wake the thread up!
					try
					{
						re.Set();
					}
					catch (ObjectDisposedException)
					{
						// This is probably a race-condition, where DisposeWaitEmptyResetEvent
						// has been called after IsWaitEmptyResetEventInvalid returned false.
						// There is nothing to do because DisposeWaitEmptyResetEvent() has already
						// woken the thread up.
					}
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

            // Makes sure the event is disposed and unregistered.
            DisposeWaitEmptyResetEvent(re);
        }

        private void DisposeWaitEmptyResetEvent(ManualResetEvent re)
        {
            // Unregisters the event from the list.
            lock (_syncRoot)
            {
                _waitEmptyResetEvents.Remove(re);
            }

            // Wakes up and disposes the event.
            re.Set();
            re.Dispose();

            // Reregisters the event for finalization.
            GC.ReRegisterForFinalize(re);
        }

        private bool IsWaitEmptyResetEventInvalid(ManualResetEvent re)
        {
			if (IsDisposed)
			{
				return true;
			}
			
			bool isInList;

            lock (_syncRoot)
            {
                isInList = _waitEmptyResetEvents.Contains(re);
            }

            return !isInList;
        }

        private ManualResetEvent CreateWaitEmptyResetEvent()
        {
            // Creates a reset event that is unset.
            ManualResetEvent ret = new ManualResetEvent(false);

            // Prevents the reset event from being finalized by the GC.
            GC.SuppressFinalize(ret);

            // Registers the reset event.
            lock (_syncRoot)
            {
                _waitEmptyResetEvents.Add(ret);
            }

            return ret;
        }



        #endregion
		
		#region Job Creation

		private Action GetJob(IDataContainer obj, string func, object[] parameters, bool isSelf, FallbackAction fallbackAction)
		{
			return new Action(() => RunCall(obj, func, parameters, isSelf, fallbackAction));
		}

        private Action GetJob(IDataContainer obj, string func, object[] parameters, bool isSelf)
        {
            return new Action(() => RunCall(obj, func, parameters, isSelf, null));
        }

		private Action GetJob(IDataContainer obj, string func, object[] parameters)
		{
			return new Action(() => RunCall(obj, func, parameters));
		}

        private Action GetJob(IDataProvider func, object[] parameters)
        {
            return new Action(() => RunCall(func, parameters));
        }

		private object[] ConformParameters(object[] parameters)
		{
			// Null parameters are replaced with an empty array.
			return parameters ?? new object[] { };
		}

		#endregion

		#region Job Processing

        private void RunCall(IDataContainer obj, string func, object[] parameters, bool isSelf, FallbackAction fallback)
        {
            // This executes in the job thread.

            // Checks if this is still alive. 
            if (IsDisposed)
                return;

            // Checks if the function still exists.
            LuaDataContainer dc = obj as LuaDataContainer;
            if (dc == null)
                return;

            IDataProvider lf = dc.GetProvider(func, isSelf);            

            if (lf == null)
                return;

            // Calls the function.
			try
			{
				lf.Execute(parameters);
			}
			catch (InvalidOperationException ex)
			{
				// Last chance fallback action, if any.
				if (fallback != null)
				{
					fallback(ex);
				}
				else if (DefaultFallbackAction != null)
				{
					DefaultFallbackAction(ex);
				}
				else
				{
					// No fallback action: let's rethrow this.
					throw;
				}
			}
			catch (Exception)
			{
				// Other exceptions than InvalidOperationException are
				// immediately rethrown because they are, indeed, unexpected.
				throw;
			}
        }

		private void RunCall(IDataContainer obj, string func, object[] parameters)
		{
			// This executes in the job thread.

			// Checks if this is still alive. 
			if (IsDisposed)
				return;

			// Checks if the function still exists.
            IDataProvider lf = obj.GetProvider(func);

			if (lf == null)
				return;

			// Calls the function.
			try
			{
				lf.Execute(parameters);
			}
			catch (Exception ex)
			{
				// Fallback action, if any.
				if (DefaultFallbackAction != null)
				{
					DefaultFallbackAction(ex);
				}
				else
				{
					// No fallback action: let's rethrow this.
					throw;
				}
			}
		}

        private void RunCall(IDataProvider func, object[] parameters)
        {
            // This executes in the job thread.

            // Checks if this is still alive. 
            if (IsDisposed)
                return;

            // Calls the function.
			try
			{
				func.Execute(parameters);
			}
			catch (Exception ex)
			{
				// Fallback action, if any.
				if (DefaultFallbackAction != null)
				{
					DefaultFallbackAction(ex);
				}
				else
				{
					// No fallback action: let's rethrow this.
					throw;
				}
			}
        }

		#endregion
	}
}