using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace SampleApp.Controllers
{
	public class HomeController : ApiController 
	{
		[AcceptVerbs("GET")]
		public HttpResponseMessage Index()
		{
			var model = new { Name = "Home" };
			return this.View("Index", model);
		}
	}
}
