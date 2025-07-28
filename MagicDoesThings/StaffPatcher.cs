using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Aspects;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Strings;
using Mutagen.Bethesda.Synthesis;
using Synthesis.Bethesda;

namespace MagicDoesThingsPatcher;
internal class StaffPatcher
{
    private readonly Dictionary<IFormLinkNullableGetter<IMagicEffectGetter>, IFormList> _MgefToFormList;
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly Lazy<Settings.Settings> _settings;
    private readonly MagicEffect.TranslationMask _mgefTranslationMask;

    public StaffPatcher(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, Lazy<Settings.Settings> settings)
    {
        _MgefToFormList = new();
        _state = state;
        _settings = settings;

        _mgefTranslationMask = new MagicEffect.TranslationMask(false)
        {
            CastingLight = true,
            CastingArt = true,
            Sounds = true,
        };
    }

    public void PatchStaves()
    {
        //todo: better spell archetype logic
        HashSet<IWeaponGetter> staves = FilterStaves();
        foreach (IWeaponGetter staff in staves)
        {
            bool success = ProcessStaff(staff);
            Console.WriteLine($"INFO: Patching staff: {staff} - success: {success}");
        }
        FillFormLists();
    }


    private HashSet<IWeaponGetter> FilterStaves()
    {
        return _state.LoadOrder.ListedOrder
            .Weapon().WinningOverrides()
            .Where(x => x.Keywords?.Contains(Skyrim.Keyword.WeapTypeStaff) ?? false)
            .Where(x =>
            {
                var origin = _state.LinkCache.Resolve<IWeaponGetter>(x.FormKey, Mutagen.Bethesda.Plugins.Cache.ResolveTarget.Origin);
                return _settings.Value.ModKeys.Contains(origin.FormKey.ModKey);
            })
            .ToHashSet();
    }


