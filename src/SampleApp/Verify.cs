using System;

namespace SampleApp
{
	internal static class Verify
	{
		public static T ArgumentNotNull<T>(T @object, string paramName) where T : class
		{
			if (@object == null)
			{
				throw new ArgumentNullException(paramName);
			}
			return @object;
		}
	}
}