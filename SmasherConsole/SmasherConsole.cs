using System;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Smasher.SmasherLib;
using Smasher.SmasherLib.Net;
using Smasher.JobLib;
using NDesk.Options;

namespace Smasher.UI
{
	class Smasher
	{
		public static void Main (string[] args)
		{
			// Default values
			string serverAddress = "localhost:3000";
			string listenerPort = "1234";
			ParseArguments(args, ref serverAddress, ref listenerPort);
			Console.WriteLine(serverAddress);

			Console.WriteLine("Start Listener");

			IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
			ClientInfo selfInfo =
				new ClientInfo(ipAddress.ToString() + ":" + listenerPort, "0.0.1");

			NetworkJobListener jobListener = new NetworkJobListener();
			if (!jobListener.Listen(serverAddress, selfInfo))
				Console.WriteLine("Failed to add to the server");

			Console.WriteLine("Start JobManager");
			LocalJobConsumer consumer = new LocalJobConsumer(2);
			consumer.JobStarted += (c, j) => {
				Console.WriteLine("C1 Started job {0}({1})", j.Id, ((SleepJob)j).SleepTime);
			};
			consumer.JobFinished += (c, j) => {
				Console.WriteLine("C1 Finished job {0}", j.Id);
			};

			// This will be a network consumer
			RemoteJobConsumer consumer2 = new RemoteJobConsumer();
			consumer2.Connect(serverAddress, selfInfo);

			consumer2.JobStarted += (c, j) => {
				Console.WriteLine("C2 Started remote job {0}({1})", j.Id, ((SleepJob)j).SleepTime);
			};
			consumer2.JobFinished += (c, j) => {
				Console.WriteLine("C2 Finished remote job {0}", j.Id);
			};

			return;

			JobManager manager = new JobManager();

			int seed = DateTime.Now.Millisecond;
			//int seed = 339;
			Random generator = new Random(seed);
			Console.WriteLine("Current Seed is {0}", seed); // in case we need to test a specific case
			for (uint i = 0; i < 10; ++i)
			{
				manager.EnqueueJob(new SleepJob(i, generator.Next(5000)));
			}
			manager.EnqueueJob(null);

			manager.Start(consumer, consumer2);
		}

		private static void ParseArguments(string[] args, ref string serverAddress, ref string listenerPort)
		{
			string address = null;
			string port = null;
			OptionSet parameters = new OptionSet() {
				{ "s|server=",	(v) => { address = v; } },
				{ "p|port=",	(v) => { port = v; } }
			};
			parameters.Parse(args);

			// Only change the output variables if we need too (we don't want to override defaults do we? :)
			if (!string.IsNullOrEmpty(address))
				serverAddress = address;
			if (!string.IsNullOrEmpty(port))
				listenerPort = port;
		}
	}
}
