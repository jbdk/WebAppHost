using System;
using System.Web.Http;
using Castle.Windsor;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using SampleApp.Scripts;
using SampleApp.Startup;
using SampleApp.Views;
using WebAppHost;

namespace SampleApp
{
	public class SampleServer
	{
		private const string BaseAddress = "http://+:8655/";

		private readonly WebAppServer _server;
		private readonly IWindsorContainer _container;

		public SampleServer()
		{
			_container = WindsorConfiguration.CreateContainer();
			_server = CreateServer();
		}

		public void Start()
		{
			_server.Start();
		}

		public void Stop()
		{
			_server.Stop();
		}

		private WebAppServer CreateServer()
		{
			var resolver = new WindsorDependencyResolver(_container);
			var server = new WebAppServer(BaseAddress);
			server.HttpConfiguration.DependencyResolver = resolver;
			server.HttpConfiguration.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
			server.HttpConfiguration.Routes.MapHttpRoute(
				name: "DefaultAPI",
				routeTemplate: "api/{controller}/{id}",
				defaults: new { id = RouteParameter.Optional });
			server.HttpConfiguration.Routes.MapHttpRoute(
				name: "Default",
				routeTemplate: "{controller}/{action}",
				defaults: new { controller = "Home", action = "Index" });
			//server.HttpConfiguration.MessageHandlers.Add(new FaviconHandler());

			server.StaticFiles.Add("/Scripts", typeof (ScriptsLocator));
			server.HttpConfiguration.MessageHandlers.Add(new StaticFileHandler("Scripts", "text/javascript"));

			var templateConfiguration = new TemplateServiceConfiguration();
			templateConfiguration.Resolver = new EmbeddedTemplateResolver(typeof(ViewResourceLocator));
			templateConfiguration.BaseTemplateType = typeof(CustomTemplateBase<>);
			Razor.SetTemplateService(new TemplateService(templateConfiguration));

			return server;
		}
	}
}
