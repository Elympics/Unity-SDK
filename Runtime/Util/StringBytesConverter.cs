using System;
using System.Text;

namespace Elympics
{
    public static class StringBytesConverter
    {
        public static string BytesToString(byte[] bytes) => Encoding.ASCII.GetString(bytes);
        public static byte[] StringToBytes(string str) => Encoding.ASCII.GetBytes(str);
        public static byte[] Base64UrlDecode(this string input)
        {
            var output = input;
            output = output
                .Replace('-', '+')
                .Replace('_', '/');
            switch (output.Length % 4)
            {
                case 0:
                    break;
                case 2:
                    output += "==";
                    break;
                case 3:
                    output += "=";
                    break;
                default:
                    throw new Exception("Illegal base64url string!");
            }
            var converted = Convert.FromBase64String(output);
            return converted;
        }
    }
}
