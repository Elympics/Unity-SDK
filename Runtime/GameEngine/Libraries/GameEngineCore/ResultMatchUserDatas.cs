#nullable enable
using System.Collections.Generic;

namespace GameEngineCore
{
    public class ResultMatchUserDatas : List<ResultMatchUserData>
    {
        public ResultMatchUserDatas()
        { }

        public ResultMatchUserDatas(List<ResultMatchUserData> matchResult) : base(matchResult)
        { }
    }
}
