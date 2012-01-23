using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Net;
using Newtonsoft.Json;

namespace Smasher.SmasherLib.Net
{
	#region Inner classes
	[Serializable]
	public struct ClientInfo
	{
		public ClientInfo (string address, string version)
		{
			Address = address;
			Version = version;
		}
		
		public readonly string Address;
		public readonly string Version;
	}
	#endregion // Inner classes
	
	public class ServerApi
	{
		static ServerApi ()
		{
			gSerialiser = new JsonSerializer();
			gSerialiser.NullValueHandling = NullValueHandling.Ignore;
			
			gEncoding = Encoding.UTF8;
		}
		
		public ServerApi (Uri serverUrl)
		{
			if (!serverUrl.IsAbsoluteUri || serverUrl.Scheme != Uri.UriSchemeHttp)
				throw new ArgumentException("The Server API only takes absolute HTTP urls!");
			
			mServerUrl = serverUrl;
		}
		
		
		public bool AddSmasher (ClientInfo info)
		{
			string infoJson = ToJson(info);
			Uri addSmasherUrl = new Uri(mServerUrl.AbsoluteUri + "addsmasher");
			string responseJson = SendHttpRequest(addSmasherUrl, infoJson);
			
			// HACKYYYYYY, but is probably quicker than deserialising! :)
			return !responseJson.Contains("FAIL");
		}
		
		private static string SendHttpRequest (Uri serverUrl, string jsonContent)
		{
			// Don't use try here as we want the main application to know it failed!
			// Create request
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(serverUrl);
			request.Method = "POST";
			request.UserAgent = "DataSmasher Listener v0.1";
			request.ContentType = "application/json";

			// Send post information
			byte[] postData = gEncoding.GetBytes(jsonContent);
			Stream requestStream = request.GetRequestStream();
			requestStream.Write(postData, 0, postData.Length);
			requestStream.Close();
			
			// Receive response
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Stream responseStream = response.GetResponseStream();
			StringBuilder responseString = new StringBuilder();
			using (StreamReader reader = new StreamReader(responseStream, gEncoding))
			{
				char[] buffer = new char[256];
				int count = reader.Read(buffer, 0, buffer.Length);
				while (count > 0)
				{
					responseString.Append(buffer, 0, count);
					count = reader.Read(buffer, 0, buffer.Length);
				}
			}

			responseStream.Close();
			
			return responseString.ToString();
		}
		
		private static string ToJson (object info)
		{
			// Serialise info
			string infoJson = "";
			using (StringWriter jsonStream = new StringWriter())
			{
				gSerialiser.Serialize(jsonStream, info);
				infoJson = jsonStream.ToString();
				jsonStream.Close();
			}
			
			return infoJson;
		}
		
		
		private static readonly JsonSerializer gSerialiser;
		private static readonly Encoding gEncoding;
		
		private readonly Uri mServerUrl;
	}
}

