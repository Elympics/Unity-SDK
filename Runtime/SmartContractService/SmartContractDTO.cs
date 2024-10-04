using System;

#nullable enable

namespace SCS
{
    [Serializable]
    public struct SmartContractDTO
    {
        public string Type;
        public string Address;
        public string ABI;

        public override string ToString() => $"{nameof(Type)}:{Type}, {nameof(Address)}:{Address} | {nameof(ABI)}:{ABI}";
    }
}
