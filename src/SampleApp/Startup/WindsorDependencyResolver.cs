using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Dependencies;
using Castle.Windsor;

namespace SampleApp.Startup
{
	/// <summary>
	/// Implementation of Web API <see cref="IDependencyResolver"/> that uses Castle Windsor.
	/// </summary>
	/// <remarks>
	/// Implementation adapted from:
	/// http://nikosbaxevanis.com/2012/06/04/using-the-web-api-dependency-resolver-with-castle-windsor-part-2/
	/// </remarks>
	public class WindsorDependencyResolver : IDependencyResolver
	{
		private readonly IWindsorContainer _container;

		public WindsorDependencyResolver(IWindsorContainer container)
		{
			_container = Verify.ArgumentNotNull(container, "container");
		}

		public void Dispose()
		{
		}

		public object GetService(Type serviceType)
		{
			return _container.Kernel.HasComponent(serviceType) ? _container.Resolve(serviceType) : null;
		}

		public IEnumerable<object> GetServices(Type serviceType)
		{
			return _container.ResolveAll(serviceType).Cast<object>().ToArray();
		}

		public IDependencyScope BeginScope()
		{
			return new ReleasingDependencyScope(this, _container.Release);
		}
	}
}