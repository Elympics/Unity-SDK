#nullable enable

using System;
using JetBrains.Annotations;

namespace Elympics
{
    [PublicAPI]
    public readonly struct TournamentFeeInfo
    {
        public FeeInfo[] Fees { get; init; }
    }


    [PublicAPI]
    public readonly struct FeeInfo
    {
        public Guid RollingTournamentId { get; init; }
        public string? EntryFeeRaw { get; init; }
        public decimal? EntryFee { get; init; }
        public string? Error { get; init; }
    }
}
