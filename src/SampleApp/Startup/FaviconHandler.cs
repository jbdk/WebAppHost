using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;

namespace SampleApp.Startup
{
	public class FaviconHandler : DelegatingHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
		{
			if (request.Method == HttpMethod.Get
				&& request.RequestUri.AbsolutePath == "/favicon.ico")
			{
				var task = new Task<HttpResponseMessage>(() => GetFavicoResponse(request));
				task.Start();
				return task;
			}
			return base.SendAsync(request, cancellationToken);
		}

		private HttpResponseMessage GetFavicoResponse(HttpRequestMessage request)
		{
			var response = request.CreateResponse();
			response.Content = new StreamContent(GetFavicoStream());
			response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/x-icon");
			return response;
		}

		private Stream GetFavicoStream()
		{
			var filePath = Assembly.GetEntryAssembly().CodeBase;
			var icon = Icon.ExtractAssociatedIcon(filePath);
			if (icon == null)
			{
				return null;
			}
			
			var type = typeof(Program);
			var assembly = type.Assembly;
			return assembly.GetManifestResourceStream(type, "favicon.ico");
		}
	}
}
