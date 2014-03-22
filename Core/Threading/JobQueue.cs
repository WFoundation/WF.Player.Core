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
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;

namespace WF.Player.Core.Threading
{
	/// <summary>
	/// A base class for a thread-safe, customizable queue of jobs.
	/// </summary>
	public abstract class JobQueue : IDisposable
	{
        #region Nested Classes

		private class FilterQueue
		{
			#region Fields
			private Queue<Job> _queue;
			private Dictionary<string, int> _ignoredTags;
			private int _dequeuesSinceLastIgnoreCheck = 0;
			#endregion

			#region Properties
			/// <summary>
			/// Gets how many elements this queue has.
			/// </summary>
			public int Count
			{
				get
				{
					return _queue.Count;
				}
			} 
			#endregion

			#region Constructors
			public FilterQueue()
			{
				_queue = new Queue<Job>();
				_ignoredTags = new Dictionary<string, int>();
			} 
			#endregion

			/// <summary>
			/// Marks a tag to be ignored until the current element at
			/// the end of the queue is dequeued.
			/// </summary>
			/// <remarks>
			/// If elements are added to the queue before the current
			/// element at the end of queue is reached, the tag stops 
			/// to be ignored anyway when this last element is dequeued.
			/// 
			/// If this queue is empty at the time of this call, this
			/// method does nothing.
			/// </remarks>
			/// <param name="tag">Non-null tag.</param>
			public void IgnoreTagUntilCurrentEndOfQueue(string tag)
			{
				// Empty queue: do nothing.
				int count = _queue.Count;
				if (count == 0)
				{
					return;
				}

				// Marks the tag to be ignored.
				_ignoredTags[tag] = count;
			}

			/// <summary>
			/// Adds a job to the end of the queue.
			/// </summary>
			/// <param name="job"></param>
			public void Enqueue(Job job)
			{
				_queue.Enqueue(job);
			}

			/// <summary>
			/// Removes and returns the first job that is not marked
			/// to be ignored, starting at the job at the beginning of
			/// the queue.
			/// </summary>
			/// <returns>The first non-ignored job or null if the queue does not
			/// contain any non-ignored job.</returns>
			public Job Dequeue()
			{
				// Immediate return if the queue is empty or there are
				// no filters.
				if (_queue.Count == 0)
				{
					return null;
				}
				else if (_ignoredTags.Count == 0)
				{
					return _queue.Dequeue();
				}
				
				// Dequeues until a non-ignored job is found.
				while (_queue.Count > 0)
				{
					// Gets the next job.
					Job job = _queue.Dequeue();
					_dequeuesSinceLastIgnoreCheck++;

					// Immediate return if it is an untagged job.
					if (job.Tag == null)
					{
						return job;
					}

					// If it is a tagged job, matches it against all
					// ignore rules.
					int ignoreElementsCount;
					if (_ignoredTags.TryGetValue(job.Tag, out ignoreElementsCount))
					{
						// The tag has an ignore rule.

						// Some elements have been already dequeued: ignoreElementsCount
						// needs to be decremented.
						ignoreElementsCount -= _dequeuesSinceLastIgnoreCheck;
						_dequeuesSinceLastIgnoreCheck = 0;

						// Is the rule still valid?
						// NO -> removes the rule and returns the element.
						// YES -> updates the rule count and continues.
						if (ignoreElementsCount < 0)
						{
							// Removes the rule (it is not valid anymore).
							_ignoredTags.Remove(job.Tag);

							// Returns the element (it is not ignored anymore).
							return job;
						}
						else
						{
							// Updates the rule count.
							_ignoredTags[job.Tag] = ignoreElementsCount;

							// Continue the loop: we need to check
							// for another element.
							Debug.WriteLine("JobQueue: Skipped job marked for ignore: " + job.Tag);
						}
					}
				}

				// No element has been found: returns null.
				return null;
			}

			/// <summary>
			/// Removes all jobs from the queue.
			/// </summary>
			/// <remarks>
			/// All tags that are marked to be ignored are unmarked.
			/// </remarks>
			public void Clear()
			{
				_queue.Clear();
				_ignoredTags.Clear();
			}
		}

        /// <summary>
        /// A job to execute in this queue.
        /// </summary>
        private class Job
        {
#if DEBUG
            /// <summary>
            /// Gets or sets the stack trace that led to this job being
            /// accepted by the parent JobQueue.
            /// </summary>
            public StackTrace StackWhenAccepted { get; set; } 
#endif

            /// <summary>
            /// Gets or sets the action to execute when running this job.
            /// </summary>
            public Action Action { get; set; }

			/// <summary>
			/// Gets or sets the tag for this job.
			/// </summary>
			public string Tag { get; set; }
        }

