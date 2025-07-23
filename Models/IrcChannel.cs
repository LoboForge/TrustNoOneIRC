namespace TNO.mIRC.Models
{
    public class IrcChannel
    {
        public string Name { get; set; } = "";
        public int UserCount { get; set; } 
        public string Topic { get; set; } = "";
        public bool IsJoined { get; set; } = false;
        public int UnreadCount { get { return Messages.Count(c => c.Timestamp > LastChecked); } }
        public DateTime LastChecked { get; set; } = DateTime.MinValue;
        public bool HasUnread { get { return UnreadCount > 0; } }
        public List<IrcUser> Users { get; set; } = new List<IrcUser>();
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public override string ToString() => $"{Name} ({UserCount} users) - Topic: {Topic}";
    }


}
