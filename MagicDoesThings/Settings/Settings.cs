using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Synthesis.Settings;

namespace MagicDoesThingsPatcher.Settings;
internal class Settings
{
    [SynthesisSettingName("Mods to patch")]
    public List<ModKey> ModKeys { get; set; } = new();
}
