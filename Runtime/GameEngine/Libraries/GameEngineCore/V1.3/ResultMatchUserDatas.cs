#nullable enable
using System.Collections.Generic;

namespace GameEngineCore.V1._3
{
    public class ResultMatchUserDatas : List<ResultMatchUserData>
    {
        public ResultMatchUserDatas()
        { }

        public ResultMatchUserDatas(List<ResultMatchUserData> matchResult) : base(matchResult)
        { }
    }
}
