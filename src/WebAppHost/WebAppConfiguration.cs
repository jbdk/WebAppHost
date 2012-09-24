using System;
using System.Web.Http;

namespace WebAppHost
{
	public class WebAppConfiguration : HttpConfiguration
	{

		public WebAppConfiguration(Uri baseAddress)
			: base(new HttpRouteCollection(baseAddress.AbsolutePath))
		{
			BaseAddress = baseAddress;
		}

		public Uri BaseAddress { get; private set; }
	}
}

