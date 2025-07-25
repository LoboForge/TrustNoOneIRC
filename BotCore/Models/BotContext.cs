namespace BotCore.Models;

public class BotContext
{
    public string User { get; set; } = "";
    public string Channel { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
