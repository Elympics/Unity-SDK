using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

#nullable enable

namespace Elympics
{
    public abstract class ElympicsEthSigner : MonoBehaviour, IEthSigner
    {
        private static readonly Regex EthAddressRegex = new("^(0x)?[0-9a-f]{40}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly BigInteger MaxUint256 = new(Enumerable.Repeat<byte>(255, 32).ToArray(), true);
        private static readonly Dictionary<char, string> EscapedChars = new()
        {
            { '"', @"\""" },
            { '\\', @"\\" },
            { '/', @"\/" },
            { '\r', @"\r" },
            { '\n', @"\n" },
            { '\b', @"\b" },
            { '\f', @"\f" },
            { '\t', @"\t" },
        };

        public virtual BigInteger ChainId
        {
            get => _chainId;
            set
            {
                if (value > MaxUint256 || value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Chain ID must be in uint256 range.");
                _chainId = value;
            }
        }
        private BigInteger _chainId = 1;

        public virtual string ProvideTypedData(string nonce)
        {
            var currentGame = ElympicsConfig.LoadCurrentElympicsGameConfig()
                ?? throw new InvalidOperationException("Current game config not found.");
            if (!EthAddressRegex.IsMatch(Address))
                throw new FormatException($"{nameof(Address)} must be a hex-encoded 20-byte value.");
            var gameId = new Guid(currentGame.gameId);
            var gameName = new string(currentGame.gameName.SelectMany(x => EscapedChars.GetValueOrDefault(x, x.ToString())).ToArray());
            var gameVersion = new string(currentGame.gameVersion.SelectMany(x => EscapedChars.GetValueOrDefault(x, x.ToString())).ToArray());
            return $@"{{
                ""types"": {{
                    ""EIP712Domain"": [
                        {{ ""name"": ""name"", ""type"": ""string"" }},
                        {{ ""name"": ""version"", ""type"": ""string"" }},
                        {{ ""name"": ""chainId"", ""type"": ""uint256"" }},
                        {{ ""name"": ""salt"", ""type"": ""string"" }}
                    ],
                    ""Login"": [
                        {{ ""name"": ""player"", ""type"": ""address"" }},
                        {{ ""name"": ""nonce"", ""type"": ""string"" }},
                        {{ ""name"": ""game"", ""type"": ""Game"" }}
                    ],
                    ""Game"": [
                        {{ ""name"": ""id"", ""type"": ""string"" }},
                        {{ ""name"": ""name"", ""type"": ""string"" }},
                        {{ ""name"": ""version_name"", ""type"": ""string"" }}
                    ]
                }},
                ""primaryType"": ""Login"",
                ""domain"": {{
                    ""name"": ""Elympics:GameLogin"",
                    ""version"": ""1"",
                    ""chainId"": {ChainId},
                    ""salt"": ""0xba563d8547226082eed52739eb13ea7c3900e3cf7598574603d218579bc52d52""
                }},
                ""message"": {{
                    ""player"": ""{Address}"",
                    ""game"": {{
                        ""id"": ""{gameId}"",
                        ""name"": ""{gameName}"",
                        ""version_name"": ""{gameVersion}""
                    }},
                    ""nonce"": ""{nonce}""
                }}
            }}";
        }

        public abstract string Address { get; }
        public abstract UniTask<string> SignAsync(string message, CancellationToken ct = default);
    }
}
