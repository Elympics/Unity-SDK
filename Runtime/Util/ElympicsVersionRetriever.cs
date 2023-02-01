using System;
using System.Linq;

namespace Elympics
{
	public static class ElympicsVersionRetriever
	{
		private static string _elympicsName = "Elympics";

		public static string GetVersionFromAssembly()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			return assemblies.Select(x => x.GetName()).FirstOrDefault(x => x.Name == _elympicsName)?.Version?.ToString(3) ?? string.Empty;
		}
	}
}
