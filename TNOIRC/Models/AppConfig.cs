using System.Net;
using LoboForge.TNOIRC.Models;

public class AppConfig
{
    public string TorExecutablePath { get; set; } = "tor/tor.exe";
    public List<IrcServerProfile> Servers { get; set; } = new() {
        new IrcServerProfile { Name = "Default", Host = "irc.libera.chat", Port = 6697 }
    };
    public List<AutoReply> AutoReplies { get; set; } = new();
}
