using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace Elympics
{
    public static class ElympicsRegions
    {
        public const string Warsaw = "warsaw";
        public const string Tokyo = "tokyo";
        public const string Dallas = "dallas";
        public const string Mumbai = "mumbai";
        public const string Taiwan = "taiwan";
        public const string HongKong = "hongkong";
        public const string Osaka = "osaka";
        public const string Seoul = "seoul";
        public const string Delhi = "delhi";
        public const string Singapore = "singapore";
        public const string Jakarta = "jakarta";
        public const string Sydney = "sydney";
        public const string Melbourne = "melbourne";
        public const string Finland = "finland";
        public const string Belgium = "belgium";
        public const string London = "london";
        public const string Frankfurt = "frankfurt";
        public const string Netherlands = "netherlands";
        public const string Zurich = "zurich";
        public const string Milan = "milan";
        public const string Paris = "paris";
        public const string Madrid = "madrid";
        public const string TelAviv = "telaviv";
        public const string Montreal = "montreal";
        public const string Toronto = "toronto";
        public const string SaoPaulo = "saopaulo";
        public const string Santiago = "santiago";
        public const string Iowa = "iowa";
        public const string SouthCarolina = "southcarolina";
        public const string NorthVirginia = "northvirginia";
        public const string Columbus = "columbus";
        public const string Oregon = "oregon";
        public const string LosAngeles = "losangeles";
        public const string SaltLakeCity = "saltlakecity";
        public const string LasVegas = "lasvegas";

        public static async UniTask<List<string>> GetAvailableRegions()
        {
            var builder = new UriBuilder(ElympicsConfig.Load()!.ElympicsApiEndpoint);
            builder.Path += "/" + ElympicsApiModels.ApiModels.Regions.Routes.BaseRouteUnityFormat;
            var completionSource = new UniTaskCompletionSource<AvailableRegionsResponseModel>();
            var url = builder.Uri.ToString();
            ElympicsWebClient.SendGetRequest<AvailableRegionsResponseModel>(url, null, null, OnResponse);

            var result = await completionSource.Task;
            return result.Regions.Select(x => x.Name).ToList();

            void OnResponse(Result<AvailableRegionsResponseModel, Exception> responseModels)
            {
                if (responseModels.IsSuccess)
                    _ = completionSource.TrySetResult(responseModels.Value);
                else
                {
                    ElympicsLogger.LogError(responseModels.Error.ToString());
                    _ = completionSource.TrySetResult(null);
                }
            }
        }

        [Obsolete("Call" + nameof(GetAvailableRegions) + "for updated list of available regions.")]
        public static readonly List<string> AllAvailableRegions = new()
        {
            Warsaw,
            Tokyo,
            Dallas,
            Mumbai
        };
    }
}
