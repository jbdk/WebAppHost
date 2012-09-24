using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using WebAppHost;

namespace HttpServer
{
	internal class WebApiServer : System.Web.Http.HttpServer
	{
		private readonly WebAppConfiguration _config;

		public WebApiServer(WebAppConfiguration config)
			: base(config)
		{
			_config = config;
		}

		/// <summary>
		/// Provides access to the protected method <see cref="HttpServer.SendAsync"/>.
		/// </summary>
		internal Task<HttpResponseMessage> PublicSendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			return SendAsync(request, cancellationToken);
		}
	}
}
