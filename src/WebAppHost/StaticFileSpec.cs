using System;
using System.Collections.Generic;
using System.Reflection;
using WebAppHost.Internals;

namespace WebAppHost
{
	public class StaticFileSpec
	{
		public readonly string PathPrefix;
		public readonly string ResourcePrefix;
		public readonly Assembly ResourceAssembly;
		public readonly Type ResourceLocatorType;

		public StaticFileSpec(string pathPrefix, Type type)
		{
			PathPrefix = Verify.ArgumentNotNull(pathPrefix, "pathPrefix");
			ResourceLocatorType = Verify.ArgumentNotNull(type, "type");
		}

		public StaticFileSpec(string pathPrefix, Assembly assembly, string resourcePrefix)
		{
			PathPrefix = Verify.ArgumentNotNull(pathPrefix, "pathPrefix");
			ResourceAssembly = Verify.ArgumentNotNull(assembly, "assembly");
			ResourcePrefix = Verify.ArgumentNotNull(resourcePrefix, "resourcePrefix");
		}
	}

	public class StaticFileSpecCollection : List<StaticFileSpec>
	{
		public void Add(string pathPrefix, Type type)
		{
			Add(new StaticFileSpec(pathPrefix, type));
		}

		public void Add(string pathPrefix, Assembly assembly, string resourcePrefix)
		{
			Add(new StaticFileSpec(pathPrefix, assembly, resourcePrefix));
		}
	}
}