using System;
using System.Collections.Generic;
using System.Linq;

namespace Elympics
{
    internal static class ElympicsPlayerAssociations
    {
        internal static Dictionary<string, ElympicsPlayer> GetUserIdsToPlayers(IEnumerable<string> userIds) =>
            userIds.Select((x, i) => new KeyValuePair<string, ElympicsPlayer>(x, ElympicsPlayer.FromIndex(i)))
                .ToDictionary(x => x.Key, x => x.Value);

        internal static Dictionary<Guid, ElympicsPlayer> GetUserIdsToPlayers(IEnumerable<Guid> userIds) =>
            userIds.Select((x, i) => new KeyValuePair<Guid, ElympicsPlayer>(x, ElympicsPlayer.FromIndex(i)))
                .ToDictionary(x => x.Key, x => x.Value);

        internal static Dictionary<ElympicsPlayer, string> GetPlayersToUserIds(IEnumerable<string> userIds) =>
            userIds.Select((x, i) => new KeyValuePair<ElympicsPlayer, string>(ElympicsPlayer.FromIndex(i), x))
                .ToDictionary(x => x.Key, x => x.Value);
    }
}
