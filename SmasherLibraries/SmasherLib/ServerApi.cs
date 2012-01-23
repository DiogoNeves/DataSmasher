using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Smasher.SmasherLib.Net
{
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
	
	public class ServerApi
	{
		public ServerApi (Uri serverUrl)
		{
			if (!serverUrl.IsAbsoluteUri || serverUrl.Scheme != Uri.UriSchemeHttp)
				throw new ArgumentException("The Server API only takes absolute HTTP urls!");
			
			mServerUrl = serverUrl;
		}
		
		
		public bool AddSmasher (ClientInfo info)
		{
			string infoJson = JsonConvert.SerializeObject(info);
			Uri addSmasherUrl = new Uri(mServerUrl.AbsoluteUri + "addsmasher");
			string responseJson = SendHttpRequest(addSmasherUrl, POST, infoJson);
			
			// HACKYYYYYY, but is probably quicker than deserialising! :)
			return !responseJson.Contains("FAIL");
		}
		
		public IEnumerable<string> GetSmasherList (ClientInfo self)
		{
			string url = mServerUrl.AbsoluteUri + "smasherlist.json";
			if (!string.IsNullOrEmpty(self.Address))
			{
				// Added self so that the server can ignore it
				url += "/filter/" + self.Address;
			}
			
			Uri getListUrl = new Uri(url);
			string smasherListJson = SendHttpRequest(getListUrl, GET);
			
			if (!string.IsNullOrEmpty(smasherListJson))
			{
				// We got the list, deserialise! :)
				IEnumerable<string> smasherList = JsonConvert.DeserializeObject<IEnumerable<string>>(smasherListJson);
				return smasherList;
			}
			
			return null;
		}
		
		private static string SendHttpRequest (Uri serverUrl, string method, string jsonContent = null)
		{
			// Don't use try here as we want the main application to know it failed!
			// Create request
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(serverUrl);
			request.Method = method;
			request.UserAgent = "DataSmasher Listener v0.1";

			// Send post information or just finalise send if there's nothing to send
			if (method == POST && !string.IsNullOrEmpty(jsonContent))
			{
				request.ContentType = "application/json";
				byte[] postData = CONNECTION_ENCODING.GetBytes(jsonContent == null ? "" : jsonContent);
				Stream requestStream = request.GetRequestStream();
				requestStream.Write(postData, 0, postData.Length);
				requestStream.Close();
			}
			
			// Receive response
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Stream responseStream = response.GetResponseStream();
			StringBuilder responseString = new StringBuilder();
			using (StreamReader reader = new StreamReader(responseStream, CONNECTION_ENCODING))
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

		
		public static readonly Encoding CONNECTION_ENCODING = Encoding.UTF8;
		
		private static readonly string GET = "GET";
		private static readonly string POST = "POST";
		
		private readonly Uri mServerUrl;
	}
}

