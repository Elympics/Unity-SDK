using System;

[Serializable]
public class GetRespectResponse
{
    public string MatchId;
    public long Respect;
    public GetRespectResponse(string matchId, long respect)
    {
        MatchId = matchId;
        Respect = respect;
    }
}
