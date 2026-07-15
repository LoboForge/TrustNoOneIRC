namespace LoboForge.TNOIRC.Services;

using LoboForge.TNOIRC.Commands;

public static class IrcSlashCommandService
{
    public static async Task<bool> TryHandleAsync(string input, string? currentChannel = null)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith('/'))
            return false;

        var trimmed = input.Trim();
        var spaceIndex = trimmed.IndexOf(' ');
        var command = spaceIndex > 0 ? trimmed[..spaceIndex].ToLowerInvariant() : trimmed.ToLowerInvariant();
        var args = spaceIndex > 0 ? trimmed[(spaceIndex + 1)..].Trim() : string.Empty;

        if (!Common.IsConnected)
            return true;

        switch (command)
        {
            case "/join":
            case "/j":
                await Common.ircClient.JoinChannelAsync(NormalizeChannel(args));
                return true;

            case "/part":
            case "/leave":
                if (string.IsNullOrWhiteSpace(args))
                    args = currentChannel ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(args))
                    await Common.ircClient.PartChannelAsync(NormalizeChannel(args));
                return true;

            case "/msg":
            case "/query":
                var msgParts = SplitTargetMessage(args);
                if (msgParts != null)
                    await Common.ircClient.SendMessageAsync(msgParts.Value.Target, msgParts.Value.Message);
                return true;

            case "/me":
                if (!string.IsNullOrWhiteSpace(currentChannel) && !string.IsNullOrWhiteSpace(args))
                    await Common.ircClient.SendMessageAsync(currentChannel, $"\x01ACTION {args}\x01");
                return true;

            case "/nick":
                if (!string.IsNullOrWhiteSpace(args))
                    await Common.ircClient.ChangeNickAsync(args.Split(' ')[0]);
                return true;

            case "/topic":
                if (string.IsNullOrWhiteSpace(args))
                {
                    if (!string.IsNullOrWhiteSpace(currentChannel))
                        await Common.ircClient.RequestTopicAsync(currentChannel);
                }
                else if (!string.IsNullOrWhiteSpace(currentChannel))
                {
                    await Common.ircClient.SetTopicAsync(currentChannel, args);
                }
                return true;

            case "/whois":
                if (!string.IsNullOrWhiteSpace(args))
                    await Common.ircClient.WhoisAsync(args.Split(' ')[0]);
                return true;

            case "/names":
                if (!string.IsNullOrWhiteSpace(currentChannel))
                    await Common.ircClient.RequestNamesAsync(currentChannel);
                return true;

            case "/mode":
                if (!string.IsNullOrWhiteSpace(args))
                    await Common.ircClient.SendRawAsync($"MODE {args}");
                return true;

            case "/kick":
                if (!string.IsNullOrWhiteSpace(currentChannel) && !string.IsNullOrWhiteSpace(args))
                {
                    var kickParts = args.Split(' ', 2);
                    await Common.ircClient.KickAsync(currentChannel, kickParts[0], kickParts.Length > 1 ? kickParts[1] : null);
                }
                return true;

            case "/raw":
            case "/quote":
                if (!string.IsNullOrWhiteSpace(args))
                    await Common.ircClient.SendRawAsync(args);
                return true;

            case "/quit":
                await Common.ircClient.DisconnectAsync(args, reconnect: false);
                return true;

            default:
                return false;
        }
    }

    private static string NormalizeChannel(string channel)
    {
        channel = channel.Trim();
        if (channel.StartsWith('#') || channel.StartsWith('&'))
            return channel;
        return "#" + channel;
    }

    private static (string Target, string Message)? SplitTargetMessage(string args)
    {
        var space = args.IndexOf(' ');
        if (space <= 0)
            return null;
        return (args[..space], args[(space + 1)..]);
    }
}
