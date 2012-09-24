using System;
using System.Text;
using RazorEngine.Templating;
using RazorEngine.Text;

namespace SampleApp.Startup
{
	public class ScriptHelper
	{
		private readonly TemplateBase _template;

		public ScriptHelper(TemplateBase template)
		{
			_template = Verify.ArgumentNotNull(template, "template");
		}

		// IEncodedString is the RazorEngine equivalent of IHtmlString in System.Web
		// (RazorEngine cannot use it, because it does not want a dependency on System.Web).
		public IEncodedString Render(params string[] scriptNames)
		{
			var sb = new StringBuilder();

			foreach (var scriptName in scriptNames)
			{
				sb.AppendFormat("<script src='{0}' type='text/javascript'></script>", PathUtils.Expand(scriptName));
				sb.AppendLine();
			}

			return new RawString(sb.ToString());
		}
	}

	public static class PathUtils
	{
		private static string _basePath = "/";

		public static void SetBasePath(string basePath)
		{
			if (string.IsNullOrWhiteSpace(basePath))
			{
				basePath = "/";
			}

			basePath = basePath.Trim();
			if (basePath != "/")
			{
				while (basePath.EndsWith("/"))
				{
					basePath = basePath.Substring(0, basePath.Length - 1);
				}
			}

			_basePath = basePath;
		}

		public static string Expand(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				path = "/";
			}

			string expandedPath;

			if (path.StartsWith("~/"))
			{
				if (_basePath == "/")
				{
					expandedPath = path.Substring(1);
				}
				else
				{
					expandedPath = _basePath + path.Substring(1);
				}
			}
			else
			{
				expandedPath = path;
			}
			return expandedPath;
		}
	}
}
