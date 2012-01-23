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
		}
	}
}
