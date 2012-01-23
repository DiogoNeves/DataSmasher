using System;
using System.Net;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Smasher.JobLib;

namespace Smasher.SmasherLib.Net
{
	public delegate void NetworkJobEventHandler (Job job);
	
	public class NetworkJobListener
	{
		#region Events
		public event NetworkJobEventHandler JobReceived;
		public event NetworkJobEventHandler JobReturned;
		#endregion // Events
		
		
		public NetworkJobListener ()
		{
			mIsListening = false;
		}
		
		/// <summary>
		/// Listen the specified serverAddress, listenerPort and clientVersion.
		/// THIS BLOCKS THE THREAD! Run in a different thread if you don't want to block!
		/// </summary>
		public bool Listen (string serverAddress, ushort listenerPort, string clientVersion)
		{
			if (mIsListening)
			{
				throw new InvalidOperationException("Sorry, you can't listen with the same listener twice...");
			}
			
			Uri serverUrl = new Uri("http://" + serverAddress, UriKind.Absolute);
			ServerApi api = new ServerApi(serverUrl);
			
			IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
			ClientInfo listenerInfo =
				new ClientInfo(ipAddress.ToString() + ":" + listenerPort.ToString(), clientVersion);
			
			if (!api.AddSmasher(listenerInfo))
				return false;
			
			// TODO: Start listening
			
			return true;
		}
		
		
		private bool mIsListening;
	}
}