    private bool ProcessStaff(IWeaponGetter staffGetter)
    {
        if (!staffGetter.ObjectEffect.TryResolve(_state.LinkCache, out var objectEffectGetter))
        {
            Console.WriteLine($"{staffGetter} - failed to resolve Object Effect");
            return false;
        }

        ObjectEffect objectEffect = (ObjectEffect)objectEffectGetter.DeepCopy();
        Effect effect = objectEffect.Effects[0];

        if (!effect.BaseEffect.TryResolve(_state.LinkCache, out var originalEffect))
        {
            Console.WriteLine($"ERROR: {staffGetter} - {effect} - failed to resolve base effect");
            return false;
        }
        string? originalEffectName = originalEffect.Name?.String;
        string? originalEffectNameTrim = originalEffect.Name?.String?.Replace(" ", "");
        IFormLink<IMagicEffectGetter>? templateLink = MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchDestructionFrostEffect;
        TemplateRecord? templateRecord = TemplateRecord.GetTemplateFromMagicEffect(originalEffect);

        if (templateLink is null || !templateLink.TryResolve(_state.LinkCache, out var template))
        {
            Console.WriteLine($"ERROR: {staffGetter} - {templateLink} is null or failed to resolve");
            return false;
        }

        if (templateRecord is null)
        {
            Console.WriteLine($"ERROR: Failed to determine template for {originalEffect}");
            return false;
        }

        var newEffect = _state.PatchMod.MagicEffects.DuplicateInAsNewRecord(template);
        ChangeObjectEffectStats(objectEffect, newEffect);

        ChangeTemplateName(newEffect, originalEffectName);
        newEffect.EditorID = $"_MDTS_{originalEffectNameTrim}Effect";
        newEffect.Description ??= new TranslatedString(Language.English);
        newEffect.Description.String = newEffect.Description.String?.Replace("<Spell>", originalEffectName);

        newEffect.DeepCopyIn(originalEffect, _mgefTranslationMask);

        if (!newEffect.EquipAbility.TryResolve(_state.LinkCache, out var hookSpellTemplate))
        {
            Console.WriteLine($"ERROR: {staffGetter} - {newEffect} - failed to resolve EquipAbility");
            return false;
        }

        var hookSpell = _state.PatchMod.Spells.DuplicateInAsNewRecord(hookSpellTemplate);
        newEffect.EquipAbility = hookSpell.ToLink();

        ChangeTemplateName(hookSpell, originalEffectName);
        hookSpell.EditorID = $"_MDTS_Hook{originalEffectNameTrim}Spell";

        if (!hookSpell.Effects[0].BaseEffect.TryResolve(_state.LinkCache, out var hookEffectTemplate))
        {
            Console.WriteLine($"ERROR: {staffGetter} - {hookSpell} - failed to resolve Base Effect");
            return false;
        }

        var hookEffect = _state.PatchMod.MagicEffects.DuplicateInAsNewRecord(hookEffectTemplate);
        hookSpell.Effects[0].BaseEffect = hookEffect.ToNullableLink();

        ChangeTemplateName(hookEffect, originalEffectName);
        hookSpell.EditorID = $"_MDTS_Hook{originalEffectNameTrim}Effect";
        hookEffect.Keywords ??= new();
        hookEffect.Keywords.Add(templateRecord.SpellBladeKeyword);

        if (hookEffect.VirtualMachineAdapter is null || hookEffect.VirtualMachineAdapter.Scripts[0].Properties is null)
        {
            Console.WriteLine($"ERROR: {staffGetter} - {hookEffect} - VMAD or Properties are null");
            return false;
        }

        var properties = hookEffect.VirtualMachineAdapter.Scripts[0].Properties;

        if (properties.Find(x => x.Name == "StaffEnch") is not ScriptObjectProperty staffEnchProperty || properties.Find(x => x.Name == "ChannelSpell") is not ScriptObjectProperty channelSpellProperty)
        {
            Console.WriteLine($"ERROR: {staffGetter} - {hookEffect} - Properties not found");
            return false; //TODO: LOG / Error handling
        }

        staffEnchProperty.Object = objectEffect.ToLink();

        if (!channelSpellProperty.Object.TryResolve(_state.LinkCache, out var lensSpellTemplate) || lensSpellTemplate is not ISpellGetter)
        {
            Console.WriteLine($"ERROR: {staffGetter} - {channelSpellProperty} - failed to resolve");
            return false; //TODO: LOG / Error handling
        }

        var lensSpell = _state.PatchMod.Spells.DuplicateInAsNewRecord(lensSpellTemplate);
        lensSpell.Name = newEffect.Name;
        lensSpell.EditorID = $"_MDTS_Lens{originalEffectNameTrim}Spell";

        channelSpellProperty.Object = lensSpell.ToLink();

        if (!lensSpell.Effects[0].BaseEffect.TryResolve(_state.LinkCache, out var lensEffectTemplate))
        {
            Console.WriteLine($"ERROR: {staffGetter} - {lensSpell} - failed to resolve base effect");
            return false;
        }

        var lensEffect = _state.PatchMod.MagicEffects.DuplicateInAsNewRecord(lensEffectTemplate);
        lensSpell.Effects[0].BaseEffect = lensEffect.ToNullableLink();

        ChangeTemplateName(lensEffect, originalEffectName);
        lensEffect.EditorID = $"_MDTS_Lens{originalEffectNameTrim}Effect";
        lensEffect.HitShader = templateRecord.HitShader;
        lensEffect.Archetype.ActorValue = templateRecord.MagicSkill;
        lensEffect.MenuDisplayObject = templateRecord.DisplayObject.AsNullable();

        if (!templateRecord.Perk.TryResolve(_state.LinkCache, out var perkTemplate))
        {
            Console.WriteLine($"ERROR: {staffGetter} - {lensEffect} - failed to resolve perk");
            return false;
        }

        var perk = _state.PatchMod.Perks.DuplicateInAsNewRecord(perkTemplate);
        perk.EditorID = $"_MDTS_{originalEffectNameTrim}Perk";
        lensEffect.PerkToApply = perk.ToLink();

        IFormList formList;

        if (_MgefToFormList.ContainsKey(originalEffect.ToNullableLink()))
        {
            formList = _MgefToFormList[originalEffect.ToNullableLink()];
        }
        else
        {
            formList = _state.PatchMod.FormLists.AddNew($"_MDT_{originalEffectNameTrim}_FormList");
            _MgefToFormList.Add(originalEffect.ToNullableLink(), formList);
        }

        foreach (var perkEffect in perk.Effects)
        {
            if (perkEffect.Conditions[0].Conditions[0].Data is not HasMagicEffectConditionData firstConditionData ||
                perkEffect.Conditions[1].Conditions[0].Data is not IIsInListConditionData secondConditionData)
            {
                Console.WriteLine($"ERROR: failed to get condition data from {perk}");
                return false;
            }
            firstConditionData.MagicEffect = lensEffect.ToLink();
            secondConditionData.FormList = formList.ToLink();
        }

        _state.PatchMod.ObjectEffects.Add(objectEffect);
        return true;
    }

    private static void ChangeTemplateName(ITranslatedNamed translatedNamed, string? originalEffectName)
    {
        translatedNamed.Name ??= new TranslatedString(Language.English);
        translatedNamed.Name.String = translatedNamed.Name.String?.Replace("X", originalEffectName);
    }

    private static void ChangeObjectEffectStats(ObjectEffect objectEffect, MagicEffect newEffect)
    {
        objectEffect.EditorID = "_MDTS_" + objectEffect.EditorID;
        objectEffect.Flags |= ObjectEffect.Flag.NoAutoCalc;
        objectEffect.CastType = CastType.Concentration;
        objectEffect.TargetType = TargetType.Aimed;
        objectEffect.ChargeTime = 0;
        objectEffect.EnchantmentAmount = 9;
        objectEffect.EnchantmentCost = 9;

        objectEffect.Effects.Clear();
        objectEffect.Effects.Add(new Effect
        {
            BaseEffect = newEffect.ToNullableLink(),
            Data = new EffectData
            {
                Area = 0,
                Magnitude = 20,
                Duration = 0
            }
        });
    }

    private void FillFormLists()
    {
        foreach (var spellGetter in _state.LoadOrder.PriorityOrder.Spell().WinningOverrides())
        {
            foreach (var effect in spellGetter.Effects)
            {
                IFormLinkNullableGetter<IMagicEffectGetter> baseEffect = effect.BaseEffect;
                if (_MgefToFormList.ContainsKey(baseEffect))
                {
                    _MgefToFormList[baseEffect].Items.Add(spellGetter);
                    break;
                }
            }
        }
    }

}
