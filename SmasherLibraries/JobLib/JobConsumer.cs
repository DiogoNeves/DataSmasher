using System;

namespace Smasher.JobLib
{
	public delegate void JobEventHandler (IJobConsumer sender, Job job);
	
	public interface IJobConsumer
	{
		#region Events
		event JobEventHandler JobStarted;
		event JobEventHandler JobFinished;
		event JobEventHandler JobFailed;
		#endregion // Events
		
		void Consume (Job job);
		bool HasAvailableWorkers { get; }
	}
}

