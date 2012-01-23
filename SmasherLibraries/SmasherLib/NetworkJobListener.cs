using System;
using System.Net;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Smasher.SmasherLib.Net
{
	public class NetworkJobListener
	{
		[Serializable]
		private struct ListenerInfo
		{
			public ListenerInfo (string address, string version)
			{
				Address = address;
				Version = version;
			}
			
			public readonly string Address;
			public readonly string Version;
		}
		
		
		public NetworkJobListener ()
		{
			mIsListening = false;
		}
		
		public bool Listen (string serverAddress, ushort listenerPort, string clientVersion)
		{
			if (mIsListening)
			{
				throw new InvalidOperationException("Sorry, you can't listen with the same listener twice...");
			}
			
			IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
			ListenerInfo listenerInfo = new ListenerInfo(ipAddress.ToString() + ":" + listenerPort.ToString(), clientVersion);
			
			if (!RegisterWithServer(serverAddress, listenerInfo))
				return false;
			
			// TODO: Start listening
			
			return true;
		}
		
		private bool RegisterWithServer (string serverAddress, ListenerInfo info)
		{
			JsonSerializer serializer = new JsonSerializer();
			serializer.NullValueHandling = NullValueHandling.Ignore;

			// Serialise listener info
			string listenerJson = "";
			using (StringWriter jsonStream = new StringWriter())
			{
				serializer.Serialize(jsonStream, info);
				listenerJson = jsonStream.ToString();
				jsonStream.Close();
			}
			
			// Don't use try here as we want the main application to know it failed!
			// Create request
			Uri serverUrl = new Uri("http://" + serverAddress + "/addsmasher", UriKind.Absolute);
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(serverUrl);
			request.Method = "POST";
			request.UserAgent = "DataSmasher Listener v0.1";
			request.ContentType = "application/json";

			// Send post information
			byte[] postData = Encoding.UTF8.GetBytes(listenerJson);
			Stream requestStream = request.GetRequestStream();
			requestStream.Write(postData, 0, postData.Length);
			requestStream.Close();
			
			// Receive response
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Stream responseStream = response.GetResponseStream();
			StringBuilder responseString = new StringBuilder();
			using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
			{
				char[] buffer = new char[BUFFER_SIZE];
				int count = reader.Read(buffer, 0, buffer.Length);
				while (count > 0)
				{
					responseString.Append(buffer, 0, count);
					count = reader.Read(buffer, 0, buffer.Length);
				}
			}

			responseStream.Close();
			
			// HACKYYYYY, change later
			return responseString.ToString().Contains("OK");
		}
		
		
		private const int BUFFER_SIZE = 256;
		
		private bool mIsListening;
	}
}

