using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace Smasher.JobProcessor
{
	public class JobInfo
	{
		#region Constructor
		protected JobInfo () : this(0, null)
		{
		}
		
		public JobInfo (int id, Action<int> action)
		{
			mId = id;
			mAction = action;
		}
		#endregion // Constructor
		
		public void Run ()
		{
			mAction(mId);
		}
		
		private int mId;
		private Action<int> mAction;
	}
	
	/// <summary>
	/// This is basically a ThreadPool
	/// </summary>
	public class JobService
	{
		#region Constructors and Factory
		
		public static JobService CreateJobService (int numOfWorkers)
		{
			return new JobService(numOfWorkers);
		}
		
		protected JobService () : this(4)
		{
		}
		
		protected JobService (int numOfWorkers)
		{
			Debug.Assert(numOfWorkers > 0, "You need, at least, 1 worker!");
			
			mNumOfWorkers = numOfWorkers;
		}
		
		#endregion // Constructors and Factory
		
		
		#region Producer and Consumer implementation
		
		public void EnqueueJob (JobInfo job)
		{
			Interlocked.Increment(ref mNumOfJobs);
			ThreadPool.QueueUserWorkItem(new WaitCallback(ConsumeJob), job);
		}
		
		protected void ConsumeJob (Object jobObject)
		{
			JobInfo job = jobObject as JobInfo;
			if (job != null)
			{
				job.Run();
				Interlocked.Decrement(ref mNumOfJobs);
			}
		}
		
		public void WaitAll ()
		{
			while (mNumOfJobs != 0) {}
		}
		#endregion // Producer and Consumer implementation
		
		
		#region Arguments
		
		public int NumberOfWorkers
		{
			get { return mNumOfWorkers; }
		}
		
		private int mNumOfWorkers;
		private int mNumOfJobs;
		#endregion // Arguments
	}
}

