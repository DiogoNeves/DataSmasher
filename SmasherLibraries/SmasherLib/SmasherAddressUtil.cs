using System;
using System.Net;
using System.Text;
using System.Net.Sockets;

namespace Smasher.SmasherLib.Net
{
	public static class SmasherAddressUtil
	{
		public static IPEndPoint GetSmasherEndPoint (string address)
		{
			string[] addressParts = address.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
			if (addressParts.Length < 2)
				throw new FormatException("Invalid Smasher Address {0}, should be format ip:port");
			
			// Parse address
			IPAddress ipAddress = Dns.GetHostEntry(addressParts[0]).AddressList[0];
			if (ipAddress == null)
				throw new FormatException("Invalid Smasher Address {0}, should be format ip:port");
			
			// Parse port
			int port;
			if (!int.TryParse(addressParts[1], out port))
				throw new FormatException("Invalid Smasher Address {0}, should be format ip:port");
			
			return new IPEndPoint(ipAddress, port);
		}
		
		public static IPAddress GetIpAddress (AddressFamily family)
		{
			// TODO: Improve this
			IPHostEntry host = Dns.GetHostEntry("localhost");
			IPAddress address = null;
			foreach (IPAddress ip in host.AddressList)
			{
				if (ip.AddressFamily == family)
				{
					address = ip;
					break;
				}
			}
			
			// TODO: Find a better type of exception
			if (address == null)
				throw new Exception("Invalid AddressFamily");
			
			return address;
		}
	}
}

