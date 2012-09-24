using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace WebAppHost.Internals
{
	/// <summary>
	/// Extension methods for <see cref="HttpListenerContext"/> for conversion to <see cref="HttpRequestMessage"/>
	/// and <see cref="HttpResponseMessage"/>.
	/// </summary>
	public static class HttpListenerContextExtensions
	{
		/// <summary>
		/// Creates a <see cref="HttpRequestMessage"/> from the information in a <see cref="HttpListenerContext"/> object.
		/// </summary>
		public static HttpRequestMessage GetHttpRequestMessage(this HttpListenerContext listenerContext)
		{
			var listenerRequest = listenerContext.Request;
			var method = new HttpMethod(listenerRequest.HttpMethod);
			var requestMessage = new HttpRequestMessage(method, listenerRequest.Url);

			// TODO: work out whether to buffer input or not
			var inputStream = listenerRequest.InputStream;
			if (inputStream == null)
			{
				inputStream = new MemoryStream();
			}
			requestMessage.Content = new StreamContent(inputStream);

			foreach (string headerName in listenerRequest.Headers.AllKeys)
			{
				var headerValues = listenerRequest.Headers.GetValues(headerName);

				// Not sure why we do this. Got the idea from System.Web.Http.WebHost.HttpControllerHandler class
				if (!requestMessage.Headers.TryAddWithoutValidation(headerName, headerValues))
				{
					requestMessage.Content.Headers.TryAddWithoutValidation(headerName, headerValues);
				}
			}

			// TODO: should use constants for these strings

			// Add context to enable route lookup later on
			requestMessage.Properties.Add("HttpListenerContext", listenerContext);

			// Add the retrieve client certificate delegate to the property bag to enable lookup later on
			requestMessage.Properties.Add("MS_RetrieveClientCertificateDelegate", RetrieveClientCertificateCallback);

			// Add information about whether the request is local or not
			requestMessage.Properties.Add("MS_IsLocal", new Lazy<bool>(() => listenerRequest.IsLocal));

			// Add information about whether custom errors are enabled for this request or not
			requestMessage.Properties.Add("MS_IncludeErrorDetail", new Lazy<bool>(() => listenerRequest.IsLocal));

			return requestMessage;
		}

		/// <summary>
		/// Send the HTTP response to the client. All response information is defined in a <see cref="HttpResponseMessage"/>.
		/// </summary>
		public static Task SendResponseAsync(this HttpListenerContext context, HttpResponseMessage responseMessage)
		{
			context.Response.StatusCode = (int) responseMessage.StatusCode;
			foreach (var pair in responseMessage.Headers)
			{
				var headerName = pair.Key;
				var headerValues = pair.Value;
				context.Response.Headers.Set(headerName, string.Join(",", headerValues));
			}
			return responseMessage.Content.CopyToAsync(context.Response.OutputStream);
		}

		private static readonly Func<HttpRequestMessage, X509Certificate2> RetrieveClientCertificateCallback =
			RetrieveClientCertificate;

		private static X509Certificate2 RetrieveClientCertificate(HttpRequestMessage request)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}

			X509Certificate2 result = null;

			object httpListenerContextObject;

			if (request.Properties.TryGetValue("HttpListenerContext", out httpListenerContextObject))
			{
				var httpListenerContext = (HttpListenerContext) httpListenerContextObject;
				result = httpListenerContext.Request.GetClientCertificate();
			}

			return result;
		}
	}
}