namespace LoboForge.TNOIRC.Models;

/// <summary>
/// Represents an IRC user with nickname, username, and hostname.
/// </summary>
public class IrcUser
{
    public string Nick { get; set; }
    public string Username { get; set; }
    public string Hostname { get; set; }
    public string Host { get; set; }
    public string Prefix { get; set; } = "";

    public string DisplayNick => $"{Prefix}{Nick}";

    public IrcUser(string nickname, string username = "", string hostname = "", string host = "", string prefix = "")
    {
        Nick = nickname;
        Username = username;
        Hostname = hostname;
        Host = host;
        Prefix = prefix;
    }

    public static IrcUser FromPrefix(string prefix)
    {
        // Format: nick!user@host
        var nick = prefix;
        var user = "";
        var host = "";

        var bangIndex = prefix.IndexOf('!');
        var atIndex = prefix.IndexOf('@');

        if (bangIndex > 0)
        {
            nick = prefix.Substring(0, bangIndex);
            if (atIndex > bangIndex)
            {
                user = prefix.Substring(bangIndex + 1, atIndex - bangIndex - 1);
                host = prefix.Substring(atIndex + 1);
            }
        }

        return new IrcUser(nick, user, host, host);
    }


    public override string ToString() => $"{Nick}!{Username}@{Hostname}";
}
public static class IrcUserExtensions
{
    public static bool HostIsIP(this IrcUser user)
    {
        var host = !string.IsNullOrWhiteSpace(user.Hostname) ? user.Hostname : user.Host;
        return System.Net.IPAddress.TryParse(host, out _);
    }
}