using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using LoboForge.TNOIRC.Models;
using LoboForge.TNOIRC.Services;

namespace LoboForge.TNOIRC.Commands
{
    public class PrivMsgHandler : IIrcCommandHandler
    {
        public bool CanHandle(string command) => command == "PRIVMSG";

        public void Handle(IrcMessage message)
        {
            if (message.Prefix == null || message.Parameters.Count < 1 || message.Trailing == null)
                return;

            var sender = IrcUser.FromPrefix(message.Prefix);
            var target = message.Parameters[0];
            var rawContent = message.Trailing;

            // Check for CTCP ACTION format: \u0001ACTION ... \u0001
            bool isAction = rawContent.StartsWith("\u0001ACTION ") && rawContent.EndsWith("\u0001");

            var content = isAction
                ? rawContent[8..^1].Trim() // Remove \u0001ACTION and trailing \u0001
                : rawContent;

            var chatMessage = new ChatMessage(
                timestamp: DateTime.UtcNow,
                sender: sender,
                target: target,
                content: content,
                isAction: isAction
            );

            if (target.StartsWith('#') || target.StartsWith('&'))
            {
                // Channel message
                var channel = Common.ircClient.JoinedChannels
                    .FirstOrDefault(c => string.Equals(c.Name, target, StringComparison.OrdinalIgnoreCase));

                if (channel != null)
                {
                    channel.Messages.Add(chatMessage);
                }

                EventBus.Publish(new ChannelMessageReceivedEvent(sender, target, content, isAction));
            }
            else
            {
                // Private message
                var existing = Common.ircClient.DirectMessages
                    .FirstOrDefault(u => string.Equals(u.Name, sender.Nick, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                    Common.ircClient.DirectMessages.Add(new IrcChannel { Name = sender.Nick });

                EventBus.Publish(new PrivateMessageReceivedEvent(sender, chatMessage, isAction));
            }
        }
    }

    public class JoinHandler : IIrcCommandHandler
    {
        public bool CanHandle(string command) => command == "JOIN";

        public void Handle(IrcMessage message)
        {
            var channel = message.Trailing;

            if (message.Prefix == null && string.IsNullOrWhiteSpace(channel))
                return;

            // Fallback: JOIN #channel without trailing colon
            if (string.IsNullOrWhiteSpace(channel) && message.Parameters.Count > 0)
                channel = message.Parameters[0];

            if (string.IsNullOrWhiteSpace(channel))
                return;

            var user = IrcUser.FromPrefix(message.Prefix);

            if (string.Equals(user.Nick, Common.ircClient.Nick, StringComparison.OrdinalIgnoreCase))
            {
                var existing = Common.ircClient.JoinedChannels
                    .FirstOrDefault(c => string.Equals(c.Name, channel, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    existing = new IrcChannel
                    {
                        Name = channel,
                        IsJoined = true
                    };
                    Common.ircClient.JoinedChannels.Add(existing);
                }
                else
                {
                    existing.IsJoined = true;
                }

                EventBus.Publish(new SelfJoinedChannelEvent(user, channel));
            }
            else
            {
                EventBus.Publish(new UserJoinedEvent(user, channel));
            }
        }

    }


    public class PartHandler : IIrcCommandHandler
    {
        public bool CanHandle(string command) => command == "PART";

        public void Handle(IrcMessage message)
        {
            if (message.Prefix == null || message.Parameters.Count < 1)
                return;

            var user = IrcUser.FromPrefix(message.Prefix);
            var channel = message.Parameters[0];
            var reason = message.Trailing ?? "";

            EventBus.Publish(new UserPartedEvent(user, channel, reason));
        }
    }

    public class NameReplyHandler : IIrcCommandHandler
    {
        public bool CanHandle(string command) => command == "353"; // RPL_NAMREPLY

        public void Handle(IrcMessage message)
        {
            //if (message.Parameters.Count < 4 || message.Trailing == null)
            //    return;

            var channelName = message.Parameters[2];
            var nickList = message.Trailing.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var channel = Common.ircClient.JoinedChannels
                .FirstOrDefault(c => string.Equals(c.Name, channelName, StringComparison.OrdinalIgnoreCase));

            if (channel == null)
                return;

            foreach (var rawNick in nickList)
            {
                var cleanNick = rawNick.TrimStart('@', '+', '~', '&', '%');
                if (!channel.Users.Any(u => u.Nick.Equals(cleanNick, StringComparison.OrdinalIgnoreCase)))
                {
                    channel.Users.Add(new IrcUser(cleanNick));
                }
            }

            // Optionally update UserCount to match actual users received
            channel.UserCount = channel.Users.Count;

            EventBus.Publish(new NameReplyEvent(channelName, nickList));
        }
    }


    public class TopicReplyHandler : IIrcCommandHandler
    {
        public bool CanHandle(string command) => command == "332"; // RPL_TOPIC

        public void Handle(IrcMessage message)
        {
            if (message.Parameters.Count < 2 || message.Trailing == null)
                return;

            var channel = message.Parameters[1];
            var topic = message.Trailing;

            EventBus.Publish(new TopicReplyEvent(channel, topic));
        }
    }

    public class WelcomeHandler : IIrcCommandHandler
    {
        public bool CanHandle(string command) => command == "001"; // RPL_WELCOME

        public void Handle(IrcMessage message)
        {
            var welcome = message.Trailing ?? "";
            EventBus.Publish(new ConnectionCompletedEvent(welcome));
        }
    }
    public class ChannelListHandler : IIrcCommandHandler
    {
        private bool _isCollecting = false;
        private readonly List<IrcChannel> _buffer = new();

        public bool CanHandle(string command) => command == "322" || command == "323";

        public void Handle(IrcMessage message)
        {
            if (message.Command == "322" && message.Parameters.Count >= 3)
            {
                if (!_isCollecting)
                {
                    _buffer.Clear();
                    _isCollecting = true;
                }

                var channel = message.Parameters[1];
                var userCount = int.TryParse(message.Parameters[2], out var count) ? count : 0;
                var topic = message.Trailing ?? "";

                _buffer.Add(new IrcChannel
                {
                    Name = channel,
                    UserCount = userCount,
                    Topic = topic
                });
            }
            else if (message.Command == "323") // RPL_LISTEND
            {
                Common.ircClient.AvailableChannels.Clear();
                Common.ircClient.AvailableChannels.AddRange(_buffer);

                _isCollecting = false;
                EventBus.Publish(new ChannelListReceivedEvent(_buffer.ToList()));
                _buffer.Clear(); // Optional: clear buffer after publish
            }
        }
    }


    public class TopicHandler : IIrcCommandHandler
    {
        public bool CanHandle(string command) => command == "332";

        public void Handle(IrcMessage message)
        {
            if (message.Parameters.Count < 2)
                return;

            var channel = message.Parameters[1];
            var topic = message.Trailing ?? "";

            EventBus.Publish(new ChannelTopicEvent(channel, topic));
        }
    }
    public class NoTopicHandler : IIrcCommandHandler
    {
        public bool CanHandle(string command) => command == "331";

        public void Handle(IrcMessage message)
        {
            if (message.Parameters.Count < 2)
                return;

            var channel = message.Parameters[1];
            EventBus.Publish(new ChannelTopicEvent(channel, Topic: null));
        }
    }
    public class QuitHandler : IIrcCommandHandler
    {
        public bool CanHandle(string command) => command == "QUIT";

        public void Handle(IrcMessage message)
        {
            if (message.Prefix == null || message.Trailing == null)
                return;

            var user = IrcUser.FromPrefix(message.Prefix);
            var reason = message.Trailing;

            EventBus.Publish(new UserQuitEvent(user, reason));
        }
    }
    public class NickHandler : IIrcCommandHandler
    {
        public bool CanHandle(string command) => command == "NICK";

        public void Handle(IrcMessage message)
        {
            if (message.Prefix == null || message.Trailing == null)
                return;

            var oldUser = IrcUser.FromPrefix(message.Prefix);
            var newNick = message.Trailing;

            EventBus.Publish(new UserNickChangedEvent(oldUser, newNick));
        }
    }
    public class NoticeHandler : IIrcCommandHandler
    {
        public bool CanHandle(string command) => command == "NOTICE";

        public void Handle(IrcMessage message)
        {
            if (message.Prefix == null || message.Parameters.Count < 1 || message.Trailing == null)
                return;

            var sender = IrcUser.FromPrefix(message.Prefix);
            var target = message.Parameters[0];
            var content = message.Trailing;

            EventBus.Publish(new NoticeReceivedEvent(sender, target, content));
        }
    }
    public class ModeHandler : IIrcCommandHandler
    {
        public bool CanHandle(string command) => command == "MODE";

        public void Handle(IrcMessage message)
        {
            if (message.Parameters.Count < 2)
                return;

            var target = message.Parameters[0];
            var mode = message.Parameters[1];
            var args = message.Parameters.Skip(2).ToArray();

            EventBus.Publish(new ModeChangedEvent(target, mode, args));
        }
    }
    public class ErrorHandler : IIrcCommandHandler
    {
        public bool CanHandle(string command) => command == "ERROR";

        public void Handle(IrcMessage message)
        {
            var reason = message.Trailing ?? "Unknown error";
            EventBus.Publish(new ErrorEvent(reason));
        }
    }

    public class NameHandler : IIrcCommandHandler
    {
        public bool CanHandle(string command) => command == "366"; // RPL_ENDOFNAMES

        public void Handle(IrcMessage message)
        {
            if (message.Parameters.Count < 2)
                return;

            var channel = message.Parameters[1];
            EventBus.Publish(new EndOfNamesEvent(channel));
        }
    }
    public class EndOfListHandler : IIrcCommandHandler
    {
        public bool CanHandle(string command) => command == "323"; // RPL_LISTEND

        public void Handle(IrcMessage message)
        {
            EventBus.Publish(new EndOfListEvent());
        }
    }
    public class PingHandler : IIrcCommandHandler
    {
        public bool CanHandle(string command) => command == "PING";

        public void Handle(IrcMessage message)
        {
            var token = message.Trailing ?? "";
            EventBus.Publish(new PingReceivedEvent(token));
        }
    }
    public class KickHandler : IIrcCommandHandler
    {
        public bool CanHandle(string command) => command == "KICK";

        public void Handle(IrcMessage message)
        {
            if (message.Prefix == null || message.Parameters.Count < 2)
                return;

            var kicker = IrcUser.FromPrefix(message.Prefix);
            var channel = message.Parameters[0];
            var kickedUser = message.Parameters[1];
            var reason = message.Trailing ?? "";

            EventBus.Publish(new KickedFromChannelEvent(kicker, channel, kickedUser, reason));
        }
    }


}