using System;

namespace Smasher.JobLib
{
	public delegate void JobEventHandler (IJobConsumer sender, Job job);
	
	public interface IJobConsumer
	{
		#region Events
		event JobEventHandler JobStarted;
		event JobEventHandler JobFinished;
		#endregion // Events
		
		void Consume (Job job);
		
		/// <summary>
		/// Gets the score of this consumer.
		/// </summary>
		/// <returns>
		/// The score. This represents how fast will this consumer be able to satisfy and consume a job.
		/// Ranges from 0.0 to 1.0 with 1.0 being the best (job will be executed close to immediately).
		/// </returns>
		float GetScore ();
	}
}

