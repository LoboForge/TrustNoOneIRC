using System.Text.Json;
using LoboForge.TNOIRC.Models;

namespace LoboForge.TNOIRC.Services;

public static class MessageLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public static string LogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LoboForge.TNOIRC", "logs");

    public static void AppendMessage(string channelKey, ChatMessage message)
    {
        try
        {
            var path = GetLogPath(channelKey);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var existing = LoadMessages(channelKey);
            existing.Add(message);
            if (existing.Count > 500)
                existing.RemoveRange(0, existing.Count - 500);

            var payload = existing.Select(m => new StoredMessage
            {
                Timestamp = m.Timestamp,
                SenderNick = m.Sender.Nick,
                Target = m.Target,
                Content = m.Content,
                IsAction = m.IsAction,
                IsNotice = m.IsNotice
            }).ToList();

            File.WriteAllText(path, JsonSerializer.Serialize(payload, JsonOptions));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MessageLog] Failed to persist: {ex.Message}");
        }
    }

    public static List<ChatMessage> LoadMessages(string channelKey)
    {
        try
        {
            var path = GetLogPath(channelKey);
            if (!File.Exists(path))
                return new List<ChatMessage>();

            var json = File.ReadAllText(path);
            var stored = JsonSerializer.Deserialize<List<StoredMessage>>(json, JsonOptions) ?? new();
            return stored.Select(s => new ChatMessage(
                s.Timestamp,
                new IrcUser(s.SenderNick),
                s.Target,
                s.Content,
                s.IsAction)
            {
                IsNotice = s.IsNotice
            }).ToList();
        }
        catch
        {
            return new List<ChatMessage>();
        }
    }

    public static void HydrateChannel(IrcChannel channel)
    {
        var history = LoadMessages(channel.Name);
        if (history.Count == 0)
            return;

        channel.Messages.Clear();
        channel.Messages.AddRange(history);
    }

    private static string GetLogPath(string channelKey)
    {
        var safeName = string.Concat(channelKey.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        return Path.Combine(LogDirectory, $"{safeName}.json");
    }

    private sealed class StoredMessage
    {
        public DateTime Timestamp { get; set; }
        public string SenderNick { get; set; } = "";
        public string Target { get; set; } = "";
        public string Content { get; set; } = "";
        public bool IsAction { get; set; }
        public bool IsNotice { get; set; }
    }
}
