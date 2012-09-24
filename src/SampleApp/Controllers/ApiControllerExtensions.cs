using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using RazorEngine;
using RazorEngine.Templating;
using Encoding = System.Text.Encoding;

namespace SampleApp.Controllers
{
	public static class ApiControllerExtensions
	{
		public static HttpResponseMessage View(this ApiController @this, string viewName, object model = null)
		{
			const string controllerNameSuffix = "Controller";
			var controllerName = @this.GetType().Name;
			if (controllerName.EndsWith(controllerNameSuffix))
			{
				// Strip the suffix off the controller name.
				controllerName = controllerName.Substring(0, controllerName.Length - controllerNameSuffix.Length);
			}

			var prefix = controllerName + ".";

			if (!viewName.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
			{
				viewName = prefix + viewName;
			}

			var text = Razor.Resolve(viewName, model).Run(new ExecuteContext());

			var response = new HttpResponseMessage(HttpStatusCode.OK);
			response.Content = new StringContent(text, Encoding.UTF8, "text/html");
			return response;
		}
	}
}
