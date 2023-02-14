using System;
using System.Linq;

namespace Elympics
{
	public static class ElympicsVersionRetriever
	{
		private static string _elympicsName = "Elympics";

		public static Version GetVersionFromAssembly()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			return assemblies.Select(x => x.GetName()).FirstOrDefault(x => x.Name == _elympicsName)?.Version;
		}

		public static string GetVersionStringFromAssembly()
		{
			return GetVersionFromAssembly()?.ToString(3) ?? string.Empty;
		}
	}
}
