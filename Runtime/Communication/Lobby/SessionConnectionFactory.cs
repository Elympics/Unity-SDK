using System;
using Elympics.Lobby;
using Elympics.Models.Authentication;

namespace Elympics
{
    internal class SessionConnectionFactory
    {
        private readonly IRegionValidator _regionValidator;
        public SessionConnectionFactory(IRegionValidator regionValidator) => _regionValidator = regionValidator;

        public SessionConnectionDetails CreateSessionConnectionDetails(string url, AuthData authData, ElympicsGameConfig gameConfig, RegionData? regionData)
        {
            var gameId = new Guid(gameConfig.GameId);
            var gameVersion = gameConfig.gameVersion;

            if (regionData.HasValue is false)
                return new SessionConnectionDetails(url, authData, gameId, gameVersion, string.Empty);

            if (_regionValidator.IsRegionValid(regionData.Value))
                return new SessionConnectionDetails(url, authData, gameId, gameVersion, regionData.Value.Name);

            throw new ElympicsException($"The specified region \"{regionData.Value.Name}\" must be one of the available regions {string.Join(" | ", _regionValidator.GetAvailableRegions)}");
        }
    }
}
