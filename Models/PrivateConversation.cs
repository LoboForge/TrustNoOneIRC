namespace LoboForge.TNOIRC.Models
{
    public class PrivateConversation
    {
        public IrcUser User { get; set; }
        public List<ChatMessage> Messages { get; set; } = new();
        public DateTime LastChecked { get; set; } = DateTime.MinValue;

        public bool HasUnread => Messages.Any(c => c.Timestamp > LastChecked);
        public int UnreadCount => Messages.Count(c => c.Timestamp > LastChecked);

        public PrivateConversation(IrcUser user)
        {
            User = user;
        }
    }
}
