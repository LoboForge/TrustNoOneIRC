using Microsoft.AspNetCore.Components;

namespace TNO.mIRC.Models
{
    public class Window
    {
        public string ID = Guid.NewGuid().ToString();
        public int index { get; set; } = 1;
        public string Title { get; set; }
        public WindowType Type { get; set; } = WindowType.Channel;
        public RenderFragment ChildContent { get; set; } = default!;

    }
    public enum WindowType
    {
        Channel,
        Join,
        Log,
        PM,
        Settings,
        Commands
    }
}
