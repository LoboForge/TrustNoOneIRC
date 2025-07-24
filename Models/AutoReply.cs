namespace TNO.mIRC.Models
{
    public class AutoReply
    {
        public string Trigger { get; set; } = string.Empty;
        public string Reply { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public bool PrivateMessages { get; set; } = true;
        public bool ChannelMessages { get; set; } = true;
        public double MinDelay { get; set; } = 0;
        public double MaxDelay { get; set; } = 0;
    }
}
