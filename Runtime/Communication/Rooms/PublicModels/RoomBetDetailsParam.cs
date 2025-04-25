using System;
using JetBrains.Annotations;

namespace Elympics.Communication.Rooms.PublicModels
{
    [PublicAPI]
    public struct RoomBetAmount
    {
        public decimal BetValue;
        public Guid CoinId;
    }
}
