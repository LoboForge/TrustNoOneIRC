using TNO.mIRC.Models;

namespace TNO.mIRC.Services
{
    public interface IIrcCommandHandler
    {
        bool CanHandle(string command); // e.g. "PRIVMSG", "001", etc.
        void Handle(IrcMessage message);
    }
    public class IrcCommandDispatcher
    {
        private readonly List<IIrcCommandHandler> _handlers;

        public IrcCommandDispatcher(IEnumerable<IIrcCommandHandler> handlers)
        {
            _handlers = handlers.ToList();
        }

        public void Dispatch(IrcMessage message)
        {
            var handler = _handlers.FirstOrDefault(h => h.CanHandle(message.Command));
            if (handler != null)
            {
                handler.Handle(message);
            }
            else
            {
                // Optional logging:
                Console.WriteLine($"[Unhandled] {message.Raw}");
            }
        }
    }

}
