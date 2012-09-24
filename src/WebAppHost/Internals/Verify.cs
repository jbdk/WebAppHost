using System;

namespace WebAppHost.Internals
{
	internal class Verify
	{
		public static T ArgumentNotNull<T>(T param, string paramName) where T : class
		{
			if (param == null)
			{
				throw new ArgumentNullException(paramName);
			}
			return param;
		}
	}
}