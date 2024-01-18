namespace SCS
{
    public struct SendTransactionWalletRequest
    {
        public string From;
        public string To;
        public string Data;

        public SendTransactionWalletRequest(string from, string to, string data)
        {
            From = from;
            To = to;
            Data = data;
        }

        //TODO: setting new allowance value requires us to decode Data using ABI, setting newValue and Encoding it once again. k.pieta 27.11.2023
    }
}
