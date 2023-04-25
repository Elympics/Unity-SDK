using System;
using UnityEngine;

namespace Elympics
{
    public abstract class ElympicsBlockchain : MonoBehaviour
    {
        public abstract void SendTransaction(string to, string from, string value, Action<BlockchainFeedbackData> onResponse);
    }
}
