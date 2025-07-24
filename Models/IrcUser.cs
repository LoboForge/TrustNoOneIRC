namespace LoboForge.TNOIRC.Models;

/// <summary>
/// Represents an IRC user with nickname, username, and hostname.
/// </summary>
public class IrcUser
{
    public string Nickname { get; set; }
    public string Username { get; set; }
    public string Hostname { get; set; }

    public IrcUser(string nickname, string username = "", string hostname = "")
    {
        Nickname = nickname;
        Username = username;
        Hostname = hostname;
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

        return new IrcUser(nick, user, host);

    }


    public override string ToString() => $"{Nickname}!{Username}@{Hostname}";
}
