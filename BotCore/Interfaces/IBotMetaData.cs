using BotCore.Interfaces;
using System.Reflection;

namespace LoboForge.TNOIRC.BotEngine
{
    public interface IBotMetadata
    {
        string Name { get; }
        bool Enabled { get; set; }
        Assembly SourceAssembly { get; }
        IBot? Instance { get; set; }

        /// <summary>
        /// Indicates the source of the bot: "COD" (Roslyn/script), "DLL" (compiled class library), etc.
        /// </summary>
        string SourceType { get; }
    }


}
