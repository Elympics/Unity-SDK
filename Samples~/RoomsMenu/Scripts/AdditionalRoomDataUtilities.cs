using System;
using System.Text.RegularExpressions;

public static class AdditionalRoomDataUtilities
{
    public static readonly string DropdownEntryFormat = "Key: %$Key$% Value: $%Value$%";
    public static string GetDropdownEntry(string key, string value)
    {
        return DropdownEntryFormat.Replace("%$Key$%", key).Replace("$%Value$%", value);
    }
    public static string ExtractKeyValue(string formattedString)
    {
        var pattern = DropdownEntryFormat.Replace("%$Key$%", "(?<key>.+)").Replace("$%Value$%", "(?<value>.+)");

        var regex = new Regex(pattern);
        var match = regex.Match(formattedString);

        if (match.Success)
            return match.Groups["key"].Value;

        throw new ArgumentException("The formatted string does not match the expected format.");
    }
}
