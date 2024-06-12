using System;

#nullable enable

namespace SCS.InternalModels.Player
{
    [Serializable]
    internal class SetAllowanceErrorResult
    {
        //It has to be camelCase
        public string code;
        public string message;
        public string stack;

        public SetAllowanceErrorResult(string code, string message, string stack)
        {
            this.code = code;
            this.message = message;
            this.stack = stack;
        }

    }
}
