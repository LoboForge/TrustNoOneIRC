using BotCore.Interfaces;
using System.Reflection;

namespace LoboForge.TNOIRC.BotEngine
{
    public interface IBotMetadata
    {
        string Name { get; }
        bool Enabled { get; set; }
        Assembly SourceAssembly { get; }
        IBot? Instance { get; set; } // 👈 Add this

    }
}
