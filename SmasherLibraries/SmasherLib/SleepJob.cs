using System;
using System.Threading;
using Smasher.JobLib;

namespace Smasher.SmasherLib
{
	[Serializable]
	public class SleepJob : Job
	{
		public SleepJob (uint id, int sleepTime) : base(id)
		{
			mSleepTime = sleepTime;
		}

		#region implemented abstract members of Smasher.JobLib.Job
		public override void Invoke ()
		{
			Thread.Sleep(SleepTime);
		}
		#endregion
		
		public int SleepTime
		{
			get { return mSleepTime; }
		}
		
		private readonly int mSleepTime;
	}
}

