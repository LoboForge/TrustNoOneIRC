using BotCore.Interfaces;
using System.Reflection;

namespace LoboForge.TNOIRC.BotEngine
{
    public class BotMetadata : IBotMetadata
    {
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public Assembly SourceAssembly { get; set; } = null!;
        public IBot? Instance { get; set; } // 👈 Store the instance
    }

}
