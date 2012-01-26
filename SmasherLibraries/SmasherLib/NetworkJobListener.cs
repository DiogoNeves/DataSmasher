using System;
using System.Net;
using System.IO;
using System.Text;
using System.Diagnostics;
using Smasher.JobLib;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using Smasher.Api;

namespace Smasher.SmasherLib.Net
{
	public delegate void NetworkJobEventHandler (Job job);
	
	public class NetworkJobListener
	{
		#region Inner objects
		public class ConnectionState
		{
			public Socket mSocket = null;
			
			public const int BufferSize = 2056;
			public byte[] mBuffer = new byte[BufferSize];
		}
		
		private class RemoteJob : Job
		{
			public RemoteJob (Job originalJob, Action<RemoteJob> finishAction) : base(originalJob.Id)
			{
				mOriginalJob = originalJob;
				mFinishAction = finishAction;
			}

			#region implemented abstract members of Smasher.JobLib.Job
			public override void Invoke ()
			{
				Console.WriteLine("Invoking remote job {0}", Id);
				mOriginalJob.Invoke();
				mFinishAction(this);
			}
			#endregion // implemented abstract members of Smasher.JobLib.Job
			
			public Job OriginalJob
			{
				get { return mOriginalJob; }
			}
			
			private Job mOriginalJob;
			private Action<RemoteJob> mFinishAction;
		}
		#endregion // Inner objects
		
		#region Events
		public event NetworkJobEventHandler JobReceived;
		public event NetworkJobEventHandler JobReturned;
		#endregion // Events
		
		
		public NetworkJobListener ()
		{
			mIsListening = false;
			mListener = null;
			mApi = null;
		}
		
		/// <summary>
		/// Listen the specified serverAddress, listenerPort and clientVersion.
		/// THIS BLOCKS THE THREAD! Run in a different thread if you don't want to block!
		/// </summary>
		public bool Listen (string serverAddress, ClientInfo listenerInfo)
		{
			if (mIsListening)
			{
				throw new InvalidOperationException("Sorry, you can't listen with the same listener twice...");
			}
			
			Uri serverUrl = new Uri("http://" + serverAddress, UriKind.Absolute);
			mApi = new ServerApi(serverUrl);
			mListenerInfo = listenerInfo;
			
			if (!mApi.AddSmasher(mListenerInfo))
				return false;
			
			// Start listening
			mIsListening = true;
			UpdateLoop(SmasherAddressUtil.GetSmasherEndPoint(mListenerInfo.Address));
			
			return true;
		}
		
		public void Stop ()
		{
			if (mApi != null)
			{
				mApi.RemoveSmasher(mListenerInfo);
				mApi = null;
			}
			
			// This stops listening
			if (mListener != null && mListener.Connected)
			{
				// We don't want to use Shutdown here!
				mListener.Close(500);
				mListener = null;
			}
			
			// Do this in the end
			mIsListening = false;
		}
		
		private void UpdateLoop (IPEndPoint localEndPoint)
		{
			mListener = new Socket(localEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			mListener.Bind(localEndPoint);
			mListener.Listen(10);

			while (mIsListening)
			{
				gAllDone.Reset();

				mListener.BeginAccept(new AsyncCallback(AcceptCallback), mListener);

				gAllDone.WaitOne();
			}
		}

		public void AcceptCallback(IAsyncResult result)
		{
			// Get the socket that handles the client request
			// listener represents the connection 'server' (that waits for clients to connect) and not the
			// individual client connections
			Socket listener = (Socket)result.AsyncState;
			Socket handler = listener.EndAccept(result);

			// Tell the main client thread we got one client so that it can keep waiting for others :)
			gAllDone.Set();
			
			// From now on this is a separate thread :) cool!
			// Lets keep the state of this thing shall we?
			ConnectionState state = new ConnectionState();
			state.mSocket = handler;
			
			// TODO: Implement version control here
			// Valid connections start with a 'YELLOW' to which we respond 'SUP'
			byte[] yellowShakeBuffer = new byte[16];
			int read = handler.Receive(yellowShakeBuffer);
			string yellowString = ServerApi.CONNECTION_ENCODING.GetString(yellowShakeBuffer, 0, read);
			if (yellowString.Equals("YELLOW"))
			{
				// Answer and start waiting for data baby!
				handler.Send(ServerApi.CONNECTION_ENCODING.GetBytes("SUP"));
				handler.BeginReceive(state.mBuffer, 0, ConnectionState.BufferSize, SocketFlags.Peek,
									new AsyncCallback(ReadCallback), state);
			}
			else
			{
				// Ooops, not a valid client!
				handler.Close();
			}
		}

		public void ReadCallback(IAsyncResult result)
		{
			ConnectionState state = (ConnectionState)result.AsyncState;
			Socket handler = state.mSocket;

			// Read data from the client socket.
			int read = handler.EndReceive(result);

			// Data was read from the client socket.
			if (read > 0)
			{
				NetworkStream stream = new NetworkStream(handler);
				BinaryFormatter formatter = new BinaryFormatter();
				Job job = (Job)formatter.Deserialize(stream);
				if (job != null)
				{
					if (JobReceived != null)
					{
						// Wrap around a job we control so we can add a special finish action to send it back
						JobReceived(new RemoteJob(job, SendJobBack));
					}
				}

				handler.BeginReceive(state.mBuffer, 0, ConnectionState.BufferSize, SocketFlags.Peek,
									new AsyncCallback(ReadCallback), state);
			}
			else {
				handler.Close();
			}
		}
		
		private void SendJobBack (RemoteJob job)
		{
			Debug.Assert(job.OriginalJob != null);
			
			Console.WriteLine("Sending job back {0}", job.Id);
			// TODO: Send job.OriginalJob back
		}
		
		
		private bool mIsListening;
		private Socket mListener;
		private ServerApi mApi;
		private ClientInfo mListenerInfo;
		
		public static ManualResetEvent gAllDone = new ManualResetEvent(false);
	}
}

