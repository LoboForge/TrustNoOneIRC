using LoboForge.TNOIRC.Models;

public class ChatMessage
{
    public DateTime Timestamp { get; set; }
    public IrcUser Sender { get; set; }
    public string Target { get; set; }
    public string Content { get; set; }
    public bool IsAction { get; set; }
    public bool IsNotice { get; set; }   // <-- Add this

    public ChatMessage(DateTime timestamp, IrcUser sender, string target, string content, bool isAction)
    {
        Timestamp = timestamp;
        Sender = sender;
        Target = target;
        Content = content;
        IsAction = isAction;
        IsNotice = false;
    }
}
