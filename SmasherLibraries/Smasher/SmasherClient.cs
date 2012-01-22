using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Smasher
{
	[Serializable]
	public abstract class DistributedFunc
	{
		public DistributedFunc (int id)
		{
			mId = id;
		}
		
		public abstract void Invoke ();
		
		protected int mId;
	}
	
	[Serializable]
	public class CalculateStuff : DistributedFunc
	{
		public CalculateStuff (int id ) : base(id)
		{
		}
		
		public override void Invoke ()
		{
			Console.WriteLine("This is my id {0}", mId);
		}
	}
	
	public class SmasherClient
	{
		public class StateObject
		{
			public Socket WorkSocket = null;
			public const int BufferSize = 1024;
			public byte[] buffer = new byte[BufferSize];
			public StringBuilder sb = new StringBuilder();
		}
		
		#region Constructor
		
		public SmasherClient ()
		{
		}
		
		#endregion // Constructor
		
		
		public void Listen (ushort httpPort, ushort clientPort)
		{
			Debug.Assert(httpPort != clientPort, 
			             "You can't listen to http and client requests using the same port");
			
			// Setup network listener
			IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
			
			// Setup the http listener
			Console.WriteLine("Setting up Http {0}:{1}", ipAddress.ToString(), httpPort);
			
			// Setup the client listener
			Console.WriteLine("Setting up Client {0}:{1}", ipAddress.ToString(), clientPort);
			IPEndPoint localEP = new IPEndPoint(ipAddress, clientPort);
			
			Thread test = new Thread(new ThreadStart(() => { SetupClient(localEP); }));
			test.Start();
			TestConnection(localEP);
		}
		
		private void TestConnection (IPEndPoint remoteEndPoint)
		{
			gAllDone.WaitOne();
			Socket client = new Socket(remoteEndPoint.Address.AddressFamily,
			                           SocketType.Stream, ProtocolType.Tcp);
			
			try
			{
				client.Connect(remoteEndPoint);
				//Console.WriteLine("Sending stuff");
				byte[] buffer = Encoding.UTF8.GetBytes("YELLOW");
				client.Send(buffer);
				//client.Send(new Buffer[1] { 0xFF });
				
				int read = client.Receive(buffer);
				string supString = Encoding.UTF8.GetString(buffer, 0, read);
				if (supString == "SUP")
				{
					Console.WriteLine("Sending Objects");
					BinaryFormatter formatter = new BinaryFormatter();
					NetworkStream sendStream = new NetworkStream(client);
					for (int i = 0; i < 1; i++)
					{
						DistributedFunc testNet = new CalculateStuff(i);
						formatter.Serialize(sendStream, testNet);
					}
				}
				client.Close();
			}
			catch (Exception e)
			{
				Console.WriteLine("Couldn't test connection\n{0}", e.ToString());
			}
		}
		
		private void SetupClient (IPEndPoint localEndPoint)
		{
			Socket listener = new Socket(localEndPoint.Address.AddressFamily,
										SocketType.Stream, ProtocolType.Tcp);
			
			try
			{
				listener.Bind(localEndPoint);
				listener.Listen(NUM_OF_LISTENERS);
				
				// TODO: Work on the Shutdown stuff
				while (true)
				{
					gAllDone.Reset();
					
					listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
					
					gAllDone.WaitOne();
				}
			}
			catch (SocketException e)
			{
				Console.WriteLine("Something went wrong with the socket:\n{0}", e.ToString());
			}
			catch (ObjectDisposedException e)
			{
				Console.WriteLine("Someone closed the Socket! Find them!!!\n{0}", e.ToString());
			}
			
			Console.WriteLine( "Closing the listener...");
		}
		
		public static void AcceptCallback(IAsyncResult ar)
		{
			// Get the socket that handles the client request.
			Socket listener = (Socket) ar.AsyncState;
			Socket handler = listener.EndAccept(ar);
			
			// Tell the main client thread we got one client,
			// keep waiting for others :)
			Console.WriteLine("Parallel!!!");
			gAllDone.Set();
			
			// Create the state object.
			StateObject state = new StateObject();
			state.WorkSocket = handler;
			
			byte[] yellowShake = new byte[10];
			int read = handler.Receive(yellowShake);
			string yellowString = Encoding.UTF8.GetString(yellowShake, 0, read);
			if (yellowString == "YELLOW")
			{
				handler.Send(Encoding.UTF8.GetBytes("SUP"));
				
				handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.Peek,
									new AsyncCallback(ReadCallback), state);
			}
			else
			{
				handler.Close();
			}
		}
		
		public static void ReadCallback(IAsyncResult ar)
		{
			StateObject state = (StateObject)ar.AsyncState;
			Socket handler = state.WorkSocket;
			
			// Read data from the client socket.
			int read = handler.EndReceive(ar);
			
			// Data was read from the client socket.
			if (read > 0)
			{
				NetworkStream stream = new NetworkStream(handler);
				BinaryFormatter formatter = new BinaryFormatter();
				DistributedFunc testNet = (DistributedFunc)formatter.Deserialize(stream);
				if (testNet != null)
				{
					// TODO: Use action to execute in the thread pool and then return here to send back
					// Use lock (handler) or (stream) to send it back (so we don't entangle messages hehe)
					testNet.Invoke();
				}
				else
				{
					Console.WriteLine("Failed");
				}
				
				handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.Peek,
									new AsyncCallback(ReadCallback), state);
			}
			else {
				handler.Close();
			}
		}
		
		
		#region Arguments
		
		public const short NUM_OF_LISTENERS = 5;
	
		public static ManualResetEvent gAllDone = new ManualResetEvent(false);
		
		#endregion // Arguments
	}
}

