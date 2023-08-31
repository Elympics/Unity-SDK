using System.Collections.Generic;

namespace Elympics
{
    public static class ElympicsRegions
    {
        public const string Warsaw = "warsaw";
        public const string Tokyo = "tokyo";
        public const string Dallas = "dallas";
        public const string Mumbai = "mumbai";

        public static readonly List<string> AllAvailableRegions = new()
        {
            Warsaw,
            Tokyo,
            Dallas,
            Mumbai,
        };

        // unavailable regions
        internal const string Taiwan = "taiwan";
        internal const string HongKong = "hongkong";
        internal const string Osaka = "osaka";
        internal const string Seoul = "seoul";
        internal const string Delhi = "delhi";
        internal const string Singapore = "singapore";
        internal const string Jakarta = "jakarta";
        internal const string Sydney = "sydney";
        internal const string Melbourne = "melbourne";
        internal const string Finland = "finland";
        internal const string Belgium = "belgium";
        internal const string London = "london";
        internal const string Frankfurt = "frankfurt";
        internal const string Netherlands = "netherlands";
        internal const string Zurich = "zurich";
        internal const string Milan = "milan";
        internal const string Paris = "paris";
        internal const string Madrid = "madrid";
        internal const string TelAviv = "telaviv";
        internal const string Montreal = "montreal";
        internal const string Toronto = "toronto";
        internal const string SaoPaulo = "saopaulo";
        internal const string Santiago = "santiago";
        internal const string Iowa = "iowa";
        internal const string SouthCarolina = "southcarolina";
        internal const string NorthVirginia = "northvirginia";
        internal const string Columbus = "columbus";
        internal const string Oregon = "oregon";
        internal const string LosAngeles = "losangeles";
        internal const string SaltLakeCity = "saltlakecity";
        internal const string LasVegas = "lasvegas";
    }
}
