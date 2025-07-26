namespace LoboForge.TNOIRC.Models
{
    public class AlertRule
    {
        public string EventType { get; set; } = "PrivateMessage";
        public string? TargetUser { get; set; }
        public string? Channel { get; set; }
        public string SoundFile { get; set; } = "";
        public string ToastMessage { get; set; } = "Alert triggered!";
        public string? TriggerWord { get; set; } // NEW: Match on content
        public bool Enabled { get; set; } = true;
    }

}
