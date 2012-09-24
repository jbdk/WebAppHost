using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RazorEngine.Templating;

namespace SampleApp.Startup
{
	public class CustomTemplateBase<T> : TemplateBase<T>
	{
		private readonly ScriptHelper _scriptHelper;

		public CustomTemplateBase()
		{
			_scriptHelper = new ScriptHelper(this);
		}

		public ScriptHelper Scripts
		{
			get { return _scriptHelper; }
		}

		public string TemplateName
		{
			get { return GetType().FullName; }
		}
	}
}
