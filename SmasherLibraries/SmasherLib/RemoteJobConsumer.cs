using System;
using Smasher.JobLib;

namespace Smasher.SmasherLib
{
	public class RemoteJobConsumer : IJobConsumer
	{
		public RemoteJobConsumer ()
		{
		}

		#region IJobConsumer implementation
		public event JobEventHandler JobStarted;
		public event JobEventHandler JobFinished;

		public void Consume (Job job)
		{
			throw new NotImplementedException ();
		}

		public bool HasAvailableWorkers {
			get {
				throw new NotImplementedException ();
			}
		}
		#endregion
	}
}

