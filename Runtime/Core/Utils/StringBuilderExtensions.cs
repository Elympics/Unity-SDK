using System.Globalization;
using System.Text;

#nullable enable

namespace Elympics.Core.Utils
{
    internal static class StringBuilderExtensions
    {
        public static StringBuilder AppendJsonEscaped(this StringBuilder sb, string? value)
        {
            if (value is null)
            {
                _ = sb.Append("null");
                return sb;
            }

            _ = sb.Append('"');
            foreach (var c in value)
                if (c is '"' or '\\' or '\n' or '\r' or '\t' or '\b' or '\f' or < ' ')
                    _ = sb.Append(c switch
                    {
                        '"' => @"\""",
                        '\\' => @"\\",
                        '\n' => @"\n",
                        '\r' => @"\r",
                        '\t' => @"\t",
                        '\b' => @"\b",
                        '\f' => @"\f",
                        _ => $"\\u{(int)c:x4}",
                    });
                else
                    _ = sb.Append(c);

            return sb.Append('"');
        }

        public static StringBuilder AppendJsonProperty(this StringBuilder sb, string key, string? value, bool isFirst = false)
        {
            if (!isFirst)
                _ = sb.Append(',');
            return sb.AppendJsonEscaped(key)
                .Append(':')
                .AppendJsonEscaped(value);
        }

        public static StringBuilder AppendJsonProperty(this StringBuilder sb, string key, int value, bool isFirst = false)
        {
            if (!isFirst)
                _ = sb.Append(',');
            return sb.AppendJsonEscaped(key)
                .Append(':')
                .Append(value.ToString());
        }

        public static StringBuilder AppendJsonProperty(this StringBuilder sb, string key, double value, bool isFirst = false)
        {
            if (!isFirst)
                _ = sb.Append(',');
            return sb.AppendJsonEscaped(key)
                .Append(':')
                .Append(value.ToString(CultureInfo.InvariantCulture));
        }

        public static StringBuilder AppendJsonProperty(this StringBuilder sb, string key, bool value, bool isFirst = false)
        {
            if (!isFirst)
                _ = sb.Append(',');
            return sb.AppendJsonEscaped(key)
                .Append(':')
                .Append(value ? "true" : "false");
        }
    }
}
