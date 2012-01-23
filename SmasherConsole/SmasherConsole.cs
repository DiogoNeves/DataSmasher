using System;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Smasher.SmasherLib;
using Smasher.JobLib;

namespace Smasher.UI
{
	class Smasher
	{
		public static void Main (string[] args)
		{
			Console.WriteLine("Test 1");

			LocalJobConsumer consumer = new LocalJobConsumer(2);
			consumer.JobStarted += (c, j) => {
				Console.WriteLine("C1 Started job {0}({1})", j.Id, ((SleepJob)j).SleepTime);
			};
			consumer.JobFinished += (c, j) => {
				Console.WriteLine("C1 Finished job {0}", j.Id);
			};

			// This will be a network consumer
			LocalJobConsumer consumer2 = new LocalJobConsumer(4);
			consumer2.JobStarted += (c, j) => {
				Console.WriteLine("C2 Started job {0}({1})", j.Id, ((SleepJob)j).SleepTime);
			};
			consumer2.JobFinished += (c, j) => {
				Console.WriteLine("C2 Finished job {0}", j.Id);
			};

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
	}
}