        #endregion
        
        #region Fields

		private FilterQueue _jobQueue = new FilterQueue();
		private Thread _jobThread;
		private bool _isDisposed = false;
		private bool _isSleeping = true;
		private bool _isActive = true;
		private bool _isAutoProcessing = true;
		private TimeSpan _delayBetweenJobs = TimeSpan.FromMilliseconds(50);
		private AutoResetEvent _jobThreadCanResumeEvent;
		private object _syncRoot = new object();

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
				return _jobThread != null && _jobThread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId;
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
						throw new ObjectDisposedException("jobQueue");
					}

					return _jobQueue.Count;
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
				lock (this._syncRoot)
				{
					return this._isActive;
				}
			}

			set
			{
				bool valueChanged = false;

				lock (this._syncRoot)
				{
					if (_isActive != value)
					{
						valueChanged = true;
						_isActive = value;
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
				lock (this._syncRoot)
				{
					return this._isAutoProcessing;
				}
			}

			set
			{
				bool valueChanged = false;

				lock (this._syncRoot)
				{
					if (_isAutoProcessing != value)
					{
						valueChanged = true;
						_isAutoProcessing = value;
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
				lock (this._syncRoot)
				{
					return this._delayBetweenJobs;
				}
			}

			set
			{
				lock (this._syncRoot)
				{
					this._delayBetweenJobs = value;
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
		/// Disposes this JobQueue and all its resources.
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

				// Waits for the thread to die and then dispose other related resources.
				_jobThread.Join();
				_jobThreadCanResumeEvent.Dispose();

				// Bye bye.
				_jobThread = null;
				_jobThreadCanResumeEvent = null;
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
		/// <param name="tag">A custom tag for this action.</param>
		protected void AcceptJob(Action job, bool wakeUpThread = true, string tag = null)
		{
			// Sanity Check.
			if (IsDisposed)
			{
				throw new InvalidOperationException("This object is disposed.");
			}

			// Adds the job to the queue.
			lock (this._syncRoot)
			{
                this._jobQueue.Enqueue(new Job() { 
                    Action = job,
					Tag = tag
#if DEBUG
                    , StackWhenAccepted = new StackTrace()  
#endif
                });
			}

			// Makes sure the thread is running.
			EnsureThreadRuns();

			// Signals the thread to go on if it is waiting.
			if (wakeUpThread)
			{
				this._jobThreadCanResumeEvent.Set(); 
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
			if (this._jobThread == null)
			{
				// Creates the thread.
				this._jobThread = new Thread(ThreadMain)
				{
					Name = this.GetType().Name + "_" + this._syncRoot.GetHashCode(),
					IsBackground = true
				};

				// Creates the signaling event.
				this._jobThreadCanResumeEvent = new AutoResetEvent(true);
			}

			// Starts the thread if it is not alive.
			if (!this._jobThread.IsAlive)
			{
				this._jobThread.Start();
			}
		}

		/// <summary>
		/// Removes from the job queue all jobs whose tag is equal to
		/// a specified tag.
		/// </summary>
		/// <param name="tag">A tag to compare. Must not be null.</param>
		protected void RemoveJobsWithTag(string tag)
		{
			lock (_syncRoot)
			{
				_jobQueue.IgnoreTagUntilCurrentEndOfQueue(tag);
			}
		}

		private void OnContinuesOnCompletionChanged(bool newValue)
		{
			if (newValue && this._jobThread != null && this._jobThread.IsAlive)
			{
				// Wakes the thread up, there may be some new things to do.
				this._jobThreadCanResumeEvent.Set();
			}
		}

		private void OnIsActiveChanged(bool value)
		{
			if (value && this._jobThread != null && this._jobThread.IsAlive)
			{
				// Wakes the thread up, there may be some new things to do.
				this._jobThreadCanResumeEvent.Set();
			}
		}

		#endregion

		#region Job Processing

		private void ThreadMain()
		{
			while (!this._isDisposed)
			{
				if (IsActive)
				{
					// The thread is waking up.
					IsSleeping = false;

					Job nextJob = null;
					int jobsLeftToDo = 0;

					// Gets the next job, if any.
					lock (this._syncRoot)
					{
						// If the instance just got disposed, return immediately.
						if (this._isDisposed)
						{
							return;
						}

						// Gets the next job, or null if none is found.
						nextJob = this._jobQueue.Dequeue();
					}

					// If there is a job to execute, do it.
					if (nextJob != null)
					{
						nextJob.Action();
					}

					// Gets how many jobs are still in queue.
					lock (_syncRoot)
					{
						jobsLeftToDo = this._jobQueue.Count;
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
				this._jobThreadCanResumeEvent.WaitOne();
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
