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
using System.Threading;
using System.Collections.Generic;

namespace WF.Player.Core.Utils.Threading
{
	/// <summary>
	/// An base class for a thread-safe, customizable queue of jobs.
	/// </summary>
	internal abstract class JobQueue : IDisposable
	{
		#region Members

		private Queue<Action> jobQueue = new Queue<Action>();
		private Thread jobThread;
		private bool isDisposed = false;
		private bool isSleeping = true;
		private bool isActive = true;
		private bool isAutoProcessing = true;
		private TimeSpan delayBetweenJobs = TimeSpan.FromMilliseconds(50);
		private AutoResetEvent jobThreadCanResumeEvent;
		private object syncRoot = new object();

		#endregion

		#region Properties

		/// <summary>
		/// Gets a boolean indicating if the calling thread is the same as the underlying
		/// thread of this job queue.
		/// </summary>
		public bool IsSameThread
		{
			get
			{
				return jobThread != null && jobThread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId;
			}
		}

		/// <summary>
		/// Gets a boolean indicating if this job queue is busy performing jobs.
		/// </summary>
		/// <remarks>If the object is disposed, this returns false.</remarks>
		public bool IsBusy
		{
			get
			{
				lock (syncRoot)
				{
					return !isDisposed && !isSleeping;
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
				lock (syncRoot)
				{
					if (isDisposed)
					{
						throw new InvalidOperationException("This object is disposed.");
					}

					return jobQueue.Count;
				}
			}
		}

		/// <summary>
		/// Gets or sets a boolean indicating if the jobs in queue are being 
		/// processed or not.
		/// </summary>
		/// <value>Default is true.</value>
		protected bool IsActive
		{
			get
			{
				lock (this.syncRoot)
				{
					return this.isActive;
				}
			}

			set
			{
				bool valueChanged = false;

				lock (this.syncRoot)
				{
					if (isActive != value)
					{
						valueChanged = true;
						isActive = value;
					}
				}

				if (valueChanged)
				{
					OnIsActiveChanged(value);
				}
			}
		}

		/// <summary>
		/// Gets or sets a boolean indicating if the queue should keep on being
		/// processed once a job is completed.
		/// </summary>
		/// <value>Default is true.</value>
		protected bool ContinuesOnCompletion
		{
			get
			{
				lock (this.syncRoot)
				{
					return this.isAutoProcessing;
				}
			}

			set
			{
				bool valueChanged = false;

				lock (this.syncRoot)
				{
					if (isAutoProcessing != value)
					{
						valueChanged = true;
						isAutoProcessing = value;
					}
				}

				if (valueChanged)
				{
					OnContinuesOnCompletionChanged(value);
				}
			}
		}

		/// <summary>
		/// Gets or sets how much time should be waited between two jobs. This is
		/// only used if <code>ContinuesOnCompletion</code> is <code>true</code>.
		/// </summary>
		/// <value>Default is 50ms.</value>
		protected TimeSpan DelayBetweenJobs
		{
			get
			{
				lock (this.syncRoot)
				{
					return this.delayBetweenJobs;
				}
			}

			set
			{
				lock (this.syncRoot)
				{
					this.delayBetweenJobs = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets if this instance is disposed.
		/// </summary>
		protected bool IsDisposed
		{
			get
			{
				lock (syncRoot)
				{
					return isDisposed;
				}
			}

			set
			{
				bool isBusyChanged;

				lock (syncRoot)
				{
					isBusyChanged = IsBusy;
					isDisposed = value;
					isBusyChanged = isBusyChanged ^ IsBusy;
				}

				if (isBusyChanged)
				{
					RaiseIsBusyChanged();
				}
			}
		}

		/// <summary>
		/// Gets or sets if the job thread is sleeping.
		/// </summary>
		protected bool IsSleeping
		{
			get
			{
				lock (syncRoot)
				{
					return isSleeping;
				}
			}

			set
			{
				bool isBusyChanged;

				lock (syncRoot)
				{
					isBusyChanged = IsBusy;
					isSleeping = value;
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
		/// Disposes this JobQueue and all its resources.
		/// </summary>
		public void Dispose()
		{
			// Marks this object as disposed.
			// Bye bye queue.
			IsDisposed = true;
			lock (syncRoot)
			{
				jobQueue.Clear();
			}

			if (jobThread != null)
			{
				// Bye bye thread: just wakes it up, it will die on its own
				// now that _isDisposed is true.
				jobThreadCanResumeEvent.Set();

				// Waits for the thread to die and then disposed other related resources.
				jobThread.Join();
				jobThreadCanResumeEvent.Dispose();

				// Bye bye.
				jobThread = null;
				jobThreadCanResumeEvent = null;
			}

			// Bye bye other things?
			DisposeOverride();

			// Requests the GC to not finalize this object (best practice).
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Overriden my children classes to perform their own disposings.
		/// </summary>
		protected virtual void DisposeOverride()
		{
			
		} 
		#endregion

		#region Job Queue

		/// <summary>
		/// Adds a job to the current queue and starts the processing thread if it is not running.
		/// </summary>
		/// <param name="job"></param>
		/// <param name="wakeUpThread">If true, wakes up the thread if it is sleeping.</param>
		protected void AcceptJob(Action job, bool wakeUpThread = true)
		{
			// Sanity Check.
			if (IsDisposed)
			{
				throw new InvalidOperationException("This object is disposed.");
			}

			// Adds the job to the queue.
			lock (this.syncRoot)
			{
				this.jobQueue.Enqueue(job);
			}

			// Makes sure the thread is running.
			EnsureThreadRuns();

			// Signals the thread to go on if it is waiting.
			if (wakeUpThread)
			{
				this.jobThreadCanResumeEvent.Set(); 
			}
		}

		/// <summary>
		/// Makes sure that the job thread is running, and starts it if it is not.
		/// </summary>
		protected void EnsureThreadRuns()
		{
			// Sanity Check.
			if (IsDisposed)
			{
				throw new InvalidOperationException("This object is disposed.");
			}
			
			// Creates the thread if it does not exist.
			if (this.jobThread == null)
			{
				// Creates the thread.
				this.jobThread = new Thread(ThreadMain)
				{
					Name = this.GetType().Name + "_" + this.syncRoot.GetHashCode(),
					IsBackground = true
				};

				// Creates the signaling event.
				this.jobThreadCanResumeEvent = new AutoResetEvent(true);
			}

			// Starts the thread if it is not alive.
			if (!this.jobThread.IsAlive)
			{
				this.jobThread.Start();
			}
		}

		private void OnContinuesOnCompletionChanged(bool newValue)
		{
			if (newValue && this.jobThread != null && this.jobThread.IsAlive)
			{
				// Wakes the thread up, there may be some new things to do.
				this.jobThreadCanResumeEvent.Set();
			}
		}

		private void OnIsActiveChanged(bool value)
		{
			if (value && this.jobThread != null && this.jobThread.IsAlive)
			{
				// Wakes the thread up, there may be some new things to do.
				this.jobThreadCanResumeEvent.Set();
			}
		}

		#endregion

		#region Job Processing

		private void ThreadMain()
		{
			while (!this.isDisposed)
			{
				if (IsActive)
				{
					// The thread is waking up.
					IsSleeping = false;

					Action nextJob = null;
					int jobsLeftToDo = 0;

					// Gets the next job, if any.
					lock (this.syncRoot)
					{
						// If the instance just got disposed, return immediately.
						if (this.isDisposed)
						{
							return;
						}

						jobsLeftToDo = this.jobQueue.Count;
						if (jobsLeftToDo > 0)
						{
							nextJob = this.jobQueue.Dequeue();
						}
					}

					// If there is a job to execute, do it.
					if (nextJob != null)
					{
						nextJob();
						jobsLeftToDo--;
					}

					// If there are more jobs to do and we should do them, let's go!
					if (jobsLeftToDo > 0 && ContinuesOnCompletion)
					{
						// Sleeps a litte bit before to free UI thread cpu.
						Thread.Sleep(DelayBetweenJobs);

						// Let's do the next job!
						continue;
					}

					// Goes to sleep and waits for a signal.
					IsSleeping = true;

				}

				// Zzzz.
				this.jobThreadCanResumeEvent.WaitOne();
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
