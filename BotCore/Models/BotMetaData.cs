using BotCore.Interfaces;
using System.Reflection;

namespace LoboForge.TNOIRC.BotEngine
{
    public class BotMetadata : IBotMetadata
    {
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public Assembly SourceAssembly { get; set; } = null!;
        public IBot? Instance { get; set; }
        public string SourceType { get; set; } = "COD"; // Default to COD, set to "DLL" for dll bots

        // Optional: Add helper constructors for convenience
        public BotMetadata() { }
        public BotMetadata(IBot bot, Assembly asm, string sourceType)
        {
            Name = bot.Name;
            Instance = bot;
            SourceAssembly = asm;
            SourceType = sourceType;
            Enabled = true;
        }
    }

}
