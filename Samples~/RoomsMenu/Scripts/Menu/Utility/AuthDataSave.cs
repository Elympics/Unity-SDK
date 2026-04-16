using System;
using Elympics.Models.Authentication;
[Serializable]
public class AuthDataSave
{
    public AuthDataSave(string id, string jwToken, string nickname, AuthType type)
    {
        this.id = id;
        this.jwToken = jwToken;
        this.nickname = nickname;
        this.type = type;
    }
    public string id;
    public string jwToken;
    public string nickname;
    public AuthType type;

}
