using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace MagicDoesThingsPatcher;

internal record TemplateRecord
{

    public ActorValue MagicSkill { get; private set; }

    public IFormLink<IKeywordGetter> SpellBladeKeyword { get; private set; }

    public IFormLink<IStaticGetter> DisplayObject { get; private set; }

    public IFormLink<IPerkGetter> Perk { get; private set; }

    protected TemplateRecord(ActorValue magicSkill,
                             IFormLink<IKeywordGetter> spellBladeKeyword,
                             IFormLink<IStaticGetter> displayObject,
                             IFormLink<IPerkGetter> perk)
    {
        MagicSkill = magicSkill;
        SpellBladeKeyword = spellBladeKeyword;
        DisplayObject = displayObject;
        Perk = perk;
    }

    public static TemplateRecord? GetTemplateFromMagicEffect(IMagicEffectGetter magicEffect)
    {
        ActorValue magicSkill;
        IFormLink<IKeywordGetter> spellBladeKeyword;
        IFormLink<IStaticGetter> displayObject;
        IFormLink<IPerkGetter> perk;
        //log when fail to find archetype
        switch (magicEffect.MagicSkill)
        {
            case ActorValue.Alteration:
                {
                    magicSkill = ActorValue.AlterationPowerModifier;
                    displayObject = Skyrim.Static.MAGINVAlteration;
                    perk = MagicDoesThings.Perk._MDT_TemplateAlteConjuNonSummonOrCommandStaffPerk;

                    if (magicEffect.Archetype.Type == MagicEffectArchetype.TypeEnum.Paralysis)
                    {
                        spellBladeKeyword = MagicDoesThings.Keyword._MDT_LightStaffKeyword;
                        displayObject = Skyrim.Static.MAGInvParalyze;
                    }
                    else if (magicEffect.Archetype.Type == MagicEffectArchetype.TypeEnum.Light)
                    {
                        spellBladeKeyword = MagicDoesThings.Keyword._MDT_ParalysisStaffKeyword;
                    }
                    else
                    {
                        spellBladeKeyword = MagicDoesThings.Keyword._MDT_LightStaffKeyword; //TODO: change
                    }
                    //TODO: determine ash spells if () return MagicDoesThings.MagicEffect._MDT_TemplateStaffEnchAlterationAshEffect;
                    break;
                }
            case ActorValue.Conjuration:
                {
                    magicSkill = ActorValue.ConjurationPowerModifier;
                    switch (magicEffect.Archetype.Type)
                    {
                        case MagicEffectArchetype.TypeEnum.CommandSummoned:
                            {
                                perk = MagicDoesThings.Perk._MDT_TemplateConjuCommandStaffPerk;
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_CommandStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVSummon;
                                break;
                            }
                        case MagicEffectArchetype.TypeEnum.SummonCreature:
                            {
                                perk = MagicDoesThings.Perk._MDT_TemplateConjuSummonReanimateStaffPerk;
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_BanishStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVSummon;
                                break;
                            }
                        case MagicEffectArchetype.TypeEnum.Reanimate:
                            {
                                perk = MagicDoesThings.Perk._MDT_TemplateConjuSummonReanimateStaffPerk;
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_ReanimateStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVReanimate;
                                break;
                            }
                        case MagicEffectArchetype.TypeEnum.Banish:
                            {
                                perk = MagicDoesThings.Perk._MDT_TemplateAlteConjuNonSummonOrCommandStaffPerk;
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_BanishStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVBanish;
                                break;
                            }
                        default:
                            {
                                perk = MagicDoesThings.Perk._MDT_TemplateAlteConjuNonSummonOrCommandStaffPerk;
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_SoulTrapStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVReanimate;
                                break;
                            }
                    }
                    break;
                }
            case ActorValue.Destruction:
                {
                    magicSkill = ActorValue.DestructionPowerModifier;
                    perk = MagicDoesThings.Perk._MDT_TemplateDestIlluRestoStaffPerk;

                    if (magicEffect.HasKeyword(Skyrim.Keyword.MagicDamageFire))
                    {
                        spellBladeKeyword = MagicDoesThings.Keyword._MDT_FireStaffKeyword;
                        displayObject = Skyrim.Static.MAGINVFireballArt;
                    }
                    else if (magicEffect.HasKeyword(Skyrim.Keyword.MagicDamageFrost))
                    {
                        spellBladeKeyword = MagicDoesThings.Keyword._MDT_FrostStaffKeyword;
                        displayObject = Skyrim.Static.MAGINVIceSpellArt;
                    }
                    else if (magicEffect.HasKeyword(Skyrim.Keyword.MagicDamageShock))
                    {
                        spellBladeKeyword = MagicDoesThings.Keyword._MDT_ShockStaffKeyword;
                        displayObject = Skyrim.Static.MAGINVShockSpellArt;
                    }
                    else
                    {
                        spellBladeKeyword = MagicDoesThings.Keyword._MDT_FireStaffKeyword;
                        displayObject = Skyrim.Static.MAGINVFireballArt;
                    }
                    break;
                    //TODO: template for destruction none
                }
            case ActorValue.Illusion:
                {
                    magicSkill = ActorValue.IllusionPowerModifier;
                    perk = MagicDoesThings.Perk._MDT_TemplateDestIlluRestoStaffPerk;
                    switch (magicEffect.Archetype.Type)
                    {
                        case MagicEffectArchetype.TypeEnum.Rally:
                            {
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_ConfidenceStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVIllusionLight01;
                                break;
                            }
                        case MagicEffectArchetype.TypeEnum.Demoralize:
                            {
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_ConfidenceStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVIllusionDarkt01;
                                break;
                            }
                        case MagicEffectArchetype.TypeEnum.Calm:
                            {
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_AggressionStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVIllusionLight01;
                                break;
                            }
                        case MagicEffectArchetype.TypeEnum.Frenzy:
                            {
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_AggressionStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVIllusionDarkt01;
                                break;
                            }
                        default:
                            {
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_ConfidenceStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVIllusionLight01;
                                break;
                            }
                    }
                    break;
                }
            case ActorValue.Restoration:
                {
                    magicSkill = ActorValue.RestorationPowerModifier;
                    perk = MagicDoesThings.Perk._MDT_TemplateDestIlluRestoStaffPerk;
                    if (magicEffect.Archetype.Type == MagicEffectArchetype.TypeEnum.ValueModifier
                        && magicEffect.Archetype.ActorValue == ActorValue.Health)
                    {
                        if (magicEffect.ResistValue == ActorValue.PoisonResist || magicEffect.ResistValue == ActorValue.ResistDisease)
                        {
                            displayObject = Skyrim.Static.MAGINVAbsorb;
                            spellBladeKeyword = MagicDoesThings.Keyword._MDT_PoisonStaffKeyword;
                        }
                        else if (magicEffect.ResistValue == ActorValue.None)
                        {
                            displayObject = Skyrim.Static.MAGINVHealSpellArt;
                            spellBladeKeyword = MagicDoesThings.Keyword._MDT_SunStaffKeyword;
                        }
                        else
                        {
                            spellBladeKeyword = MagicDoesThings.Keyword._MDT_SunStaffKeyword;
                            displayObject = Skyrim.Static.MAGINVHealSpellArt;  //TODO: change
                        }
                    }
                    else if (magicEffect.Archetype.Type == MagicEffectArchetype.TypeEnum.TurnUndead)
                    {
                        displayObject = Skyrim.Static.MAGINVTurnUndead;
                        spellBladeKeyword = MagicDoesThings.Keyword._MDT_TurnStaffKeyword;
                    }
                    else
                    {
                        spellBladeKeyword = MagicDoesThings.Keyword._MDT_SunStaffKeyword;
                        displayObject = Skyrim.Static.MAGINVHealSpellArt;  //TODO: change
                    }
                    break;
                }
            default:
                return null;
        }
        return new TemplateRecord(magicSkill, spellBladeKeyword, displayObject, perk);
    }

}
