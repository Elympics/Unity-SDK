using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Elympics
{
    [Obsolete("Use " + nameof(InitialMatchPlayerDatasGuid) + " instead")]
    public class InitialMatchPlayerDatas : List<InitialMatchPlayerData>
    {
        private InitialMatchPlayerDatas(IEnumerable<InitialMatchPlayerData> playerDatas) : base(playerDatas)
        { }

        public InitialMatchPlayerDatas(InitialMatchPlayerDatasGuid initialMatchPlayerDatasGuid)
            : this(initialMatchPlayerDatasGuid.Select(x => new InitialMatchPlayerData(x)).ToList())
        { }
    }
}
