using System;
using System.Net;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Smasher.JobProcessor;

namespace SmasherConsole
{
	class Smasher
	{
		class SmasherInfo
		{
			public SmasherInfo () {}

			public SmasherInfo (string address)
			{
				mAddress = address;
			}

			public string Address
			{
				get { return mAddress; }
				set { mAddress = value; }
			}

			private string mAddress = "127.0.0.1:1234";
		}

		public static void AddToServer ()
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
			Console.WriteLine("Sending:\n{0}", smasherInfoData);

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

			try
			{
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				string responseString = "";
				Stream responseStream = response.GetResponseStream();
				using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
				{
					StringBuilder responseBuilder = new StringBuilder();
					char[] buffer = new char[BUFFER_SIZE];
					int count = reader.Read(buffer, 0, BUFFER_SIZE);
					while (count > 0)
					{
						responseBuilder.Append(buffer);
						count = reader.Read(buffer, 0, BUFFER_SIZE);
					}
					responseString = responseBuilder.ToString();
				}

				Console.WriteLine(responseString);
				responseStream.Close();
			}
			catch (WebException e)
			{
				Console.WriteLine("Something went wrong with the response:\n" + e.ToString());
			}

			Console.WriteLine("Finished Smasher!");
		}

		public static void Main (string[] args)
		{
			//AddToServer();
			JobService jobService = JobService.CreateJobService(4);

			// Create jobs
			for (int i = 0; i < 100; ++i)
			{
				jobService.EnqueueJob(new JobInfo(i, (x) => { Console.WriteLine("This is info{0}", x); }));
			}

			jobService.WaitAll();
		}


		private const int BUFFER_SIZE = 256;
	}
}
