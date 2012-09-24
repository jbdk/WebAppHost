using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Dependencies;

namespace SampleApp.Startup
{
	/// <summary>
	/// Dependency scope implementation that releases individual components when disposes.
	/// </summary>
	/// <remarks>
	/// Implementation adapted from:
	/// http://nikosbaxevanis.com/2012/06/04/using-the-web-api-dependency-resolver-with-castle-windsor-part-2/
	/// </remarks>
	public class ReleasingDependencyScope : IDependencyScope
	{
		private readonly IDependencyScope _scope;
		private readonly Action<object> _release;
		private readonly List<object> _instances = new List<object>();

		public ReleasingDependencyScope(IDependencyScope scope, Action<object> release)
		{
			_scope = Verify.ArgumentNotNull(scope, "scope");
			_release = Verify.ArgumentNotNull(release, "release");
		}

		public void Dispose()
		{
			foreach (var instance in _instances)
			{
				// TODO: check for exception during release
				_release(instance);
			}
			_instances.Clear();
		}

		public object GetService(Type serviceType)
		{
			var service = _scope.GetService(serviceType);
			AddToScope(service);
			return service;
		}

		public IEnumerable<object> GetServices(Type serviceType)
		{
			var services = _scope.GetServices(serviceType);
			AddToScope(services);
			return services;
		}

		private void AddToScope(params object[] services)
		{
			if (services.Any())
			{
				_instances.AddRange(services);
			}
		}
	}
}
