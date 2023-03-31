using System;
using System.Collections;
using System.Collections.Generic;
using Elympics;
using UnityEngine;

namespace Elympics
{
    public abstract class ElympicsBlockchain : MonoBehaviour
    {
        public abstract void SendTransaction(string to, string from, string value, Action<BlockchainFeedbackData> onResponse);
    }
}
