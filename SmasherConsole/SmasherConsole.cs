using System;
using System.Net;

namespace SmasherConsole
{
	class Smasher
	{
		public static void Main (string[] args)
		{
			Console.WriteLine("Starting Smasher!");

			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("http://127.0.0.1:1337/addsmasher");
			request.Method = "POST";

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			Console.WriteLine(response.ToString());
		}
	}
}
