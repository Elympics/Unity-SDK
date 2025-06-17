#nullable enable

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Elympics.ElympicsSystems.Internal;
using UnityEngine;
using UnityEngine.Networking;

namespace Elympics.Communication.Mappers
{
    internal static class CoinIcons
    {
        private static readonly Dictionary<Guid, Texture2D> CachedIcons = new();

        internal static async UniTask<Texture2D?> GetIconOrNull(Guid coinId, string iconUrl, ElympicsLoggerContext logger)
        {
            if (CachedIcons.TryGetValue(coinId, out var icon))
                return icon;

            logger = logger.WithMethodName();

            try
            {
                using var request = await UnityWebRequestTexture.GetTexture(iconUrl).SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    icon = DownloadHandlerTexture.GetContent(request);
                    CachedIcons[coinId] = icon;
                    return icon;
                }

                logger.Error($"Failed to download an icon for coin with ID {coinId} from {iconUrl}. Reason: {request.error}");
                return null;
            }
            catch (Exception e)
            {
                logger.Exception(e);
                return null;
            }
        }
    }
}
