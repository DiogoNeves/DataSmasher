using System;
using System.Diagnostics;
using Smasher.JobLib;
using Smasher.SmasherLib.Net;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using Smasher.Api;

namespace Smasher.SmasherLib
{
	public class RemoteJobConsumer : IJobConsumer
	{
		#region Inner objects
		private class ConnectionInfo
		{
			public ConnectionInfo (Socket smasher)
			{
				mUseCount = 0;
				mSmasher = smasher;
			}
			
			public Socket Smasher
			{
				get { return mSmasher; }
			}
			
			public int mUseCount;
			private readonly Socket mSmasher;
		}
		#endregion // Inner objects
		
		
		public RemoteJobConsumer () : this(10)
		{
		}
		
		public RemoteJobConsumer (int maxNumOfConnections)
		{
			mMaxNumOfConnections = maxNumOfConnections;
			mConnectionList = new Dictionary<string, ConnectionInfo>();
			mListLocker = new object();
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
			
			// Close all connections
			foreach (KeyValuePair<string, ConnectionInfo> connection in mConnectionList)
			{
				if (connection.Value.Smasher.Connected)
				{
					connection.Value.Smasher.Close();
				}
			}
			mConnectionList.Clear();
		}
		
		private void Update ()
		{
			// Update loop (running on a thread)
			while (mIsRunning)
			{
				Debug.Assert(mApi != null, "You missed the API creation somewhere");
				
				// Remove disconnected
				lock (mListLocker)
				{
					// Try to do a better remove!
					List<string> addresses = new List<string>(mConnectionList.Keys);
					foreach (string addr in addresses)
					{
						if (!mConnectionList[addr].Smasher.Connected)
						{
							Console.WriteLine("REMCONS - Removing disconnected Smasher {0}", addr);
							mConnectionList.Remove(addr);
						}
					}
				}
				
				if (mConnectionList.Count < mMaxNumOfConnections)
				{
					// TODO: Would be cool to have an ignore list instead of just self
					IEnumerable<string> smasherList = mApi.GetSmasherList(mSelfInfo);
					if (smasherList != null)
					{
						foreach (string smasherAddress in smasherList)
						{
							if (mConnectionList.ContainsKey(smasherAddress))
								continue;
							
							Console.WriteLine("REMCONS - Found smasher in {0}", smasherAddress);
							
							try
							{
								// Start a connection with this smasher
								// TODO: Add timeout to connection (use a Thread?)
								IPEndPoint smasherEndPoint = SmasherAddressUtil.GetSmasherEndPoint(smasherAddress);
								Socket smasher = ConnectToSmasher(smasherEndPoint);
								if (smasher != null && smasher.Connected)
								{
									// Add to connection list!
									lock (mListLocker)
									{
										mConnectionList.Add(smasherAddress, new ConnectionInfo(smasher));
									}
								}
								else
								{
									// Ignore this Smasher (hopefully we'll have ignore list later)
									Console.WriteLine("REMCONS - Ignoring {0}", smasherAddress);
								}
							}
							catch (Exception e)
							{
								// Ignore this Smasher (hopefully we'll have ignore list later)
								Console.WriteLine("REMCONS - Ignoring {0} because of {1}", smasherAddress, e.ToString());
							}
						}
					}
				}
				
				// We don't want this to get too busy, wait a little before updating it again
				Thread.Sleep(3000);
			}
		}
		
		private Socket ConnectToSmasher (IPEndPoint smasherEndPoint)
		{
			Socket client = new Socket(smasherEndPoint.Address.AddressFamily,
			                           SocketType.Stream, ProtocolType.Tcp);

			client.Connect(smasherEndPoint);
				
			// Shake hands please! We say 'YELLOW' they answer 'SUP' :)
			byte[] buffer = ServerApi.CONNECTION_ENCODING.GetBytes("YELLOW");
			client.Send(buffer);
			
			int read = client.Receive(buffer);
			string supString = ServerApi.CONNECTION_ENCODING.GetString(buffer, 0, read);
			if (supString == "SUP")
			{
				return client;
			}
			
			client.Close();
			
			return null;
		}

		#region IJobConsumer implementation
		public event JobEventHandler JobStarted;
		public event JobEventHandler JobFinished;
		public event JobEventHandler JobFailed;

		public void Consume (Job job)
		{
			Debug.Assert(HasAvailableWorkers, "Doesn't have available workers!");
			if (!HasAvailableWorkers)
				throw new InvalidOperationException("Hey! Check HasAvailableWorkers value first!");
			
			Thread sendJobThread = new Thread(new ThreadStart(() => {
				ConnectionInfo selectedConnection = null;
				
				// TODO: Is there a better way to do this without locking?
				lock(mListLocker)
				{
					foreach (ConnectionInfo connection in mConnectionList.Values)
					{
						// Check if it's connected and set as in use
						if (connection.Smasher.Connected &&
						    Interlocked.CompareExchange(ref connection.mUseCount, 1, 0) == 0)
						{
							selectedConnection = connection;
							break;
						}
					}
				}
				
				// We do this outside the foreach so that the lock takes as less time as possible
				if (selectedConnection != null)
				{
					Console.WriteLine("REMCONS - Sending job {0} to remote Smasher", job.Id);
					
					// We create the formatter and stream here because they might not be thread safe
					BinaryFormatter formatter = new BinaryFormatter();
					NetworkStream stream = new NetworkStream(selectedConnection.Smasher);
					formatter.Serialize(stream, job);
					
					// Start reading after this (block while reading), if it fails, add job back to queue
					// TODO: Add timeout
					Job result = (Job)formatter.Deserialize(stream);
					if (result != null)
					{
						if (JobFinished != null)
							JobFinished(this, result);
					}
					
					// Go back to unused
					Interlocked.Exchange(ref selectedConnection.mUseCount, 0);
				}
				else
				{
					// We could use the HasAvailableWorkers to only let it Consume if there were AVAILABLE workers
					// but we'd have to iterate through each connection everytime we call HasAvailableWorkers...
					// not ideal. It's much simpler to simply add the job back to the queue
					// TODO: Obviously this needs more work and thought! We don't want a job to keep coming back and
					// forward because we only have a connection and is being used!
					if (JobFailed != null)
						JobFailed(this, job);
				}
			}));
			sendJobThread.Start();
		}

		public bool HasAvailableWorkers {
			get { return mIsRunning && mConnectionList.Count > 0; }
		}
		#endregion
		
		private int mMaxNumOfConnections;
		private Dictionary<string, ConnectionInfo> mConnectionList;
		private object mListLocker;
		
		private bool mIsRunning;
		
		private ServerApi mApi;
		private ClientInfo mSelfInfo;
	}
}

