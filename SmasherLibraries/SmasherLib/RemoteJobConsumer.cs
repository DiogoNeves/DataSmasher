using System;
using System.Diagnostics;
using Smasher.JobLib;
using Smasher.SmasherLib.Net;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Smasher.SmasherLib
{
	public class RemoteJobConsumer : IJobConsumer
	{
		public RemoteJobConsumer () : this(10)
		{
		}
		
		public RemoteJobConsumer (uint maxNumOfConnections)
		{
			mMaxNumOfConnections = maxNumOfConnections;
			mHasJobWaiting = false;
			mConnectionCount = 0;
			mIsRunning = false;
			mApi = null;
		}
		
		public void Connect (string serverAddress, ClientInfo selfInfo)
		{
			Uri serverUrl = new Uri("http://" + serverAddress, UriKind.Absolute);
			mApi = new ServerApi(serverUrl);
			mSelfInfo = selfInfo;
			
			// Start Keep Alive loop in a different thread.
			// This will update the list of connected smashers and get more from the server if necessary
			mIsRunning = true;
			Thread updateThread = new Thread(new ThreadStart(() => {
				Update();
			}));
			updateThread.Start();
		}
		
		/// <summary>This will stop the keep alive loop!</summary>
		public void Disconnect ()
		{
			mIsRunning = false;
		}
		
		private void Update ()
		{
			// Update loop (running on a thread)
			while (mIsRunning)
			{
				Debug.Assert(mApi != null, "You missed the API creation somewhere");
				
				if (mConnectionCount < mMaxNumOfConnections)
				{
					IEnumerable<string> smasherList = mApi.GetSmasherList(mSelfInfo);
					if (smasherList != null)
					{
						foreach (string smasherAddress in smasherList)
						{
							Console.WriteLine("Found smasher in {0}", smasherAddress);
						}
					}
				}
				
				// We don't want this to get too busy, wait a little before updating it again
				Thread.Sleep(1000);
			}
		}

		#region IJobConsumer implementation
		public event JobEventHandler JobStarted;
		public event JobEventHandler JobFinished;
		public event JobEventHandler JobFailed;

		public void Consume (Job job)
		{
			Debug.Assert(HasAvailableWorkers, "Doesn't have available workers!");
			if (mHasJobWaiting)
				throw new InvalidOperationException("Hey! There's a job waiting! Check HasAvailableWorkers value first!");
		}

		public bool HasAvailableWorkers {
			get { return !mHasJobWaiting && mConnectionCount > 0; }
		}
		#endregion
		
		private uint mMaxNumOfConnections;
		private uint mConnectionCount;
		private bool mHasJobWaiting;
		private bool mIsRunning;
		
		private ServerApi mApi;
		private ClientInfo mSelfInfo;
	}
}

