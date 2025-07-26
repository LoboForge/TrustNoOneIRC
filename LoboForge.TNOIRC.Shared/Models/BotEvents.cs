using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoboForge.TNOIRC.Shared.Models
{
    public class BotSendChannelMessageEvent
    {
        public string Channel { get; set; }
        public string Message { get; set; }
    }

    public class BotPrivateMessageEvent
    {
        public string Nick { get; set; }
        public string Message { get; set; }
    }
}
