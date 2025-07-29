using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class NetworkMap
{
    private readonly Dictionary<string, Node> nodes = new();

    public NetworkMap(string dataPath = "BuiltinBots/Data/Nodes.json")
    {
        if (File.Exists(Path.Combine(dataPath)))
        {
            var json = File.ReadAllText(dataPath);
            var loadedNodes = JsonSerializer.Deserialize<List<Node>>(json) ?? new();
            foreach (var n in loadedNodes)
                nodes[n.Id] = n;
        }
    }

    public Node? GetNode(string id) => nodes.TryGetValue(id, out var node) ? node : null;
    public IEnumerable<Node> AllNodes => nodes.Values;
}
