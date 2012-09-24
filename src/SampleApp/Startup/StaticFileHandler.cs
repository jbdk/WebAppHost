using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SampleApp.Startup
{
	public class StaticFileHandler : DelegatingHandler
	{
		private readonly string _basePath;
		private readonly string _resourceNamePrefix;
		private readonly string _contentType;

		public StaticFileHandler(string folderName, string contentType)
		{
			Verify.ArgumentNotNull(folderName, "folderName");
			_contentType = Verify.ArgumentNotNull(contentType, "contentType");
			if (!Regex.IsMatch(folderName, @"^[A-Za-z][a-zA-Z0-9]*$"))
			{
				throw new ArgumentException("Incorrect format for folder name. Contain alpha-numerics only.");
			}

			// ReSharper disable PossibleNullReferenceException
			var topLevelNamespace = (typeof(Program).FullName).Split('.')[0];
			// ReSharper restore PossibleNullReferenceException
			_resourceNamePrefix = topLevelNamespace + "." + folderName + ".";
			_basePath = "/" + folderName + "/";
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
		{
			if (request.Method == HttpMethod.Get
				&& request.RequestUri.AbsolutePath.StartsWith(_basePath, true, CultureInfo.InvariantCulture))
			{
				var task = new Task<HttpResponseMessage>(() => GetStaticFileResponse(request));
				task.Start();
				return task;
			}
			return base.SendAsync(request, cancellationToken);
		}

		private HttpResponseMessage GetStaticFileResponse(HttpRequestMessage request)
		{
			var stream = GetEmbeddedFileStream(request);
			var response = request.CreateResponse();
			if (stream == null)
			{
				response.StatusCode = HttpStatusCode.NotFound;
				response.Content = new StringContent("File not found: " + request.RequestUri.AbsolutePath);
				response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
			}
			else
			{
				response.Content = new StreamContent(stream);
				response.Content.Headers.ContentType = new MediaTypeHeaderValue(_contentType);
			}
			return response;
		}

		private Stream GetEmbeddedFileStream(HttpRequestMessage request)
		{
			var pathParts = request.RequestUri.AbsolutePath.Split('/');
			var fileName = pathParts[pathParts.Length - 1];
			var resourceName = _resourceNamePrefix + fileName;
			var assembly = Assembly.GetExecutingAssembly();
			var stream = assembly.GetManifestResourceStream(resourceName);
			return stream;
		}
	}
}
