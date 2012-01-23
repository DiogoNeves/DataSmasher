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
			// Parse arguments or use default values
			string serverAddress = "localhost:3000";
			string listenerPort = "1234";
			ParseArguments(args, ref serverAddress, ref listenerPort);

			// Get client information
			IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
			ClientInfo selfInfo =
				new ClientInfo(ipAddress.ToString() + ":" + listenerPort, "0.0.1");

			// Create consumers
			Console.WriteLine("Create Job Consumers");

			LocalJobConsumer local = new LocalJobConsumer(2);
			RemoteJobConsumer remote = new RemoteJobConsumer();
			remote.Connect(serverAddress, selfInfo);

			// Create the job manager (different thread)
			Console.WriteLine("Start Job Manager");

			JobManager manager = new JobManager();
			Thread managerThread = new Thread(new ThreadStart(() => {
				manager.Start(local, remote);
			}));

			// We need the manager to one of the consumer delegates, set them here before starting consuming stuff!!!
			// Not, no circular dependencies ;) and that's why we love delegates
			SetConsumerDelegates(local, manager, "local");
			SetConsumerDelegates(remote, manager, "remote");

			// Now we can start the manager
			managerThread.Start();

			/*/ // Toggle debug code
			int seed = DateTime.Now.Millisecond;
			//int seed = 339;
			Random generator = new Random(seed);
			Console.WriteLine("Current Seed is {0}", seed); // in case we need to test a specific case
			for (uint i = 0; i < 10; ++i)
			{
				manager.EnqueueJob(new SleepJob(i, generator.Next(5000)));
			}
			manager.EnqueueJob(null);
			/**/

			// Start listening for network jobs
			Console.WriteLine("Start Network Listener");

			NetworkJobListener jobListener = new NetworkJobListener();
			jobListener.JobReceived += (job) => {
				Console.WriteLine("Received {0} job over the network", job.Id);
				manager.EnqueueJob(job);
			};
			jobListener.JobReturned += (job) => {
				Console.WriteLine("Returned {0} job over the network", job.Id);
			};

			Thread listenerThread = new Thread(new ThreadStart(() => {
				if (!jobListener.Listen(serverAddress, selfInfo))
					Console.WriteLine("Failed to add to the server");
			}));
			listenerThread.Start();

			/*/ // Toggle 2 sec shutdown
			Thread debugTerminate = new Thread(new ThreadStart(() => {
				Thread.Sleep(2000);
				manager.EnqueueJob(null);
			}));
			debugTerminate.Start();
			/**/

			managerThread.Join();

			// Terminate update threads!
			Console.WriteLine("Terminating update loops");

			remote.Disconnect();
			jobListener.Stop();

			Console.WriteLine("The End!");
			Console.ReadKey();
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

		private static void SetConsumerDelegates (IJobConsumer consumer, JobManager manager, string name)
		{
			consumer.JobStarted += (cons, job) => {
				Console.WriteLine("{0} - Started job {1}({2})", name, job.Id, ((SleepJob)job).SleepTime);
			};
			consumer.JobFinished += (cons, job) => {
				Console.WriteLine("{0} - Finished job {1}", name, job.Id);
			};
			// TODO: Add reason to the delegate!
			consumer.JobFailed += (cons, job) => {
				Console.WriteLine("{0} - Failed to consume job {1}", name, job.Id);

				// Add it back to the queue
				manager.EnqueueJob(job);
			};
		}
	}
}
