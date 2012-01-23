using System;
using System.Diagnostics;

namespace Smasher.JobLib
{
	[Serializable]
	public abstract class Job
	{
		#region Constructors
		private Job () : this(0)
		{
			Debug.Fail("How did you call this constructor?");
		}
		
		public Job (uint id)
		{
			mId = id;
		}
		#endregion // Constructors
		
		/// <summary>
		/// This has to be implemented by the specialised Job.
		/// </summary>
		/// <remarks>
		/// Everything in this class has to be self contained and the method can't have parameters (they should
		/// basically be properties in the class, set before being consumed).
		/// This is due to the fact we may have to serialise it over the network... Maybe later I'll figure out a
		/// way to serialise closures.
		/// </remarks>
		public abstract void Invoke ();
		
		
		public uint Id
		{
			get { return mId; }
		}
		
		
		private readonly uint mId;
	}
}

