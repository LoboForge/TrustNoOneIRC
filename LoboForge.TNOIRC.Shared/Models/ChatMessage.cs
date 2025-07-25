namespace LoboForge.TNOIRC.Models
{
    public class ChatMessage
    {
        public DateTime Timestamp { get; set; }
        public IrcUser Sender { get; set; }
        public string Target { get; set; } // Channel or user
        public string Content { get; set; }
        public bool IsAction { get; set; } // For /me actions
        public ChatMessage(DateTime timestamp, IrcUser sender, string target, string content, bool isAction = false)
        {
            Timestamp = timestamp;
            Sender = sender;
            Target = target;
            Content = content;
            IsAction = isAction;
        }


    }
}
