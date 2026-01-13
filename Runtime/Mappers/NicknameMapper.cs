using Elympics.Communication.Authentication.Models;
namespace Elympics.Mappers
{
    public static class NicknameMapper
    {
        public static NicknameType ConvertToNickNameType(string type)
        {
            return type switch
            {
                "Common" => NicknameType.Common,
                "Verified" => NicknameType.Verified,
                _ => NicknameType.Undefined
            };
        }
    };
}
