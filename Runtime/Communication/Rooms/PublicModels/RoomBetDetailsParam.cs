using System;
using JetBrains.Annotations;

namespace Elympics.Communication.Rooms.PublicModels
{
    [PublicAPI]
    public struct RoomBetDetailsParam
    {
        public decimal BetValue;
        public Guid CoinId;
    }
}
