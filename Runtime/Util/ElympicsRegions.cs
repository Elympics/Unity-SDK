using System.Collections.Generic;

namespace Elympics
{
    public static class ElympicsRegions
    {
        public const string Warsaw = "warsaw";
        public const string Tokyo = "tokyo";
        public const string Dallas = "dallas";

        public static readonly List<string> AllAvailableRegions = new()
        {
            Warsaw,
            Tokyo,
            Dallas,
        };
    }
}
