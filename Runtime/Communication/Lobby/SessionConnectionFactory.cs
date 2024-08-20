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
                return new SessionConnectionDetails(url, authData, gameId, gameVersion, string.Empty, false);

            if (_regionValidator.IsRegionValid(regionData.Value))
                return new SessionConnectionDetails(url, authData, gameId, gameVersion, regionData.Value.Name, regionData.Value.IsCustom);

            throw new ElympicsException($"The specified region must be one of the available regions listed in {nameof(ElympicsRegions.AllAvailableRegions)}.");
        }
    }
}
