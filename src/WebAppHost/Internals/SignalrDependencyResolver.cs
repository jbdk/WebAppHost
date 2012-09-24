using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;

namespace WebAppHost.Internals
{
	/// <summary>
	/// Provides a dependency resolver for SignalR that uses the Web API dependency resolver class.
	/// </summary>
	public class SignalrDependencyResolver : SignalR.DefaultDependencyResolver
	{
		private readonly IDependencyResolver _webapiDependencyResolver;

		public SignalrDependencyResolver(IDependencyResolver dependencyResolver)
		{
			_webapiDependencyResolver = dependencyResolver;
		}

		public override object GetService(Type serviceType)
		{
			if (_webapiDependencyResolver == null)
			{
				return base.GetService(serviceType);
			}

			// Relies on the web api resolver returning null if it cannot resolve.
			return _webapiDependencyResolver.GetService(serviceType) ?? base.GetService(serviceType);
		}

		public override IEnumerable<object> GetServices(Type serviceType)
		{
			if (_webapiDependencyResolver == null)
			{
				return base.GetServices(serviceType);
			}

			// not sure if the web api resolver will return null or an empty enumerable,
			// so handle both cases
			var services = _webapiDependencyResolver.GetServices(serviceType);
			if (services == null)
			{
				return base.GetServices(serviceType);
			}

			// create a list so that we can count the number of objects returned
			// and still return an enumerable to our caller
			var list = new List<object>(services);
			if (list.Count == 0)
			{
				return base.GetServices(serviceType);
			}

			return list;
		}
	}
}
