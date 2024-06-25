using System;

#nullable enable

namespace Elympics.Blockchain.TypedData
{
    [Serializable]
    public class Login
    {
        public byte[] player = default!;  // 20-byte address
        public string nonce = default!;
        public Game game = default!;
    }
}
