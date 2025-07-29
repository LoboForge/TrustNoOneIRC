
    public class Node
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> Exits { get; set; } = new(); // e.g., "scan" => "firewall"
        public List<string> Actions { get; set; } = new(); // e.g., "probe", "download"
    }


