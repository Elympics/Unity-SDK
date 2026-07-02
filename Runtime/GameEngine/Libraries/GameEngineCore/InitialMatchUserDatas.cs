#nullable enable
using System.Collections.Generic;

namespace GameEngineCore
{
    public class InitialMatchUserDatas : List<InitialMatchUserData>
    {
        public InitialMatchUserDatas()
        { }

        public InitialMatchUserDatas(List<InitialMatchUserData> initialMatchData) : base(initialMatchData)
        { }
    }
}
