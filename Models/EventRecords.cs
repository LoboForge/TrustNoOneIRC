

using LoboForge.TNOIRC.Models;

/// <summary>
/// Marker interface for all IRC event types.
/// </summary>
public interface IIrcEvent { }

public class RequestPrivateMessageEvent
{
    public IrcUser Target { get; }

    public RequestPrivateMessageEvent(IrcUser target)
    {
        Target = target;
    }
}

/// <summary>
/// Raised when a private or channel message is received.
/// </summary>
public class MessageReceivedEvent
{
    public IrcUser User { get; }
    public string Target { get; }
    public string Message { get; }

    public MessageReceivedEvent(IrcUser user, string target, string message)
    {
        User = user;
        Target = target;
        Message = message;
    }
}

/// <summary>
/// Raised when the client joins a channel.
/// </summary>
public class ChannelJoinedEvent : IIrcEvent
{
    public string Channel { get; set; }
}

/// <summary>
/// Raised when the client leaves a channel.
/// </summary>
public class ChannelLeftEvent : IIrcEvent
{
    public string Channel { get; set; }
}

/// <summary>
/// Raised when the topic of a channel is received.
/// </summary>
public record ChannelTopicEvent(string Channel, string? Topic);


/// <summary>
/// Raised when the user list of a channel is received.
/// </summary>
public class ChannelUsersEvent : IIrcEvent
{
    public string Channel { get; set; }
    public List<string> Users { get; set; } = new();
}

/// <summary>
/// Raised when a user joins the channel.
/// </summary>
public class UserJoinedEvent
{
    public IrcUser User { get; }
    public string Channel { get; }

    public UserJoinedEvent(IrcUser user, string channel)
    {
        User = user;
        Channel = channel;
    }
}

/// <summary>
/// Raised when a user leaves the channel.
/// </summary>
public class UserLeftEvent : IIrcEvent
{
    public string Channel { get; set; }
    public string Username { get; set; }
}

/// <summary>
/// Raised when the server sends a welcome message.
/// </summary>
public class WelcomeEvent : IIrcEvent
{
    public string Message { get; set; }
}

/// <summary>
/// Raised for raw, unhandled messages.
/// </summary>
public class RawMessageEvent : IIrcEvent
{
    public string RawLine { get; set; }
}
public class UserPartedEvent
{
    public IrcUser User { get; }
    public string Channel { get; }
    public string Reason { get; }

    public UserPartedEvent(IrcUser user, string channel, string reason)
    {
        User = user;
        Channel = channel;
        Reason = reason;
    }
}
public class NameReplyEvent
{
    public string Channel { get; }
    public List<string> Users { get; }

    public NameReplyEvent(string channel, IEnumerable<string> users)
    {
        Channel = channel;
        Users = users.ToList();
    }
}
public record EndOfNamesEvent(string channel);
public class TopicReplyEvent
{
    public string Channel { get; }
    public string Topic { get; }

    public TopicReplyEvent(string channel, string topic)
    {
        Channel = channel;
        Topic = topic;
    }
}
public class ConnectionCompletedEvent
{
    public string WelcomeMessage { get; }

    public ConnectionCompletedEvent(string welcomeMessage)
    {
        WelcomeMessage = welcomeMessage;
    }
}
public record ChannelListReceivedEvent(List<IrcChannel> Channels);
public record SelfJoinedChannelEvent(IrcUser User, string Channel);

public record ChannelCreatedEvent(string ChannelName, IrcUser Creator);

public record ServerMessage(string mesage);
public record UserQuitEvent(IrcUser User, string Reason);
public record UserNickChangedEvent(IrcUser OldUser, string NewNick);
public record NoticeReceivedEvent(IrcUser Sender, string Target, string Message);
public record ModeChangedEvent(string Target, string Mode, string[] Arguments);
public record ErrorEvent(string Reason);
public record EndOfListEvent();
public record PingReceivedEvent(string Token);
public record KickedFromChannelEvent(IrcUser kicker, string channel, string kickedUser, string reason);
public record ChannelMessageReceivedEvent(IrcUser Sender, string Target, string Content,  bool IsAction);
public record PrivateMessageReceivedEvent(IrcUser Sender, ChatMessage Message, bool IsAction);