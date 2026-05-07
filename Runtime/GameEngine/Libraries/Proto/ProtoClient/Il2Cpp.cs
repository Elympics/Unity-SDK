using Google.Protobuf.WellKnownTypes;
using ProtoGameEngine;

namespace Proto
{
    public static class Il2Cpp
    {
        public static void ForceAotFix()
        {
            new Value().ClearKind();
            new ListValue().Values.Clear();
            new NullableGameEndedMsg().ClearKind();
        }
    }
}
