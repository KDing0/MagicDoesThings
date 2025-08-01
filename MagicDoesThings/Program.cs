using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Synthesis.Bethesda;
using Noggog;

namespace MagicDoesThingsPatcher;

public class Program
{
    /*        enum SpellArchetype
            {
                None,
                AlterationAsh,
                AlterationLight,
                AlterationParalysis,
                AlterationNone,
                ConjurationCommand,
                ConjurationNonSummon,
                ConjurationReanimate,
                ConjurationSummon,
                DestructionFire,
                DestructionFrost,
                DestructionShock,
                DestructionNone,
                IllusionCourage,
                IllusionFear,
                IllusionFury,
                IllusionCalm,
                IllusionNone,
                RestorationPoison,
                RestorationSun,
                RestorationTurn,
                RestorationNone
            }*/

    private static Lazy<Settings.Settings> _settings = null!;

    public static async Task<int> Main(string[] args)
    {
        return await SynthesisPipeline.Instance
            .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
            .SetTypicalOpen(GameRelease.SkyrimSE, "MagicDoesThingsPatcher.esp")
            .AddRunnabilityCheck(x => x.LoadOrder.ContainsKey(ModKey.FromNameAndExtension("MagicDoesThings.esp")))
            .SetAutogeneratedSettings(nickname: "Settings", path: "settings.json", out _settings)
            .AddRunnabilityCheck(x => _settings.Value.ModKeys.Any())
            .Run(args);
    }

    public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        var staffPatcher = new StaffPatcher(state, _settings);
        var scrollPatcher = new ScrollPatcher(state, _settings);

        staffPatcher.PatchStaves();
        scrollPatcher.PatchScrolls();
    }
}
