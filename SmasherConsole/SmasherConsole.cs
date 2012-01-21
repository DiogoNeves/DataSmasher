using System;
using System.Net;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace SmasherConsole
{
	class Smasher
	{
		class SmasherInfo
		{
			public SmasherInfo () {}

			public SmasherInfo (string version)
			{
			}

			public string Version
			{
				get { return mVersion; }
				set { mVersion = value; }
			}

			private string mVersion = "0.0.1";
		}

		public static void Main (string[] args)
		{
			Console.WriteLine("Starting Smasher!");

			Console.WriteLine("Preparing request");

			JsonSerializer serializer = new JsonSerializer();
			serializer.NullValueHandling = NullValueHandling.Ignore;

			// get smasher info
			string smasherInfoData = "";
			using (StringWriter smasherInfoStream = new StringWriter())
			{
				serializer.Serialize(smasherInfoStream, new SmasherInfo());
				smasherInfoData = smasherInfoStream.ToString();
				smasherInfoStream.Close();
			}

			// create request
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("http://127.0.0.1:3000/addsmasher");
			request.Method = "POST";
			request.UserAgent = "DataSmasher v0.1";
			request.ContentType = "application/json";

			// send post information
			Console.WriteLine("Setting request content");

			byte[] postData = Encoding.UTF8.GetBytes(smasherInfoData);
			Stream requestStream = request.GetRequestStream();
			requestStream.Write(postData, 0, postData.Length);
			requestStream.Close();

			// get response
			Console.WriteLine("Getting response");

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Stream responseStream = response.GetResponseStream();
			Console.WriteLine(responseStream.ToString());
			responseStream.Close();

			Console.WriteLine("Finished Smasher!");
		}
	}
}
