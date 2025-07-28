using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Strings;
using Mutagen.Bethesda.Synthesis;
using Synthesis.Bethesda;

namespace MagicDoesThingsPatcher;
internal class ScrollPatcher
{
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly Lazy<Settings.Settings> _settings;

    private readonly MagicEffect.TranslationMask _originalMgefTranslationMask;
    private readonly MagicEffect.TranslationMask _mgefTranslationMask;


    public ScrollPatcher(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, Lazy<Settings.Settings> settings)
    {
        _state = state;
        _settings = settings;
        _originalMgefTranslationMask = new(false)
        {
            CastingLight = true,
            CastingArt = true,
            Sounds = true,
        };
        _mgefTranslationMask = new(true)
        {
            VirtualMachineAdapter = false,
            Conditions = false,
            EditorID = false
        };
    }

    public void PatchScrolls()
    {
        HashSet<IScrollGetter> suitableScrolls = FilterScrolls();
        foreach (IScrollGetter scroll in suitableScrolls)
        {
            bool success = ProcessScroll(scroll);
            Console.WriteLine($"INFO: Patching scroll: {scroll} - success: {success}");
        }
    }

    private HashSet<IScrollGetter> FilterScrolls()
    {
        return _state.LoadOrder.ListedOrder
            .Scroll().WinningOverrides()
            .Where(x => x.CastType == CastType.FireAndForget)
            .Where(x =>
            {
                var origin = _state.LinkCache.Resolve<IScrollGetter>(x.FormKey, Mutagen.Bethesda.Plugins.Cache.ResolveTarget.Origin);
                return _settings.Value.ModKeys.Contains(origin.FormKey.ModKey);
            })
            .ToHashSet();
    }

    private IFormLink<ISpellGetter>? FindSpellFromMagicEffect(IFormLinkGetter<IMagicEffectGetter> formLink, string? scrollSpellName)
    {
        foreach (var spellGetter in _state.LoadOrder.ListedOrder.Spell().WinningOverrides())
        {
            foreach (var effect in spellGetter.Effects)
            {
                IFormLinkNullableGetter<IMagicEffectGetter> baseEffect = effect.BaseEffect;
                if (baseEffect.Equals(formLink)
                    && !spellGetter.HalfCostPerk.IsNull
                    && (spellGetter.EquipmentType.Equals(Skyrim.EquipType.EitherHand.AsNullable())
                        || spellGetter.EquipmentType.Equals(Skyrim.EquipType.BothHands.AsNullable())
                        || spellGetter.EquipmentType.Equals(Skyrim.EquipType.RightHand.AsNullable()))
                    && spellGetter.Type == SpellType.Spell)
                {
                    if (spellGetter.Name?.String != scrollSpellName)
                    {
                        Console.WriteLine($"  WARN: Spell name of {spellGetter} is different from scroll's {scrollSpellName}");
                    }
                    return spellGetter.ToLink();
                }
            }
        }
        return null;
    }

    private bool ProcessScroll(IScrollGetter scrollGetter)
    {
        var scroll = scrollGetter.DeepCopy();
        string scrollSpellName;

        if (scroll.Name is not null && scroll.Name.String is not null)
        {
            scrollSpellName = string.Join(" ", scroll.Name.String.Split(" ")[2..]);
        }
        else
        {
            scrollSpellName = "Some Generic Spell";
        }


        if (!scroll.Effects[0].BaseEffect.TryResolve(_state.LinkCache, out var baseEffect))
        {
            Console.WriteLine($"  ERROR: {scrollGetter} - failed to resolve base effect, skipping");
            return false;
        }

        var spell = FindSpellFromMagicEffect(scroll.Effects[0].BaseEffect, scrollSpellName);
        if (spell == null)
        {
            Console.WriteLine($"  WARN: {scrollGetter} - failed to find according spell, skipping");
            return false;
        }

        bool isEffectUsingMagnitude = baseEffect.Flags.HasFlag(MagicEffect.Flag.PowerAffectsMagnitude);
        int charges = baseEffect.MinimumSkillLevel switch
        {
            < 25 => 5,
            < 50 => 4,
            < 75 => 3,
            < 100 => 2,
            >= 100 => 1,
        };

        CreateSecondEffect(scrollSpellName, spell, charges, out MagicEffect secondEffect, out ScriptEntry secondEffectScriptEntry);

        MagicEffect firstEffect = CreateFirstEffect(scrollSpellName, baseEffect, secondEffect, scroll, secondEffectScriptEntry, charges);

        Perk applyPerk = CreatePerk(scrollSpellName, spell, isEffectUsingMagnitude, secondEffect, firstEffect);

        firstEffect.PerkToApply = applyPerk.ToLink();

        scroll.TargetType = TargetType.Self;
        scroll.BaseCost = 180;
        scroll.Keywords ??= new();
        scroll.Keywords.Add(MagicDoesThings.Keyword._MDT_ScrollKeyword);
        scroll.Effects.Clear();
        EffectData effectData = new()
        {
            Area = 0,
            Magnitude = 0,
            Duration = 600
        };
        scroll.Effects.Add(new Effect
        {
            Data = effectData,
            BaseEffect = firstEffect.ToNullableLink()
        });
        scroll.Effects.Add(new Effect
        {
            Data = effectData,
            BaseEffect = secondEffect.ToNullableLink()
        });
        _state.PatchMod.Scrolls.Add(scroll);
        return true;
    }

