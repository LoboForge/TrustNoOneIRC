namespace LoboForge.TNOIRC.Models;

public static class IrcNickPrefix
{
    public static readonly char[] ModeChars = { '~', '&', '@', '%', '+' };

    public static (string Prefix, string Nick) Parse(string rawNick)
    {
        var prefix = string.Empty;
        var index = 0;
        while (index < rawNick.Length && ModeChars.Contains(rawNick[index]))
        {
            prefix += rawNick[index];
            index++;
        }

        return (prefix, rawNick[index..]);
    }
}
