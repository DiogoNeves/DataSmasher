using System;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Smasher.SmasherLib;

namespace Smasher.UI
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

			HttpWebRequest request = null;
			try
			{
				Console.WriteLine("Sending:\n{0}", smasherInfoData);

				// create request
				request = (HttpWebRequest)HttpWebRequest.Create("http://127.0.0.1:3000/addsmasher");
				request.Method = "POST";
				request.UserAgent = "DataSmasher v0.1";
				request.ContentType = "application/json";
	
				// send post information
				Console.WriteLine("Setting request content");
	
				byte[] postData = Encoding.UTF8.GetBytes(smasherInfoData);
				Stream requestStream = request.GetRequestStream();
				requestStream.Write(postData, 0, postData.Length);
				requestStream.Close();
			}
			catch (WebException e)
			{
				Console.WriteLine("Failed to connect to server:\n{0}", e.ToString());
				return;
			}

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
			LocalJobConsumer consumer = new LocalJobConsumer();
			consumer.JobStarted += (c, j) => {
				Console.WriteLine("Started job {0}({1}) score is {2}", j.Id, ((SleepJob)j).SleepTime, c.GetScore());
			};
			consumer.JobFinished += (c, j) => {
				Console.WriteLine("Finished job {0} score is {1}", j.Id, c.GetScore());
			};

			Console.WriteLine("Initial consumer score is {0}", consumer.GetScore());

			int seed = DateTime.Now.Millisecond;
			//int seed = 339;
			Random generator = new Random(seed);
			Console.WriteLine("Current Seed is {0}", seed); // in case we need to test a specific case
			for (uint i = 0; i < 10; ++i)
			{
				while (consumer.GetScore() <= 0.0f)
				{
					Thread.Sleep(10);
				}

				consumer.Consume(new SleepJob(i, generator.Next(5000)));
			}

			Console.ReadKey();

			//AddToServer();

			//SmasherClient client = new SmasherClient();
			//client.Listen(1234, 3000);

			// Start job service, this will manage any tasks we get from the server
//			JobService jobService = JobService.CreateJobService(4);
//
//			// Create jobs
//			for (int i = 0; i < 100; ++i)
//			{
//				jobService.EnqueueJob(new JobInfo(i, (x) => { Console.WriteLine("This is info{0}", x); }));
//			}
//
//			jobService.WaitAll();
		}


		private const int BUFFER_SIZE = 256;
	}
}
