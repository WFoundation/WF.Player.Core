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

namespace WF.Player.Core
{
	/// <summary>
	/// A thread-safe queue that sequentializes access to a Lua execution environment.
	/// </summary>
	public class LuaExecutionQueue : IDisposable
	{
		#region Fields

		private Lua _luaState;
		private Queue<Action> _jobQueue = new Queue<Action>();
		private Thread _jobThread;
		private bool _isDisposed = false;
		private bool _isSleeping = true;
		private object _syncRoot = new object();
		private AutoResetEvent _jobThreadCanResumeEvent;

		#endregion

		#region Properties

		/// <summary>
		/// Gets a boolean indicating if the calling thread is the same as the underlying
		/// thread of this LuaExecutionQueue.
		/// </summary>
		public bool IsSameThread
		{
			get
			{
				return _jobThread != null && _jobThread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId;
			}
		}

		/// <summary>
		/// Gets a boolean indicating if this LuaExecutionQueue is busy performing jobs.
		/// </summary>
		/// <remarks>If the object is disposed, this returns false.</remarks>
		public bool IsBusy
		{
			get
			{
				lock (_syncRoot)
				{
					return !_isDisposed && !_isSleeping;
				}
			}
		}

		/// <summary>
		/// Gets how many jobs are in queue.
		/// </summary>
		public int QueueCount
		{
			get
			{				
				lock (_syncRoot)
				{
					if (_isDisposed)
					{
						throw new InvalidOperationException("This object is disposed.");
					}

					return _jobQueue.Count;
				}
			}
		}

		private bool IsDisposed
		{
			get
			{
				lock (_syncRoot)
				{
					return _isDisposed;  
				}
			}

			set
			{
				bool isBusyChanged;

				lock (_syncRoot)
				{
					isBusyChanged = IsBusy;
					_isDisposed = value;
					isBusyChanged = isBusyChanged ^ IsBusy ;
				}

				if (isBusyChanged)
				{
					RaiseIsBusyChanged();
				}
			}
		}

		private bool IsSleeping
		{
			get
			{
				lock (_syncRoot)
				{
					return _isSleeping;
				}
			}

			set
			{
				bool isBusyChanged;
				
				lock (_syncRoot)
				{
					isBusyChanged = IsBusy;
					_isSleeping = value;
					isBusyChanged = isBusyChanged ^ IsBusy;
				}

				if (isBusyChanged)
				{
					RaiseIsBusyChanged();
				}
			}
		}

		#endregion

		#region Events

		/// <summary>
		/// Raised when the IsBusy property has changed.
		/// </summary>
		public event EventHandler IsBusyChanged;

		#endregion

		#region Constructors and Destructor
		/// <summary>
		/// Creates a new execution queue for a Lua state.
		/// </summary>
		/// <param name="lua"></param>
		public LuaExecutionQueue(Lua lua)
		{
			_luaState = lua;
		}

		/// <summary>
		/// Disposes this queue and the underlying lua state.
		/// </summary>
		public void Dispose()
		{
			// Marks this object as disposed.
			// Bye bye queue.
			IsDisposed = true;
			lock (_syncRoot)
			{
				_jobQueue.Clear();
			}

			if (_jobThread != null)
			{
				// Bye bye thread: just wakes it up, it will die on its own
				// now that _isDisposed is true.
				_jobThreadCanResumeEvent.Set();

				// Waits for the thread to die and then disposed other related resources.
				_jobThread.Join();
				_jobThreadCanResumeEvent.Dispose();

				// Bye bye.
				_jobThread = null;
				_jobThreadCanResumeEvent = null;
			}

			// Bye bye lua state.
			if (_luaState != null)
			{
				_luaState.Dispose();
				_luaState = null;
			}

			// Requests the GC to not finalize this object (best practice).
			GC.SuppressFinalize(this);
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

		#region Job Queue

		private void AcceptJob(Action job)
		{
			// Sanity Check.
			if (IsDisposed)
			{
				throw new ObjectDisposedException("LuaState", "This instance of LuaExecutionQueue is disposed.");
			}

			// Adds the job to the queue.
			lock (_syncRoot)
			{
				_jobQueue.Enqueue(job);

				System.Diagnostics.Debug.WriteLine("LuaExecutionQueue: Added a job. Current queue: " + _jobQueue.Count);
			}

			// Makes sure the thread is running.
			EnsureThreadRuns();

			// Signals the thread to go on if it is waiting.
			_jobThreadCanResumeEvent.Set();
		}

		private void EnsureThreadRuns()
		{
			// Creates the thread if it does not exist.
			if (_jobThread == null)
			{
				// Creates the thread.
				_jobThread = new Thread(ThreadMain)
				{
					Name = "LuaExecutionQueue_" + _syncRoot.GetHashCode(),
					IsBackground = true
				};

				// Creates the signaling event.
				_jobThreadCanResumeEvent = new AutoResetEvent(true);
			}

			// Starts the thread if it is not alive.
			if (!_jobThread.IsAlive)
			{
				_jobThread.Start();
			}
		}

		#endregion

		#region Job Processing

		private void RunCall(LuaTable obj, string func, object[] parameters)
		{
			// This executes in the job thread.

			// Checks if this is still alive. 
			if (_isDisposed)
				return;

			// Checks if the function still exists.
			LuaFunction lf;
			lock (_luaState)
			{
				lf = obj[func] as LuaFunction; 
			}
			if (lf == null)
				return;

			// Calls the function.
			lock (_luaState)
			{
				lf.Call(parameters); 
			}
		}

		private void RunCall(LuaFunction func, object[] parameters)
		{
			// This executes in the job thread.

			// Checks if this is still alive. 
			if (_isDisposed)
				return;

			// Calls the function.
			lock (_luaState)
			{
				func.Call(parameters); 
			}
		}

		private void ThreadMain()
		{			
			while (!_isDisposed)
			{
				// The thread is waking up.
				IsSleeping = false;

				Action nextJob = null;
				int jobsLeftToDo = 0;
				
				// Gets the next job, if any.
				lock (_syncRoot)
				{					
					// If the instance just got disposed, return immediately.
					if (_isDisposed)
					{
						return;
					}

					jobsLeftToDo = _jobQueue.Count;
					if (jobsLeftToDo > 0)
					{
						nextJob = _jobQueue.Dequeue();
					}

					System.Diagnostics.Debug.WriteLine("LuaExecutionQueue: About to process job. Queue after this one: " + jobsLeftToDo);
				}

				// If there is a job to execute, do it.
				if (nextJob != null)
				{
					nextJob();
					jobsLeftToDo--;
				}

				// If there are more jobs to do, let's go!
				if (jobsLeftToDo > 0)
				{
					// Sleeps a litte bit before to free UI thread memory.
					Thread.Sleep(50);

					// Let's do the next job!
					continue;
				}

				// Goes to sleep and waits for a signal.
				System.Diagnostics.Debug.WriteLine("LuaExecutionQueue: No more jobs, going to sleep.");
				IsSleeping = true;
				_jobThreadCanResumeEvent.WaitOne();
				System.Diagnostics.Debug.WriteLine("LuaExecutionQueue: Got signal, waking up.");
			}
		}

		#endregion

		#region Event Raisers

		private void RaiseIsBusyChanged()
		{
			if (IsBusyChanged != null)
			{
				IsBusyChanged(this, EventArgs.Empty);
			}
		}

		#endregion
	}
}