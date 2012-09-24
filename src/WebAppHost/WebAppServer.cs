using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HttpServer;
using SignalR;
using SignalR.Hosting.Common;
using SignalR.Hosting.Self;
using SignalR.Hosting.Self.Infrastructure;
using WebAppHost.Internals;

namespace WebAppHost
{
	/// <summary>
	/// Combined self-host server for ASP.NET Web API and SignalR
	/// </summary>
	public class WebAppServer
	{
		private readonly Regex _urlRegex;
		private readonly HttpListener _listener;
		private readonly DisconnectHandler _disconnectHandler;
		private WebApiServer _webApiServer;
		private readonly WebAppConfiguration _webAppConfiguration;
		private RoutingHost _routingHost;
		private List<EmbeddedFileHandler> _embeddedFileHandlers = new List<EmbeddedFileHandler>(); 

		/// <summary>
		/// Create an instance of <see cref="WebAppServer"/>.
		/// </summary>
		/// <param name="urlReservation">
		/// The URL reservation to listen on. This string is similar to a URL, but the
		/// hostname may be a strong wildcard ('+') or a weak wildcard ('*'). E.g. "http://+:8080/".
		/// </param>
		public WebAppServer(string urlReservation)
		{
			_urlRegex = new Regex("^" + urlReservation.Replace("*", ".*?").Replace("+", ".*?"), RegexOptions.IgnoreCase);
			_listener = new HttpListener();
			_listener.Prefixes.Add(urlReservation);
			_disconnectHandler = new DisconnectHandler(_listener);
			var uri = new Uri(urlReservation.Replace("*", "localhost").Replace("+", "localhost"));
			_webAppConfiguration = new WebAppConfiguration(uri);
			StaticFiles = new StaticFileSpecCollection();
		}

		public WebAppConfiguration HttpConfiguration
		{
			get { return _webAppConfiguration; }
		}

		public AuthenticationSchemes AuthenticationSchemes
		{
			get { return _listener.AuthenticationSchemes; }
			set { _listener.AuthenticationSchemes = value; }
		}

		public StaticFileSpecCollection StaticFiles { get; private set; } 

		public Action<HostContext> OnProcessRequest { get; set; }

		/// <summary>
		/// Starts the server connection.
		/// </summary>
		public void Start()
		{
			_embeddedFileHandlers.Clear();
			foreach (var staticFile in StaticFiles)
			{
				if (staticFile.ResourcePrefix == null)
				{
					var handler = new EmbeddedFileHandler(staticFile.PathPrefix, staticFile.ResourceLocatorType);
					_embeddedFileHandlers.Add(handler);
				}
				else
				{
					var handler = new EmbeddedFileHandler(staticFile.PathPrefix, staticFile.ResourceAssembly, staticFile.ResourcePrefix);
					_embeddedFileHandlers.Add(handler);
				}
			}

			_listener.Start();

			_disconnectHandler.Initialize();
			_webApiServer = new WebApiServer(_webAppConfiguration);

			// create a signalr routing host -- its dependency resolver uses the web api resolver
			_routingHost = new RoutingHost(new SignalrDependencyResolver(_webAppConfiguration.DependencyResolver));
			_routingHost.MapHubs();
			ReceiveLoop();
		}

		/// <summary>
		/// Stops the server.
		/// </summary>
		public void Stop()
		{
			_listener.Stop();
		}

		private void ReceiveLoop()
		{
			_listener.BeginGetContext(ar =>
			{
				HttpListenerContext context;
				try
				{
					context = _listener.EndGetContext(ar);
				}
				catch (Exception)
				{
					return;
				}

				ReceiveLoop();

				// Process the request async
				ProcessRequestAsync(context).ContinueWith(task =>
				{
					if (task.IsFaulted)
					{
						Exception ex = task.Exception.GetBaseException();
						context.Response.ServerError(ex).Catch();

						Debug.WriteLine(ex.Message);
					}

					context.Response.CloseSafe();
				});

			}, null);
		}

		private Task ProcessRequestAsync(HttpListenerContext context)
		{
			try
			{
				Debug.WriteLine("Server: Incoming request to {0}.", context.Request.Url);

				PersistentConnection connection;

				string path = ResolvePath(context.Request.Url);

				foreach (var embeddedFileHandler in _embeddedFileHandlers)
				{
					var result = embeddedFileHandler.Handle(path, context);
					if (result != null)
					{
						return result;
					}
				}

				if (_routingHost.TryGetConnection(path, out connection))
				{
					// https://developer.mozilla.org/En/HTTP_Access_Control
					string origin = context.Request.Headers["Origin"];
					if (!String.IsNullOrEmpty(origin))
					{
						context.Response.AddHeader("Access-Control-Allow-Origin", origin);
						context.Response.AddHeader("Access-Control-Allow-Credentials", "true");
					}

					var request = new HttpListenerRequestWrapper(context);
					var response = new HttpListenerResponseWrapper(context.Response, _disconnectHandler.GetDisconnectToken(context));
					var hostContext = new HostContext(request, response);

#if NET45
                    hostContext.Items[HostConstants.SupportsWebSockets] = Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 2;
#endif

					if (OnProcessRequest != null)
					{
						OnProcessRequest(hostContext);
					}

#if DEBUG
					hostContext.Items[HostConstants.DebugMode] = true;
#endif
					hostContext.Items["System.Net.HttpListenerContext"] = context;

					// Initialize the connection
					connection.Initialize(_routingHost.DependencyResolver);

					return connection.ProcessRequestAsync(hostContext);
				}

				if (path.Equals("/clientaccesspolicy.xml", StringComparison.InvariantCultureIgnoreCase))
				{
					using (var stream = typeof(WebAppServer).Assembly.GetManifestResourceStream(typeof(WebAppServer), "clientaccesspolicy.xml"))
					{
						if (stream == null)
						{
							var response = new HttpResponseMessage(HttpStatusCode.NotFound);
							return context.SendResponseAsync(response);
						}
						var bytes = new byte[1024];
						int byteCount = stream.Read(bytes, 0, bytes.Length);
						return context.Response.WriteAsync(new ArraySegment<byte>(bytes, 0, byteCount));
					}
				}

				HttpRequestMessage requestMessage = context.GetHttpRequestMessage();

				return _webApiServer.PublicSendAsync(requestMessage, _disconnectHandler.GetDisconnectToken(context))
					.Then(response =>
					{
						var responseMessage = response ?? new HttpResponseMessage(HttpStatusCode.InternalServerError) { RequestMessage = requestMessage };
						return context.SendResponseAsync(responseMessage);
					});
			}
			catch (Exception ex)
			{
				return TaskAsyncHelper.FromError(ex);
			}
		}

		private string ResolvePath(Uri url)
		{
			string baseUrl = url.GetComponents(UriComponents.Scheme | UriComponents.HostAndPort | UriComponents.Path, UriFormat.SafeUnescaped);

			Match match = _urlRegex.Match(baseUrl);
			if (!match.Success)
			{
				throw new InvalidOperationException("Unable to resolve path");
			}

			string path = baseUrl.Substring(match.Value.Length);
			if (!path.StartsWith("/"))
			{
				return "/" + path;
			}

			return path;
		}


	}
}
