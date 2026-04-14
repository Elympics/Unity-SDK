#nullable enable
using System;
using System.Collections.Generic;

namespace GameEngineCore.V1._1
{
    [Obsolete("Use newer version from V1.3")]
    public class InitialMatchData : List<UserData>
    {
        public InitialMatchData()
        { }

        public InitialMatchData(List<UserData> initialMatchData) : base(initialMatchData)
        { }
    }
}