    private MagicEffect CreateFirstEffect(string scrollSpellName, IMagicEffectGetter baseEffect, IMagicEffect secondEffect, IScroll scroll, IScriptEntry secondEffectScriptEntry, int charges)
    {
        ScriptEntry firstEffectScriptEntry = secondEffectScriptEntry.DeepCopy();
        firstEffectScriptEntry.Name = "_MDT_ScrollScript";
        firstEffectScriptEntry.Properties.Add(new ScriptObjectProperty
        {
            Name = "TheScroll",
            Flags = ScriptProperty.Flag.Edited,
            Object = scroll.ToLink()
        });

        MagicEffect firstEffect = _state.PatchMod.MagicEffects.AddNew($"_MDTS_Scroll{scrollSpellName.Replace(" ", "")}Effect");
        firstEffect.DeepCopyIn(secondEffect, _mgefTranslationMask);
        firstEffect.DeepCopyIn(baseEffect, _originalMgefTranslationMask);
        firstEffect.HitEffectArt = Skyrim.ArtObject.AbsorbSpellEffect;

        firstEffect.Name!.String = "Scroll Amplification";
        firstEffect.Description = new TranslatedString(Language.English, $"You may cast <{scrollSpellName}> a total of <{charges}> times for no cost. If you already know <{scrollSpellName}>, it is <20>% more powerful.");
        firstEffect.VirtualMachineAdapter = new()
        {
            Version = 5,
            ObjectFormat = 2,
            Scripts = new() { firstEffectScriptEntry }
        };

        firstEffect.Flags ^= MagicEffect.Flag.HideInUI;
        firstEffect.Keywords = new()
        {
            Skyrim.Keyword.WISpellColorful
        };
        return firstEffect;
    }

