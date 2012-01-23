using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace Smasher.JobLib
{
	public class JobManager
	{
		public JobManager ()
		{
			mQueueLocker = new object();
			mJobQueue = new Queue<Job>();
		}
		
		public void EnqueueJob (Job job)
		{
			lock (mQueueLocker)
			{
				mJobQueue.Enqueue(job);
				Monitor.Pulse(mQueueLocker);
			}
		}
		
		/// <summary>
		/// Starts the main loop.
		/// </summary>
		/// <param name='primary'>
		/// (Mandatory) Primary Job Consumer, this is the one that will have priority on Job consuption
		/// (e.g. local machine consumer).
		/// </param>
		/// <param name='secundary'>
		/// (Optional) Lower priority consumer.
		/// </param>
		/// <remarks>
		/// THIS BLOCKS EXECUTION! Execute in a thread if you don't want to block the main thread!
		/// Tweak the consumer score to achieve the best performance.
		/// </remarks>
		public void Start (IJobConsumer primary, IJobConsumer secundary)
		{
			Debug.Assert(primary != null);
			if (primary == null)
				throw new ArgumentNullException("You have to set the primary consumer!");
			
			while (true)
			{
				Job nextJob;
				lock (mQueueLocker)
				{
					while (mJobQueue.Count == 0)
						Monitor.Wait(mQueueLocker);
					
					nextJob = mJobQueue.Dequeue();
				}
				
				// We can't get here without Dequeueing at least once!
				if (nextJob == null)
					break;
				
				IJobConsumer nextConsumer = null;
				while (nextConsumer == null)
				{
					if (primary.HasAvailableWorkers)
					{
						nextConsumer = primary;
					}
					else if (secundary != null && secundary.HasAvailableWorkers)
					{
						nextConsumer = secundary;
					}
				}
				nextConsumer.Consume(nextJob);
			}
		}
		
		private readonly object mQueueLocker;
		private Queue<Job> mJobQueue;
	}
}

