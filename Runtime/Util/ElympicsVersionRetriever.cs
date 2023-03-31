using System;
using System.Linq;

namespace Elympics
{
	public static class ElympicsVersionRetriever
	{
		private const string ElympicsName = "elympics";

		public static Version GetVersionFromAssembly()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			return assemblies.Select(x => x.GetName())
				.FirstOrDefault(x => x.Name.ToLowerInvariant() == ElympicsName)?
				.Version;
		}

		public static string GetVersionStringFromAssembly()
		{
			return GetVersionFromAssembly()?.ToString(3) ?? string.Empty;
		}
	}
}
