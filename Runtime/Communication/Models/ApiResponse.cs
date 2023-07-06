using System;

namespace Elympics.Models
{
    [Serializable]
    public class ApiResponse
    {
        public string ErrorMessage;

        public bool IsSuccess => ErrorMessage == null;
    }
}
