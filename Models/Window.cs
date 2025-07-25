using Microsoft.AspNetCore.Components;

namespace LoboForge.TNOIRC.Models
{
    public class Window
    {
        public string ID = Guid.NewGuid().ToString();
        public int index { get; set; } = 1;
        public string Title { get; set; }
        public WindowType Type { get; set; } = WindowType.Channel;
        public RenderFragment ChildContent { get; set; } = default!;

        public int Top { get; set; }
        public int Left { get; set; }
        public int Width { get; set; } = 480;
        public int Height { get; set; } = 360;
        public bool IsMinimized { get; set; } = false;
    }
    public enum WindowType
    {
        Channel,
        Join,
        Log,
        PM,
        Settings,
        Commands,
        AutoReply,
        BotManager
    }
}
