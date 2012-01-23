using System;
using System.Diagnostics;
using Smasher.JobLib;
using Smasher.SmasherLib.Net;
using System.Collections.Generic;
using System.Net;

namespace Smasher.SmasherLib
{
	public class RemoteJobConsumer : IJobConsumer
	{
		public RemoteJobConsumer ()
		{
			mHasJobWaiting = false;
		}
		
		public void Connect (string serverAddress, ClientInfo selfInfo)
		{
			Uri serverUrl = new Uri("http://" + serverAddress, UriKind.Absolute);
			ServerApi api = new ServerApi(serverUrl);
			
			IEnumerable<string> smasherList = api.GetSmasherList(selfInfo);
			if (smasherList != null)
			{
				foreach (string smasherAddress in smasherList)
				{
					Console.WriteLine("Found smasher in {0}", smasherAddress);
				}
			}
			
			// Start Keep Alive loop in a different thread.
			// This will update the list of connected smashers and get more from the server if necessary
		}
		
		/// <summary>This will stop the keep alive loop!</summary>
		public void Disconnect ()
		{
		}

		#region IJobConsumer implementation
		public event JobEventHandler JobStarted;
		public event JobEventHandler JobFinished;

		public void Consume (Job job)
		{
			Debug.Assert(HasAvailableWorkers, "Doesn't have available workers!");
			if (mHasJobWaiting)
				throw new InvalidOperationException("Hey! There's a job waiting! Check HasAvailableWorkers value first!");
		}

		public bool HasAvailableWorkers {
			get { return !mHasJobWaiting; } // TODO: And has connections
		}
		#endregion
		
		private bool mHasJobWaiting;
	}
}

