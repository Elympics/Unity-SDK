#nullable enable
using System.Collections.Generic;

namespace GameEngineCore.V1._3
{
    public class InitialMatchUserDatas : List<InitialMatchUserData>
    {
        public InitialMatchUserDatas()
        { }

        public InitialMatchUserDatas(List<InitialMatchUserData> initialMatchData) : base(initialMatchData)
        { }
    }
}
