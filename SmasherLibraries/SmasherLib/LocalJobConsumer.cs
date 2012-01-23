using System;
using System.Diagnostics;
using System.Threading;
using Smasher.JobLib;

namespace Smasher.SmasherLib
{
	public class LocalJobConsumer : IJobConsumer
	{
		public LocalJobConsumer () : this(4)
		{
		}
		
		public LocalJobConsumer (int maxNumOfWorkers)
		{
			Debug.Assert(maxNumOfWorkers > 0, "Invalid max number of workers");
			
			mMaxNumOfWorkers = maxNumOfWorkers;
			mNumOfActiveWorkers = 0;
		}

		#region IJobConsumer implementation
		public event JobEventHandler JobStarted;
		public event JobEventHandler JobFinished;

		public void Consume (Job job)
		{
			Debug.Assert(HasAvailableWorkers, "Doesn't have available workers!");
			if (!HasAvailableWorkers)
				throw new InvalidOperationException("Hey! We're busy! Check HasAvailableWorkers value first!");
			
			Interlocked.Increment(ref mNumOfActiveWorkers);
			if (JobStarted != null)
				JobStarted(this, job);
			
			// Invoke job in a different thread
			// TODO: Cache threads and pass the job in the parameters
			Thread jobThread = new Thread(new ThreadStart(() => {
				job.Invoke();
				
				if (JobFinished != null)
					JobFinished(this, job);
				Interlocked.Decrement(ref mNumOfActiveWorkers);
			}));
				
			jobThread.Start();
		}

		public bool HasAvailableWorkers
		{
			get { return mNumOfActiveWorkers < mMaxNumOfWorkers; }
		}
		#endregion // IJobConsumer implementation
		
		
		private readonly int mMaxNumOfWorkers;
		private int mNumOfActiveWorkers;
	}
}