    private Perk CreatePerk(string scrollSpellName, IFormLink<ISpellGetter> spell, bool isEffectUsingMagnitude, MagicEffect secondEffect, MagicEffect firstEffect)
    {
        static Noggog.ExtendedList<PerkCondition> CreatePerkConditions(IConditionDataGetter effectConditionData, IFormLink<ISpellGetter> spell) => new()
            {
                new PerkCondition
                {
                    RunOnTabIndex = 0,
                    Conditions = new()
                    {
                        new ConditionFloat
                        {
                            CompareOperator = CompareOperator.EqualTo,
                            ComparisonValue = 1,
                            Data = (ConditionData)effectConditionData
                        }
                    }
                },
                new PerkCondition
                {
                    RunOnTabIndex = 1,
                    Conditions = new()
                    {
                        new ConditionFloat
                        {
                            CompareOperator = CompareOperator.EqualTo,
                            ComparisonValue = 1,
                            Data = new GetIsIDConditionData
                            {
                                Object = (IFormLinkOrIndex<IReferenceableObjectGetter>)spell,
                                RunOnType = Condition.RunOnType.Subject
                            }
                        }
                    }
                }
            };

        Perk applyPerk = _state.PatchMod.Perks.AddNew($"_MDTS_{scrollSpellName.Replace(" ", "")}ScrollPerk");
        applyPerk.Playable = true;
        applyPerk.NumRanks = 1;

        HasMagicEffectConditionData firstFunctionConditionData = new()
        {
            MagicEffect = (IFormLinkOrIndex<IMagicEffectGetter>)firstEffect.ToLink(),
            RunOnType = Condition.RunOnType.Subject
        };
        HasMagicEffectConditionData secondFunctionConditionData = new()
        {
            MagicEffect = (IFormLinkOrIndex<IMagicEffectGetter>)secondEffect.ToLink(),
            RunOnType = Condition.RunOnType.Subject
        };


        applyPerk.Effects.Add(new PerkEntryPointModifyValue
        {
            Rank = 0,
            Priority = 98,
            EntryPoint = APerkEntryPointEffect.EntryType.ModSpellCost,
            Modification = PerkEntryPointModifyValue.ModificationType.Set,
            PerkConditionTabCount = 2,
            Conditions = CreatePerkConditions(firstFunctionConditionData, spell),
            Value = 0.0f
        });

        applyPerk.Effects.Add(new PerkEntryPointModifyValue
        {
            Rank = 0,
            Priority = 99,
            EntryPoint = isEffectUsingMagnitude switch
            {
                true => APerkEntryPointEffect.EntryType.ModSpellMagnitude,
                false => APerkEntryPointEffect.EntryType.ModSpellDuration
            },
            Modification = PerkEntryPointModifyValue.ModificationType.Multiply,
            PerkConditionTabCount = 3,
            Conditions = CreatePerkConditions(secondFunctionConditionData, spell),
            Value = 1.2f
        });
        return applyPerk;
    }

    private void CreateSecondEffect(string scrollSpellName, IFormLink<ISpellGetter> spell, int charges, out MagicEffect secondEffect, out ScriptEntry secondEffectScriptEntry)
    {
        secondEffect = _state.PatchMod.MagicEffects.AddNew($"_MDTS_ScrollKnown{scrollSpellName.Replace(" ", "")}Effect");
        secondEffect.Name = new TranslatedString(Language.English, "Scroll Amplification 2");
        secondEffect.MenuDisplayObject = Skyrim.Static.MagicHatMarker.AsNullable();
        secondEffect.Flags |= MagicEffect.Flag.NoArea
                            | MagicEffect.Flag.HideInUI
                            | MagicEffect.Flag.NoRecast
                            | MagicEffect.Flag.PowerAffectsMagnitude;

        secondEffect.BaseCost = 1;
        secondEffect.SpellmakingCastingTime = 0.5f;
        secondEffect.Archetype = new MagicEffectArchetype()
        {
            Type = MagicEffectArchetype.TypeEnum.Script,
            ActorValue = ActorValue.None
        };
        secondEffect.Projectile = Skyrim.Projectile.HealFakeProjectile;
        secondEffect.CastType = CastType.FireAndForget;
        secondEffect.TargetType = TargetType.Self;
        secondEffect.SkillUsageMultiplier = 1;
        secondEffect.DualCastScale = 1;
        secondEffect.Conditions.Add(new ConditionFloat()
        {
            CompareOperator = CompareOperator.EqualTo,
            ComparisonValue = 1,
            Data = new HasSpellConditionData()
            {
                Spell = (IFormLinkOrIndex<ISpellGetter>)spell,
                RunOnType = Condition.RunOnType.Subject
            }
        });

        secondEffectScriptEntry = new()
        {
            Name = "_MDT_ScrollKnownScript",
            Flags = ScriptEntry.Flag.Local,
            Properties = new()
            {
                new ScriptIntProperty
                {
                    Name = "BaseCharges",
                    Flags = ScriptProperty.Flag.Edited,
                    Data = charges
                },
                new ScriptObjectProperty
                {
                    Name = "ScrollSpell",
                    Flags = ScriptProperty.Flag.Edited,
                    Object = spell
                }
            }
        };
        secondEffect.VirtualMachineAdapter = new()
        {
            Version = 5,
            ObjectFormat = 2,
            Scripts = new() { secondEffectScriptEntry }
        };
    }
}
