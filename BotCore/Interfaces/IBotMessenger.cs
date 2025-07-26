using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotCore.Interfaces
{
    public interface IBotMessenger
    {
        void SendChannelMessage(string channel, string message);
        void SendPrivateMessage(string nick, string message);
    }
}
