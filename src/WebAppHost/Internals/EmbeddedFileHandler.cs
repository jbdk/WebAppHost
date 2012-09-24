using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SignalR;

namespace WebAppHost.Internals
{
	public class EmbeddedFileHandler
	{
		private readonly string _pathPrefix;
		private readonly int _pathPrefixLength;
		private readonly Assembly _assembly;
		private readonly string _resourceNamePrefix;

		private readonly Dictionary<string, string> _resources =
			new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
		private readonly ConcurrentDictionary<string, string> _etags = new ConcurrentDictionary<string, string>();


		public EmbeddedFileHandler(string pathPrefix, Type type) : this(pathPrefix, type.Assembly, GetResourcePrefix(type))
		{
		}

		public EmbeddedFileHandler(string pathPrefix, Assembly assembly, string resourceNamePrefix)
		{
			_resourceNamePrefix = Verify.ArgumentNotNull(resourceNamePrefix, "resourceNamePrefix");
			_assembly = Verify.ArgumentNotNull(assembly, "assembly");
			Verify.ArgumentNotNull(pathPrefix, "pathPrefix");
			if (!pathPrefix.EndsWith("/"))
			{
				pathPrefix += "/";
			}
			_pathPrefix = pathPrefix;
			_pathPrefixLength = pathPrefix.Length;

			// this is necessary
			foreach (var resourceName in assembly.GetManifestResourceNames())
			{
				if (resourceName.StartsWith(resourceNamePrefix, StringComparison.InvariantCultureIgnoreCase))
				{
					_resources[resourceName] = resourceName;
				}
			}
		}

		public Task Handle(string path, HttpListenerContext context)
		{
			if (!path.StartsWith(_pathPrefix, StringComparison.InvariantCultureIgnoreCase))
			{
				return null;
			}

			var resourceNameSuffix = path.Substring(_pathPrefixLength).Replace("/", ".");
			var caseInsensitiveResourceName = _resourceNamePrefix + resourceNameSuffix;

			string resourceName;
			if (_resources.TryGetValue(caseInsensitiveResourceName, out resourceName))
			{
				var ifNoneMatch = context.Request.Headers.Get("If-None-Match");
				if (ifNoneMatch != null)
				{
					string etag;
					if (_etags.TryGetValue(resourceName, out etag))
					{
						if (etag == ifNoneMatch)
						{
							SetNotModified(context);
							return TaskAsyncHelper.Empty;
						}
					}
				}

				var contentType = GetContentType(resourceName);
				
				using (var stream = _assembly.GetManifestResourceStream(resourceName))
				{
					if (stream == null)
					{
						// should not happen
						SetNotFound(context);
					}
					else
					{
						
						var hashAlgorithm = MD5.Create();
						var memoryStream = new MemoryStream();
						var cryptoStream = new CryptoStream(stream, hashAlgorithm, CryptoStreamMode.Read);
						cryptoStream.CopyTo(memoryStream);
						var digest = hashAlgorithm.Hash;

						var sb = new StringBuilder(digest.Length * 2);

						foreach (byte b in digest)
						{
							sb.AppendFormat("{0:x2}", b);
						}

						var etag = sb.ToString();
						_etags[resourceName] = etag;

						context.Response.SendChunked = false;
						context.Response.ContentType = contentType;
						context.Response.ContentLength64 = memoryStream.Length;
						context.Response.AddHeader("ETag", etag);
						context.Response.AddHeader("Expires", DateTime.UtcNow.AddMonths(1).ToString("ddd, dd MMM yyyy HH:mm:ss") + " GMT");

						//context.Response.AddHeader("Expires", DateTime.UtcNow.AddMonths(1).ToString("ddd, dd MMM yyyy HH:mm:ss GMT"));
						//context.Response.AddHeader("ETag", etag);
						//memoryStream.Seek(0, SeekOrigin.Begin);

						// note that WriteTo is more efficient than CopyTo
						// See http://geekswithblogs.net/mknapp/archive/2011/10/23/writing-to-an-httplistenerresponse-output-stream.aspx
						memoryStream.WriteTo(context.Response.OutputStream);
						//context.Response.OutputStream.Close();
					}
				}
			}
			else
			{
				SetNotFound(context);
			}

			return TaskAsyncHelper.Empty;
		}

		private void SetNotFound(HttpListenerContext context)
		{
			const string content = "Not found";
			var bytes = Encoding.UTF8.GetBytes(content);
			context.Response.StatusCode = (int)HttpStatusCode.NotFound;
			context.Response.AddHeader("Content-Type", "text/plain");
			context.Response.AddHeader("Content-Length", bytes.Length.ToString(CultureInfo.InvariantCulture));
			context.Response.OutputStream.Write(bytes, 0, bytes.Length);
		}

		private void SetNotModified(HttpListenerContext context)
		{
			const string content = "Not modified";
			var bytes = Encoding.UTF8.GetBytes(content);
			context.Response.StatusCode = (int)HttpStatusCode.NotModified;
			context.Response.AddHeader("Content-Type", "text/plain");
			context.Response.AddHeader("Content-Length", bytes.Length.ToString(CultureInfo.InvariantCulture));
			context.Response.OutputStream.Write(bytes, 0, bytes.Length);
		}

		private static string GetResourcePrefix(Type type)
		{
			Verify.ArgumentNotNull(type, "type");
			var @namespace = type.Namespace;
			if (string.IsNullOrWhiteSpace(@namespace))
			{
				throw new ArgumentException("Type has  no namespace: " + type);
			}
			var resourcePrefix = @namespace + ".";
			return resourcePrefix;
		}

		private static string GetContentType(string name)
		{
			int pos = name.LastIndexOf('.');
			if (pos < 0)
			{
				// no trailing suffix
				return "application/octet-stream";
			}

			var suffix = name.Substring(pos).ToLowerInvariant();

			switch (suffix)
			{
				case ".js":
					return "text/javascript";
				case ".css":
					return "text/css";
				case ".htm":
				case ".html":
					return "text/html";
				case ".jpg":
				case ".jpeg":
					return "image/jpeg";
				case ".png":
					return "image/png";
				case ".gif":
					return "image/gif";
				case ".ico":
					return "image/x-icon";
				case ".bmp":
					return "image/bmp";
				case ".xml":
					return "application/xml";
				case ".json":
					return "application/json";
				default:
					// TODO: lookup mime type
					return "application/octet-stream";
			}
		}
	}
}
