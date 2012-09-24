using System;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Http.Tracing;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using SignalR.Hubs;

namespace SampleApp.Startup
{
	public class WindsorConfiguration
	{
		public static IWindsorContainer CreateContainer()
		{
			var container = new WindsorContainer();
			container.AddFacility<StartableFacility>();
			container.Install(FromAssembly.InThisApplication());

			container.Register(
				AllTypes.FromThisAssembly()
					.BasedOn<ApiController>()
					.LifestyleTransient());
			container.Register(
				AllTypes.FromThisAssembly()
					.BasedOn<IHub>()
					.LifestyleSingleton());
			container.Register(
				Component.For<ITraceWriter>()
					.ImplementedBy<TraceWriter>()
					.LifestyleSingleton());

			container.Register(
				AllTypes.FromThisAssembly()
					.Pick().If(Component.IsCastleComponent));
			return container;
		}
	}

	public class TraceWriter : ITraceWriter
	{
		public bool IsEnabled(string category, TraceLevel level)
		{
			return true;
		}

		public void Trace(HttpRequestMessage request, string category, TraceLevel level, Action<TraceRecord> traceAction)
		{
			var record = new TraceRecord(request, category, level);
			traceAction(record);

			var sb = new StringBuilder();
			if (record.Request != null)
			{
				if (record.Request.Method != null)
				{
					sb.Append(" ");
					sb.Append(request.Method.ToString());
				}
				if (record.Request.RequestUri != null)
				{
					sb.Append(" ");
					sb.Append(record.Request.RequestUri.ToString());
				}
				if (!string.IsNullOrWhiteSpace(record.Category))
				{
					sb.Append(" ");
					sb.Append(record.Category);
				}
				sb.Append(" ");
				sb.Append(record.Message);

				if (record.Exception != null)
				{
					sb.Append(record.Exception.ToString());
				}
			}
			Console.WriteLine(sb.ToString());
		}
	}
}