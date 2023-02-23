using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Aspects;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Strings;
using Mutagen.Bethesda.Synthesis;

namespace MagicDoesThingsPatcher;
internal class StaffPatcher
{
    private readonly Dictionary<IFormLinkNullableGetter<IMagicEffectGetter>, IFormList> _MgefToFormList;
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly Lazy<Settings.Settings> _settings;

    public StaffPatcher(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, Lazy<Settings.Settings> settings)
    {
        _MgefToFormList = new();
        _state = state;
        _settings = settings;
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

    private static IFormLink<IMagicEffectGetter>? GetStaffArchetype(IMagicEffectGetter magicEffect)
    {
        //log when fail to find archetype
        switch (magicEffect.MagicSkill)
        {
            case ActorValue.Alteration:
                {
                    if (magicEffect.Archetype.Type == MagicEffectArchetype.TypeEnum.Paralysis) return MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchAlterationParalysisEffect;
                    if (magicEffect.Archetype.Type == MagicEffectArchetype.TypeEnum.Light) return MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchAlterationLightEffect;
                    //TODO: determine ash spells if () return MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchAlterationAshEffect;
                    return MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchAlterationLightEffect;
                }

            case ActorValue.Conjuration:
                {
                    return magicEffect.Archetype.Type switch
                    {
                        MagicEffectArchetype.TypeEnum.CommandSummoned => MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchConjurationCommandEffect,
                        MagicEffectArchetype.TypeEnum.SummonCreature => MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchConjurationSummonEnchEffect,
                        MagicEffectArchetype.TypeEnum.Banish => MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchConjurationSummonEnchEffect,
                        _ => MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchConjurationNonSummonOrCommandEffect
                    };
                }

            case ActorValue.Destruction:
                {
                    if (magicEffect.HasKeyword(Skyrim.Keyword.MagicDamageFire)) return MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchDestructionFireEffect;
                    if (magicEffect.HasKeyword(Skyrim.Keyword.MagicDamageFrost)) return MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchDestructionFrostEffect;
                    if (magicEffect.HasKeyword(Skyrim.Keyword.MagicDamageShock)) return MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchDestructionShockEffect;
                    return MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchDestructionFireEffect; //TODO: template for destruction none
                }
            case ActorValue.Illusion:
                {
                    return magicEffect.Archetype.Type switch
                    {
                        MagicEffectArchetype.TypeEnum.Rally => MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchIllusionConfidenceUpEffect,
                        MagicEffectArchetype.TypeEnum.Demoralize => MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchIllusionConfidenceDownEffect,
                        MagicEffectArchetype.TypeEnum.Frenzy => MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchIllusionAggressionUpEffect,
                        MagicEffectArchetype.TypeEnum.Calm => MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchIllusionAggressionDownEffect,
                        _ => MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchIllusionConfidenceUpEffect //TODO: template
                    };
                }
            case ActorValue.Restoration:
                {
                    if (magicEffect.Archetype.Type == MagicEffectArchetype.TypeEnum.ValueModifier
                        && magicEffect.Archetype.ActorValue == ActorValue.Health)
                    {
                        if (magicEffect.ResistValue == ActorValue.PoisonResist || magicEffect.ResistValue == ActorValue.ResistDisease) return MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchRestorationPoisonEffect;
                        if (magicEffect.ResistValue == ActorValue.None) return MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchRestorationSunEffect;
                    }
                    if (magicEffect.Archetype.Type == MagicEffectArchetype.TypeEnum.TurnUndead) return MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchRestorationTurnEffect;
                    return MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchRestorationTurnEffect;
                }
            default:
                Console.WriteLine($"Failed to determine template for {magicEffect}");
                return null;
        }
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
        IFormLink<IMagicEffectGetter>? templateLink = GetStaffArchetype(originalEffect);

        if (templateLink is null || !templateLink.TryResolve(_state.LinkCache, out var template))
        {
            Console.WriteLine($"ERROR: {staffGetter} - {templateLink} is null or failed to resolve");
            return false;
        }

        Console.WriteLine($"INFO: Assigned {template} to {staffGetter}");
        var newEffect = _state.PatchMod.MagicEffects.DuplicateInAsNewRecord(template);

        ChangeObjectEffectStats(objectEffect, newEffect);

        ChangeTemplateName(newEffect, originalEffectName);
        newEffect.Description ??= new TranslatedString(Language.English);
        newEffect.Description.String = newEffect.Description.String?.Replace("<Spell>", originalEffectName);

        if (!newEffect.EquipAbility.TryResolve(_state.LinkCache, out var hookSpellTemplate))
        {
            Console.WriteLine($"ERROR: {staffGetter} - {newEffect} - failed to resolve EquipAbility");
            return false;
        }

        var hookSpell = _state.PatchMod.Spells.DuplicateInAsNewRecord(hookSpellTemplate);
        newEffect.EquipAbility = hookSpell.ToLink();

        ChangeTemplateName(hookSpell, originalEffectName);


        if (!hookSpell.Effects[0].BaseEffect.TryResolve(_state.LinkCache, out var hookEffectTemplate))
        {
            Console.WriteLine($"ERROR: {staffGetter} - {hookSpell} - failed to resolve Base Effect");
            return false;
        }

        var hookEffect = _state.PatchMod.MagicEffects.DuplicateInAsNewRecord(hookEffectTemplate);
        hookSpell.Effects[0].BaseEffect = hookEffect.ToNullableLink();

        ChangeTemplateName(hookEffect, originalEffectName);

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

        channelSpellProperty.Object = lensSpell.ToLink();

        if (!lensSpell.Effects[0].BaseEffect.TryResolve(_state.LinkCache, out var lensEffectTemplate))
        {
            Console.WriteLine($"ERROR: {staffGetter} - {lensSpell} - failed to resolve base effect");
            return false;
        }

        var lensEffect = _state.PatchMod.MagicEffects.DuplicateInAsNewRecord(lensEffectTemplate);
        lensSpell.Effects[0].BaseEffect = lensEffect.ToNullableLink();

        ChangeTemplateName(lensEffect, originalEffectName);

        if (!lensEffect.PerkToApply.TryResolve(_state.LinkCache, out var perkTemplate))
        {
            Console.WriteLine($"ERROR: {staffGetter} - {lensEffect} - failed to resolve perk");
            return false;
        }

        var perk = _state.PatchMod.Perks.DuplicateInAsNewRecord(perkTemplate);

        lensEffect.PerkToApply = perk.ToLink();

        var formList = _state.PatchMod.FormLists.AddNew($"_MDT_{objectEffect.Name?.String?.Replace(" ", "")}_FormList");

        foreach (var perkEffect in perk.Effects)
        {
            if (perkEffect.Conditions[0].Conditions[0].Data is not FunctionConditionData firstConditionData || perkEffect.Conditions[1].Conditions[0].Data is not FunctionConditionData secondConditionData)
            {
                Console.WriteLine($"ERROR: failed to get condition data from {perk}");
                return false;
            }
            firstConditionData.ParameterOneRecord = lensEffect.ToLink();
            secondConditionData.ParameterOneRecord = formList.ToLink();
        }

        _MgefToFormList.Add(originalEffect.ToNullableLink(), formList);
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
