using System;

#nullable enable

namespace Elympics.Blockchain.TypedData
{
    [Serializable]
    public class Game
    {
        public string id = default!;
        public string name = default!;
        public string version_name = default!;
    }
}
