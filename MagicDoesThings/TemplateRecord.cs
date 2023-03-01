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
    public IFormLink<IEffectShaderGetter> HitShader { get; private set; }

    protected TemplateRecord(ActorValue magicSkill,
                             IFormLink<IKeywordGetter> spellBladeKeyword,
                             IFormLink<IStaticGetter> displayObject,
                             IFormLink<IPerkGetter> perk,
                             IFormLink<IEffectShaderGetter> hitShader)
    {
        MagicSkill = magicSkill;
        SpellBladeKeyword = spellBladeKeyword;
        DisplayObject = displayObject;
        Perk = perk;
        HitShader = hitShader;
    }

    public static TemplateRecord? GetTemplateFromMagicEffect(IMagicEffectGetter magicEffect)
    {
        ActorValue magicSkill;
        IFormLink<IKeywordGetter> spellBladeKeyword;
        IFormLink<IStaticGetter> displayObject;
        IFormLink<IPerkGetter> perk;
        IFormLink<IEffectShaderGetter> hitShader;
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
                        spellBladeKeyword = MagicDoesThings.Keyword._MDT_ParalysisStaffKeyword;
                        displayObject = Skyrim.Static.MAGInvParalyze;
                        hitShader = Skyrim.EffectShader.ParalyzeFxShader;
                    }
                    else if (magicEffect.Archetype.Type == MagicEffectArchetype.TypeEnum.Light)
                    {
                        spellBladeKeyword = MagicDoesThings.Keyword._MDT_LightStaffKeyword;
                        hitShader = Skyrim.EffectShader.HealFXS;
                    }
                    else
                    {
                        spellBladeKeyword = MagicDoesThings.Keyword._MDT_DefaultAlterationStaffKeyword;
                        hitShader = new FormLink<IEffectShaderGetter>();
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
                                hitShader = Skyrim.EffectShader.ReanimateFXShader;
                                break;
                            }
                        case MagicEffectArchetype.TypeEnum.SummonCreature:
                            {
                                perk = MagicDoesThings.Perk._MDT_TemplateConjuSummonReanimateStaffPerk;
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_BanishStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVSummon;
                                hitShader = Skyrim.EffectShader.GhostVioletFXShader;
                                break;
                            }
                        case MagicEffectArchetype.TypeEnum.Reanimate:
                            {
                                perk = MagicDoesThings.Perk._MDT_TemplateConjuSummonReanimateStaffPerk;
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_ReanimateStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVReanimate;
                                hitShader = Skyrim.EffectShader.ReanimateFXShader;
                                break;
                            }
                        case MagicEffectArchetype.TypeEnum.Banish:
                            {
                                perk = MagicDoesThings.Perk._MDT_TemplateAlteConjuNonSummonOrCommandStaffPerk;
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_BanishStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVBanish;
                                hitShader = Skyrim.EffectShader.GhostEtherealFXShader;
                                break;
                            }
                        default:
                            {
                                perk = MagicDoesThings.Perk._MDT_TemplateAlteConjuNonSummonOrCommandStaffPerk;
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_SoulTrapStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVReanimate;
                                hitShader = Skyrim.EffectShader.GhostEtherealFXShader;
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
                        hitShader = Skyrim.EffectShader.FireCloakFXShader;
                    }
                    else if (magicEffect.HasKeyword(Skyrim.Keyword.MagicDamageFrost))
                    {
                        spellBladeKeyword = MagicDoesThings.Keyword._MDT_FrostStaffKeyword;
                        displayObject = Skyrim.Static.MAGINVIceSpellArt;
                        hitShader = Skyrim.EffectShader.FrostFXShader;
                    }
                    else if (magicEffect.HasKeyword(Skyrim.Keyword.MagicDamageShock))
                    {
                        spellBladeKeyword = MagicDoesThings.Keyword._MDT_ShockStaffKeyword;
                        displayObject = Skyrim.Static.MAGINVShockSpellArt;
                        hitShader = Skyrim.EffectShader.ShockPlayerCloakFXShader;
                    }
                    else
                    {
                        spellBladeKeyword = MagicDoesThings.Keyword._MDT_DefaultDestructionStaffKeyword;
                        displayObject = Skyrim.Static.MAGINVFireballArt;
                        hitShader = Skyrim.EffectShader.AbsorbBlueFXS;
                    }
                    break;
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
                                hitShader = Skyrim.EffectShader.IllusionPositiveFXS;
                                break;
                            }
                        case MagicEffectArchetype.TypeEnum.Demoralize:
                            {
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_ConfidenceStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVIllusionDarkt01;
                                hitShader = Skyrim.EffectShader.IllusionNegativeFXS;
                                break;
                            }
                        case MagicEffectArchetype.TypeEnum.Calm:
                            {
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_AggressionStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVIllusionLight01;
                                hitShader = Skyrim.EffectShader.IllusionPositiveFXS;
                                break;
                            }
                        case MagicEffectArchetype.TypeEnum.Frenzy:
                            {
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_AggressionStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVIllusionDarkt01;
                                hitShader = Skyrim.EffectShader.IllusionNegativeFXS;
                                break;
                            }
                        default:
                            {
                                spellBladeKeyword = MagicDoesThings.Keyword._MDT_ConfidenceStaffKeyword;
                                displayObject = Skyrim.Static.MAGINVIllusionLight01;
                                hitShader = Skyrim.EffectShader.IllusionPositiveFXS;
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
                            hitShader = Skyrim.EffectShader.TurnUnFXShader;
                        }
                        else if (magicEffect.ResistValue == ActorValue.None)
                        {
                            displayObject = Skyrim.Static.MAGINVHealSpellArt;
                            spellBladeKeyword = MagicDoesThings.Keyword._MDT_SunStaffKeyword;
                            hitShader = Skyrim.EffectShader.HealFXS;
                        }
                        else
                        {
                            spellBladeKeyword = MagicDoesThings.Keyword._MDT_SunStaffKeyword;
                            displayObject = Skyrim.Static.MAGINVHealSpellArt;  //TODO: change
                            hitShader = Skyrim.EffectShader.HealFXS;
                        }
                    }
                    else if (magicEffect.Archetype.Type == MagicEffectArchetype.TypeEnum.TurnUndead)
                    {
                        displayObject = Skyrim.Static.MAGINVTurnUndead;
                        spellBladeKeyword = MagicDoesThings.Keyword._MDT_TurnStaffKeyword;
                        hitShader = Skyrim.EffectShader.TurnUnFXShader;
                    }
                    else
                    {
                        spellBladeKeyword = MagicDoesThings.Keyword._MDT_SunStaffKeyword;
                        displayObject = Skyrim.Static.MAGINVHealSpellArt;  //TODO: change
                        hitShader = Skyrim.EffectShader.HealFXS;
                    }
                    break;
                }
            default:
                return null;
        }
        return new TemplateRecord(magicSkill, spellBladeKeyword, displayObject, perk, hitShader);
    }

}
