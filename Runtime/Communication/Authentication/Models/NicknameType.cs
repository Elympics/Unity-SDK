namespace Elympics.Communication.Authentication.Models
{
    public enum NicknameType
    {
        /// <summary>Unexpected and invalid value that indicates an error in the Elympics systems.</summary>
        Undefined = 0,
        /// <summary>Regular, non-verified username.</summary>
        Common = 1,
        /// <summary>Special status for users who have verified their username.</summary>
        Verified = 2,
    }
}
