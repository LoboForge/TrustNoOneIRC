using System.Net;
using LoboForge.TNOIRC.Models;

public class AppConfig
{
    public string? TorExecutablePath { get; set; }
    public int TorSocksPort { get; set; } = 9050;
    public List<IrcServerProfile> Servers { get; set; } = new() {
        new IrcServerProfile { Name = "Default", Host = "irc.libera.chat", Port = 6697 }
    };
    public List<AutoReply> AutoReplies { get; set; } = new();
    public List<AlertRule> AlertRules { get; set; } = new();
}
