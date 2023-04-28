using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Elympics
{
    public static class ElympicsRegions
    {
        public static readonly string TelAviv = "telaviv";
        public static readonly string Warsaw  = "warsaw";
        public static readonly string Tokyo   = "tokyo";

        public static readonly List<string> AllAvailableRegions = new List<string>()
        {
            TelAviv, Warsaw, Tokyo
        };
    }
}
