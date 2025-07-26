namespace LoboForge.TNOIRC.Models
{
    public class AutoReply
    {
        public string Trigger { get; set; } = string.Empty;
        public string Reply { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;

        public bool PrivateMessages { get; set; } = false;
        public bool ChannelMessages { get; set; } = false;

        public string? TargetChannel { get; set; }
        public string? SenderNick { get; set; }

        public double MinDelay { get; set; } = 0.5;
        public double MaxDelay { get; set; } = 1.5;
    }

}
