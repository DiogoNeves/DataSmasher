using System;
using System.Diagnostics;
using System.Threading;
using Smasher.JobLib;

namespace Smasher.SmasherLib
{
	public delegate void LocalJobEventHandler (IJobConsumer sender, Job job);
	
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
			Debug.Assert(mNumOfActiveWorkers < mMaxNumOfWorkers, "Invalid number of workers");
			
			Interlocked.Increment(ref mNumOfActiveWorkers);
			if (JobStarted != null)
				JobStarted(this, job);
			
			// Invoke job in a different thread
			Thread jobThread = new Thread(new ThreadStart(() => {
				job.Invoke();
				
				// Always do before so that if the event handlers get the number of active workers, it is right :)
				Interlocked.Decrement(ref mNumOfActiveWorkers);
				if (JobFinished != null)
					JobFinished(this, job);
			}));
			
			jobThread.Start();
		}

		public float GetScore ()
		{
			return 1.0f - (mNumOfActiveWorkers / (float)mMaxNumOfWorkers);
		}
		#endregion // IJobConsumer implementation
		
		
		private readonly int mMaxNumOfWorkers;
		private int mNumOfActiveWorkers;
	}
}

