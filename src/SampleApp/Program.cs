using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampleApp
{
	class Program
	{
		static void Main(string[] args)
		{
			var server = new SampleServer();
			server.Start();
			Console.WriteLine("Started, press enter to stop");
			Console.ReadLine();
			server.Stop();
		}
	}
}
