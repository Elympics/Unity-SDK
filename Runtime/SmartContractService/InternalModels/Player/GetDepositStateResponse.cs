using System;
using System.Collections.Generic;

namespace SCS.InternalModels.Player
{
    [Serializable]
    public class GetDepositStateResponse
    {
        public string ChainId;
        public List<DepositState> Deposits;
    }
}
