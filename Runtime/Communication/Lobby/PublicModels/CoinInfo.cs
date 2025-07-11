#nullable enable
using System;
using UnityEngine;

namespace Elympics
{
    public readonly struct CoinInfo
    {
        public Guid Id { get; init; }
        public CurrencyInfo Currency { get; init; }
        public ChainInfo Chain { get; init; }
    }

    public readonly struct CurrencyInfo
    {
        public string Ticker { get; init; }
        public string? Address { get; init; }
        public int Decimals { get; init; }
        public Texture2D? Icon { get; init; }
    }

    public readonly struct ChainInfo
    {
        public string Type { get; init; }
        public string Name { get; init; }
        public int ExternalId { get; init; }
    }
}
