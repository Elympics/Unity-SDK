using System;
using System.Linq;

namespace Elympics
{
	public static class CurrentGameVersionUploadedToTheCloudStatus
	{
		private static bool isCheckingGameVersion = false;

		private static ElympicsGameConfig activeGameConfig = null;

		public static bool IsVersionUploaded { get; private set; } = false;

		public static event Action<bool> CheckingIfGameVersionIsUploadedChanged = null;

		public static void Initialize(ElympicsGameConfig _config)
		{
			if (_config != activeGameConfig)
			{
				activeGameConfig = _config;

				activeGameConfig.DataChanged += RequestCheckIfCurrentGameVersionUploadedToCloud;
				ElympicsWebIntegration.GameUploadedToTheCloud += RequestCheckIfCurrentGameVersionUploadedToCloud;
			}

			RequestCheckIfCurrentGameVersionUploadedToCloud();
		}

		public static void Disable()
		{
			activeGameConfig.DataChanged -= RequestCheckIfCurrentGameVersionUploadedToCloud;
			ElympicsWebIntegration.GameUploadedToTheCloud -= RequestCheckIfCurrentGameVersionUploadedToCloud;

			activeGameConfig = null;
		}

		private static void RequestCheckIfCurrentGameVersionUploadedToCloud()
		{
			if (!isCheckingGameVersion)
			{
				isCheckingGameVersion = true;
				CheckingIfGameVersionIsUploadedChanged?.Invoke(isCheckingGameVersion);
			}

			ElympicsWebIntegration.GetGameVersions((gameVersions) =>
			{
				var gameVersionUploaded = gameVersions.Versions.Any(x => string.Equals(x.Version, activeGameConfig.gameVersion));

				isCheckingGameVersion = false;
				CheckingIfGameVersionIsUploadedChanged?.Invoke(isCheckingGameVersion);

				IsVersionUploaded = gameVersionUploaded;
			});
		}
	}
}
