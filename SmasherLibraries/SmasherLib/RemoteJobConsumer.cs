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

namespace Smasher.SmasherLib
{
	public class RemoteJobConsumer : IJobConsumer
	{
		public RemoteJobConsumer () : this(10)
		{
		}
		
		public RemoteJobConsumer (int maxNumOfConnections)
		{
			mMaxNumOfConnections = maxNumOfConnections;
			mHasJobWaiting = false;
			mIsRunning = false;
			mConnectionList = new List<Socket>(maxNumOfConnections);
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
				
				// Remove disconnected
				// TODO: Reuse sockets!
				mConnectionList.RemoveAll((socket) => {
					return !socket.Connected;
				});
				
				if (mConnectionList.Count < mMaxNumOfConnections)
				{
					// TODO: Would be cool to have an ignore list instead of just self
					IEnumerable<string> smasherList = mApi.GetSmasherList(mSelfInfo);
					if (smasherList != null)
					{
						foreach (string smasherAddress in smasherList)
						{
							Console.WriteLine("Found smasher in {0}", smasherAddress);
							
							try
							{
								// Start a connection with this smasher
								// TODO: Add timeout to connection (use a Thread?)
								IPEndPoint smasherEndPoint = SmasherAddressUtil.GetSmasherEndPoint(smasherAddress);
								Socket smasher = ConnectToSmasher(smasherEndPoint);
								if (smasher != null && smasher.Connected)
								{
									// Add to connection list!
									mConnectionList.Add(smasher);
								}
								else
								{
									// Ignore this Smasher (hopefully we'll have ignore list later)
									Console.WriteLine("Ignoring {0}", smasherAddress);
								}
							}
							catch (Exception e)
							{
								// Ignore this Smasher (hopefully we'll have ignore list later)
								Console.WriteLine("Ignoring {0} because of {1}", smasherAddress, e.ToString());
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
			if (mHasJobWaiting)
				throw new InvalidOperationException("Hey! There's a job waiting! Check HasAvailableWorkers value first!");
			
			/*
			BinaryFormatter formatter = new BinaryFormatter();
			NetworkStream sendStream = new NetworkStream(client);
			formatter.Serialize(sendStream, job);
			*/
		}

		public bool HasAvailableWorkers {
			get { return !mHasJobWaiting && mConnectionList.Count > 0; }
		}
		#endregion
		
		private int mMaxNumOfConnections;
		private bool mHasJobWaiting;
		private bool mIsRunning;
		
		private List<Socket> mConnectionList;
		
		private ServerApi mApi;
		private ClientInfo mSelfInfo;
	}
}

