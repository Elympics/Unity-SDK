using System.Text.RegularExpressions;
#if UNITY_EDITOR
using ParrelSync;

#endif

namespace Plugins.Elympics.Plugins.ParrelSync
{
	public static class ElympicsClonesManager
	{
		private const string BotArgument = "bot";

		public static bool IsClone()
		{
#if UNITY_EDITOR
			return ClonesManager.IsClone();
#else
			return false;
#endif
		}

		public static int GetCloneNumber()
		{
#if UNITY_EDITOR
			var path = ClonesManager.GetCurrentProjectPath();
			return int.Parse(Regex.Match(path, @"\d+$").Value);
#else
			return 0;
#endif
		}

		public static bool IsBot()
		{
#if UNITY_EDITOR
			var arg = ClonesManager.GetArgument();
			return arg == BotArgument;
#else
			return false;
#endif
		}
	}
}
