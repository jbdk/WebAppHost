using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using RazorEngine.Templating;

namespace SampleApp.Startup
{
	public class EmbeddedTemplateResolver : ITemplateResolver
	{
		private readonly ConcurrentDictionary<string, string> _resourceNameCache = new ConcurrentDictionary<string, string>();
		private readonly List<string> _resourceNames = new List<string>();
		private readonly Assembly _assembly;
		private readonly string _baseNamespace;

		/// <summary>
		/// Finds embedded resource templates from the name.
		/// </summary>
		/// <param name="locator">
		/// Type used to determine the assembly and the base namespace for finding
		/// the embedded resource.
		/// </param>
		public EmbeddedTemplateResolver(Type locator)
		{
			Verify.ArgumentNotNull(locator, "locator");
			_assembly = locator.Assembly;

			var name = locator.Name;
			var fullName = locator.FullName ?? name;
			_baseNamespace = fullName.Substring(0, fullName.Length - name.Length);

			foreach (var resourceName in _assembly.GetManifestResourceNames())
			{
				if (resourceName.StartsWith(_baseNamespace, true, CultureInfo.InvariantCulture))
				{
					_resourceNames.Add(resourceName);
				}
			}
		}

		private const string FileSuffix = ".cshtml";

		public string Resolve(string name)
		{
			var resourceName = GetResourceName(name);

			if (resourceName == null)
			{
				throw new ArgumentException("Cannot resolve view: " + name);
			}

			using (var stream = _assembly.GetManifestResourceStream(resourceName))
			{
				// We should not get an NRE here, because resourceName is guaranteed
				// to be a valid resource name
				// ReSharper disable AssignNullToNotNullAttribute
				using (var reader = new StreamReader(stream, Encoding.UTF8))
				// ReSharper restore AssignNullToNotNullAttribute
				{
					return reader.ReadToEnd();
				}
			}
		}

		private string GetResourceName(string name)
		{
			string resourceName;
			if (!_resourceNameCache.TryGetValue(name, out resourceName))
			{
				var normalizedName = name;

				// strip off the ".cshtml" file suffix
				if (normalizedName.EndsWith(FileSuffix, true, CultureInfo.InvariantCulture))
				{
					normalizedName = normalizedName.Substring(0, name.Length - FileSuffix.Length);
				}

				resourceName = GetResourceNameHelper(normalizedName);
				if (resourceName == null)
				{
					normalizedName = "Shared." + normalizedName;
					resourceName = GetResourceNameHelper(normalizedName);
				}

				// it is possible that with multiple threads, this could be set twice,
				// so do not use the Add() method, because this would throw an exception 
				_resourceNameCache[name] = resourceName;
			}

			return resourceName;
		}

		private string GetResourceNameHelper(string name)
		{
			var resourceName = _baseNamespace + name + FileSuffix;
			return _resourceNames.FirstOrDefault(item => item.Equals(resourceName, StringComparison.InvariantCultureIgnoreCase));
		}
	}
}