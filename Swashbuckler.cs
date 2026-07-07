using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Emit;
using Dawnsbury.Mods.Phoenix;
using Dawnsbury.Mods.RemasteredSwashbuckler.RegisteredComponents;

namespace Dawnsbury.Mods.RemasteredSwashbuckler;

/// <summary>
/// The Remastered Swashbuckler Class
/// </summary>
public class RemasteredSwashbuckler
{

    /// <summary>
    /// The Flashy Dodge persistent QEffect ID
    /// </summary>
    public static readonly QEffectId FlashyDodgeQEID = ModManager.RegisterEnumMember<QEffectId>("Flashy Dodge QEID");
    
    /// <summary>
    /// The Flashy Roll persistent QEffect ID
    /// </summary>
    public static readonly QEffectId FlashyRollQEID = ModManager.RegisterEnumMember<QEffectId>("Flashy Roll QEID");
    
    /// <summary>
    /// Repeat of AddSwash's CreatePanache method with an optional duration.
    /// <param name="duration">ExpirationCondition that indicates the duration of panache. The default is Never (corresponding to success or critical success).</param>
    /// <returns>panache QEffect</returns>
    /// </summary>
    public static QEffect CreatePanache(ExpirationCondition duration = ExpirationCondition.Never)
    {
        QEffect panache = new QEffect()
        {
            Id = AddSwash.PanacheId,
            Key = "Panache",
            Name = "Panache",
            Illustration = new ModdedIllustration("PhoenixAssets/panache.PNG"),
            Description = "You have a status bonus to your Speed.\n\nYou can use finishers by spending panache.",
            ExpiresAt = duration,
            BonusToAllSpeeds = (delegate(QEffect qfpanache)
            {
                return new Bonus(1, BonusType.Status, "Panache");
            }),
            YouBeginAction = async (qfpanache, action) =>
            {
                if (action.HasTrait(AddSwash.Finisher) && !qfpanache.CannotExpireThisTurn)
                {
                    qfpanache.ExpiresAt = ExpirationCondition.Immediately;
                }
            }
        };
        
        if (duration == ExpirationCondition.CountsDownAtEndOfYourTurn)
            panache.Value = 2;
        else if (duration == ExpirationCondition.ExpiresAtEndOfYourTurn)
            panache.Value = 1;
        
        return panache;
    }

    /// <summary>
    /// Determines if the provided feat should be removed.
    /// </summary>
    /// <param name="feat">The feat being considered for removal</param>
    /// <returns>True if the feat should be removed and false otherwise.</returns>
    public static bool ShouldFeatBeRemoved(Feat feat)
    {
        // For later: Consider Flamboyant Cruelty. The skill bonus is no longer applicable, but the damage bonus is. It's legacy only, so it cannot be changed within this mod, but it may be removed.
        
        bool dummyflag = ModManager.TryParse("Opportune Riposte", out FeatName LegacyOpportuneRiposte);
        // Tumble Behind is now in the base game; add the swashbuckler trait instead.
        dummyflag = ModManager.TryParse("Tumble Behind", out FeatName ModTumbleBehind);
        dummyflag = ModManager.TryParse("LeadingDance", out FeatName ModLeadingDance);
        return feat.FeatName == LegacyOpportuneRiposte ||
               feat.FeatName == ModTumbleBehind ||
               feat.FeatName == ModLeadingDance;
    }
    
    /// <summary>
    /// Returns the Parry trait - should already exist in the legacy Swashbuckler mod.
    /// </summary>
    /// <returns>Trait object for the Parry trait</returns>
    public static Trait GetParryTrait()
    {
        bool dummyflag = ModManager.TryParse("Parry", out Trait parryTrait);
        return parryTrait;
    }

    /// <summary>
    /// Creates the Remastered Swashbuckler Feats
    /// </summary>
    /// <returns>The Enumerable of Swashbuckler Feats</returns>
    public static IEnumerable<Feat> CreateRemasteredSwashbucklerFeats()
    {
        // Define local copy of the parry trait.
        Trait parry = GetParryTrait();
        
        // Extravagant Parry
        yield return new TrueFeat(RemasteredSwashbucklerFeatNames.ExtravagantParry, 1, "You use one-handed weapons to parry with style.", "You gain a +1 circumstance bonus to your AC until the start of your next turn, or a +2 circumstance bonus if you have a free hand or are wielding a weapon with the parry trait. You lose this circumstance bonus if you no longer meet this feat's requirements. If a creature misses you with a Strike while you have this bonus, you gain Panache until the end of your next turn.", new Trait[] { AddSwash.SwashTrait })
        .WithPermanentQEffect("You gain a circumstance bonus to AC using your two held weapons.", delegate (QEffect qf)
        {
                qf.ProvideMainAction = qftechnical =>
                {
                    if ((qf.Owner.PrimaryItem != null && qf.Owner.SecondaryItem != null) || ((qf.Owner.PrimaryItem != null || qf.Owner.SecondaryItem != null) && qf.Owner.HasFreeHand))
                    {
                        if (qf.Owner.PrimaryItem.HasTrait(Trait.Weapon) || (qf.Owner.SecondaryItem.HasTrait(Trait.Weapon)) )
                        {
                            return new ActionPossibility(new CombatAction(qf.Owner, IllustrationName.Swords, "Extravagant Parry", new Trait[0] { }, "You use your weapons to block oncoming attacks and increase your AC.", Target.Self())
                                .WithActionCost(1)
                                .WithEffectOnEachTarget(async (caster, spell, target, result) =>
                                {
                                    QEffect parrybonus = new QEffect();
                                    parrybonus.Name = "Extravagant Parry";
                                    parrybonus.Illustration = IllustrationName.Swords;
                                    parrybonus.Description = "You have a +2 circumstance bonus to AC from using your weapons to block.";
                                    parrybonus.ExpiresAt = ExpirationCondition.ExpiresAtStartOfYourTurn;
                                    qf.AfterYouAreTargeted = async delegate (QEffect qf, CombatAction action)
                                    {
                                        if (action.HasTrait(Trait.Attack) && (action.CheckResult == CheckResult.CriticalFailure || action.CheckResult == CheckResult.Failure))
                                        {
                                            qf.Owner.AddQEffect(CreatePanache(ExpirationCondition.ExpiresAtEndOfYourTurn));
                                        }
                                    };
                                    parrybonus.BonusToDefenses = delegate (QEffect thing, CombatAction? bonk, Defense defense)
                                    {
                                        if (parrybonus.Owner.PrimaryItem == null && parrybonus.Owner.SecondaryItem == null) return null;
                                        if (defense == Defense.AC)
                                        {
                                            if (parrybonus.Owner.HasFreeHand)
                                            {
                                                return new Bonus(2, BonusType.Circumstance, "Extravagant Parry");
                                            }
                                            else if (parrybonus.Owner.PrimaryItem != null)
                                            {
                                                if (parrybonus.Owner.PrimaryItem.HasTrait(parry))
                                                {
                                                    return new Bonus(2, BonusType.Circumstance, "Extravagant Parry");
                                                }
                                            }
                                            else if (parrybonus.Owner.SecondaryItem != null)
                                            {
                                                if (parrybonus.Owner.SecondaryItem.HasTrait(parry))
                                                {
                                                    return new Bonus(2, BonusType.Circumstance, "Extravagant Parry");
                                                }
                                            }
                                            return new Bonus(1, BonusType.Circumstance, "Extravagant Parry");
                                        }
                                        else return null;
                                    };
                                    parrybonus.StateCheck = qfdw =>
                                    {
                                        //Remove the bonus if a non-weapon item is added to your hand, or if the weapon count drops below zero.
                                        int weaponCount = 0;
                                        int nonweaponCount = 0;
                                        if (qfdw.Owner.PrimaryItem != null)
                                        {
                                            if (qfdw.Owner.PrimaryItem.HasTrait(Trait.Weapon))
                                            {
                                                weaponCount++;
                                            }
                                            else
                                            {
                                                nonweaponCount++;
                                            }
                                        }
                                        if (qfdw.Owner.SecondaryItem != null)
                                        {
                                            if (qfdw.Owner.SecondaryItem.HasTrait(Trait.Weapon))
                                            {
                                                weaponCount++;
                                            }
                                            else
                                            {
                                                nonweaponCount++;
                                            }
                                        }
                                        if (weaponCount == 0 || nonweaponCount > 0)
                                        {
                                            parrybonus.Owner.RemoveAllQEffects((QEffect effect) => effect.Name == "Extravagant Parry");
                                        }
                                    };
                                    target.AddQEffect(parrybonus);
                                })
                                .WithSoundEffect(SfxName.RaiseShield));
                        }
                    }
                    return null;
                };
        });
    
        // Leading Dance
        yield return new TrueFeat(RemasteredSwashbucklerFeatNames.LeadingDance, 4, "You sweep your foe into your dance.", "Attempt a Performance check against an adjacent enemy's Will DC." + S.FourDegreesOfSuccess("Your foe is swept up in your dance. You both move 10 feet in the same direction. Your movement doesn't trigger reactions (and the enemy's movement doesn't trigger reactions because it's forced movement). You gain panache.", "As critical success, but you both only move 5 feet.", "The foe doesn't follow your steps. You can move 5 feet if you choose, but this movement triggers reactions normally. You gain panache until the end of your next turn.", "You stumble, falling prone in your space."), new Trait[] { AddSwash.SwashTrait, Trait.Move, RemasteredSwashbucklerTraits.BravadoTrait }, null)
        .WithPermanentQEffect(delegate (QEffect qf)
        {
            qf.ProvideActionIntoPossibilitySection = delegate (QEffect effect, PossibilitySection section)
            {
                if (section.PossibilitySectionId == PossibilitySectionId.SkillActions)
                {
                    return new ActionPossibility(new CombatAction(effect.Owner, IllustrationName.WarpStep, "Leading Dance", new Trait[] { Trait.Move, RemasteredSwashbucklerTraits.BravadoTrait }, "Attempt a Performance check against an adjacent enemy's Will DC." + S.FourDegreesOfSuccess("Your foe is swept up in your dance. You both move 10 feet in the same direction. Your movement doesn't trigger reactions (and the enemy's movement doesn't trigger reactions because it's forced movement). You gain panache.", "As critical success, but you both only move 5 feet.", "The foe doesn't follow your steps. You can move 5 feet if you choose, but this movement triggers reactions normally. You gain panache until the end of your next turn.", "You stumble, falling prone in your space."),
                            Target.Touch())
                        .WithActionCost(1)
                        .WithActiveRollSpecification(new ActiveRollSpecification(TaggedChecks.SkillCheck(Skill.Performance), Checks.DefenseDC(Defense.Will)))
                        .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                        {
                            switch (result)
                            {
                                case CheckResult.CriticalSuccess:
                                    //Need a way to move only up to 10 feet.
                                    target.AddQEffect(new QEffect(ExpirationCondition.Never)
                                    {
                                        Id = QEffectId.IgnoreAoOWhenMoving,
                                        Name = "LeadingDanceReaction"                                     
                                    });
                                    await caster.StrideAsync("Choose a location to move to.", false, true, null, false, true, false);
                                    await caster.StrideAsync("Choose a location to move to.", false, true, null, false, true, false);
                                    await caster.PullCreature(target);
                                    caster.AddQEffect(CreatePanache());
                                    target.RemoveAllQEffects((QEffect effect) => effect.Name == "LeadingDanceReaction");
                                    break;
                                case CheckResult.Success:
                                    //Need to make sure this doesn't trigger reactions.
                                    target.AddQEffect(new QEffect(ExpirationCondition.Never)
                                    {
                                        Id = QEffectId.IgnoreAoOWhenMoving,
                                        Name = "LeadingDanceReaction"
                                    });
                                    await caster.StrideAsync("Choose a location to move to.", false, true, null, false, true, false);
                                    await caster.PullCreature(target);
                                    caster.AddQEffect(CreatePanache());
                                    target.RemoveAllQEffects((QEffect effect) => effect.Name == "LeadingDanceReaction");
                                    break;
                                case CheckResult.Failure:
                                    await caster.StrideAsync("Choose a location to move to.", false, true, null, false, true, false);
                                    caster.AddQEffect(CreatePanache(ExpirationCondition.CountsDownAtEndOfYourTurn));
                                    break;
                                case CheckResult.CriticalFailure:
                                    await caster.FallProne();
                                    break;
                            }
                        }));
                }
                else return null;
            };
        })
        .WithPrerequisite(FeatName.Performance, "Must be trained in Performance.");
        
        // Flashy Dodge
        yield return new TrueFeat(RemasteredSwashbucklerFeatNames.FlashyDodge, 1, null, "You deftly dodge out of the way, gaining a +2 circumstance bonus to AC against the triggering attack. If the Strike misses, you gain panache until the end of your next turn.", new Trait[] { AddSwash.SwashTrait }, null)
        .WithPermanentQEffect("You gain a +2 bonus to AC as a reaction", delegate (QEffect qf)
        {
            qf.YouAreTargeted = async (QEffect qf2, CombatAction action) =>
            {
                if (action.HasTrait(Trait.Attack))
                {
                    if (await qf.Owner.Battle.AskToUseReaction(qf.Owner, action.Owner.Name + " is attacking you.\nUse reaction to gain +2 circumstance bonus to AC for this attack?"))
                    {
                        qf.Owner.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfAnyTurn)
                        {
                            Id = FlashyDodgeQEID,
                            BonusToDefenses = delegate (QEffect qf3, CombatAction? action3, Defense defense)
                            {
                                if (defense == Defense.AC)
                                {
                                    return new Bonus(2, BonusType.Circumstance, "Flashy Dodge");
                                }
                                else return null;
                            }
                        });
                    }
                }
            };
            
            qf.AfterYouAreTargeted = async (QEffect qf2, CombatAction action) =>
            {
                if (qf.Owner.HasEffect(FlashyDodgeQEID))
                {
                    if (action.CheckResult == CheckResult.Failure || action.CheckResult == CheckResult.CriticalFailure)
                    {
                        qf.Owner.AddQEffect(CreatePanache(ExpirationCondition.ExpiresAtEndOfYourTurn));
                        bool flashyRollFlag = qf.Owner.PersistentCharacterSheet?.Calculated.AllFeats.Any(feat => feat.FeatName == RemasteredSwashbucklerFeatNames.FlashyRoll) ?? false;
                        if (flashyRollFlag)
                        {
                            await qf.Owner.StrideAsync("Choose a location to move to.", false, true, null, false, true, false);
                            await qf.Owner.StrideAsync("Choose a location to move to.", false, true, null, false, true, false);
                        }
                    }
                    qf.Owner.RemoveAllQEffects((QEffect qf3) => qf3.Id == FlashyDodgeQEID);
                }
            };
        });
        
        // Flashy Roll
        yield return new TrueFeat(RemasteredSwashbucklerFeatNames.FlashyRoll, 8, null, "You can use Flashy Dodge before attempting a Reflex save, in addition to its original trigger. If you do, the circumstance bonus applies to your Reflex save against the triggering effect.\n\nWhen you use Flashy Dodge and the triggering attack fails or critically fails, or when you succeed or critically succeed at the saving throw, you can also Stride up to 10 feet as part of the reaction. If you do, the reaction gains the move trait. You can use Flashy Roll while Flying or Swimming instead of Striding if you have the corresponding movement type.", new Trait[] { AddSwash.SwashTrait }, null)
        .WithPermanentQEffect("You gain a +2 bonus to AC as a reaction", delegate (QEffect qf)
        {
            qf.YouAreTargeted = async (QEffect qf2, CombatAction action) =>
            {
                if (action.SavingThrow?.Defense == Defense.Reflex)
                {
                    if (await qf.Owner.Battle.AskToUseReaction(qf.Owner, action.Owner.Name + " has targeted you with a Reflex effect.\nUse reaction to gain +2 circumstance bonus to Reflex for this effect?"))
                    {
                        qf.Owner.AddQEffect(new QEffect(ExpirationCondition.ExpiresAtEndOfAnyTurn)
                        {
                            Id = FlashyRollQEID,
                            BonusToDefenses = delegate (QEffect qf3, CombatAction? action3, Defense defense)
                            {
                                if (defense == Defense.Reflex)
                                {
                                    return new Bonus(2, BonusType.Circumstance, "Flashy Dodge");
                                }
                                else return null;
                            }
                        });
                    }
                }
            };
            
            qf.AfterYouAreTargeted = async (QEffect qf2, CombatAction action) =>
            {
                if (qf.Owner.HasEffect(FlashyRollQEID))
                {
                    if (action.CheckResult == CheckResult.Success || action.CheckResult == CheckResult.CriticalSuccess)
                    {
                        qf.Owner.AddQEffect(CreatePanache(ExpirationCondition.ExpiresAtEndOfYourTurn));
                        await qf.Owner.StrideAsync("Choose a location to move to.", false, true, null, false, true, false);
                        await qf.Owner.StrideAsync("Choose a location to move to.", false, true, null, false, true, false);
                    }
                    qf.Owner.RemoveAllQEffects((QEffect qf3) => qf3.Id == FlashyRollQEID);
                }
            };
        }).WithPrerequisite((CalculatedCharacterSheetValues sheet) => sheet.HasFeat(RemasteredSwashbucklerFeatNames.FlashyDodge), "You must have Flashy Dodge.");
        
        // Tumble Behind (level 2 Swashbuckler version)
        yield return CommonFeatTemplates.CreateDuplicateFeatForDifferentClass(FeatName.TumbleBehind, RemasteredSwashbucklerFeatNames.RemasterSwashTumbleBehind, 2, AddSwash.SwashTrait);
    }

    /// <summary>
    /// Patches all feats for the Remastered Swashbuckler
    /// </summary>
    /// <param name="feat">The feat to patch</param>
    public static void PatchSwashFeats(Feat feat)
    { 
        bool dummyflag = ModManager.TryParse("Swashbuckler", out FeatName LegacySwashbuckler);
        
        if (feat.FeatName == LegacySwashbuckler && feat is ClassSelectionFeat classSelectionFeat)
        {
            // Add the Add Panache action.
            // Also add skill feats at levels 3, 7, and 15. These should be restricted skill feats, buuut that takes more effort.
            feat.WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
            {
                sheet.AddFeat(AddPanache!, null);
                sheet.AddSkillFeatOption(3);
                sheet.AddSkillFeatOption(7);
                sheet.AddSkillFeatOption(15);
            });
            
            // Update class description.
            UpdateClassDescription(classSelectionFeat);
            
            // Update skill scaling. For the remaster, the swashbuckler gets an extra skill advancement at levels 3, 7, and 15
            // which must be applied to either Acrobatics or their style skill. For convenience, it is implemented as only
            // advancing the style skill.
             classSelectionFeat.Subfeats.ForEach(subClass =>
             {
                 switch (subClass.Name)
                 {
                     case "Battledancer":
                         UpdateBattledancer(subClass);
                         break;
                     case "Braggart":
                         UpdateBraggart(subClass);
                         break;                        
                     case "Fencer":
                         UpdateFencer(subClass);
                         break;
                     case "Gymnast":
                         UpdateGymnast(subClass);
                         break;                        
                     case "Wit":
                         UpdateWit(subClass);
                         break;                        
                 }                    
             });
             
            // Add Stylish Combatant to add the circumstance bonus to relevant skills always.
            AddStylishCombatant(classSelectionFeat);
            
            // Update Precise Strike to apply without panache.
            PatchPreciseStrike(classSelectionFeat);
           
            // Add the ability to gain panache on a failure.
            feat.WithOnCreature(delegate (Creature creature)
            {
                creature.AddQEffect(PanacheGranterFailure());
            });
            
            // Patch Opportune Riposte.
            feat.WithOnCreature(delegate (Creature creature)
            {
                if (creature.Level >= 3)
                {
                    creature.AddQEffect(PatchOpportuneRiposte());
                }
            });
            
            // Patch One for All effect
            PatchOneForAllAid(feat);
        }
            
        // Apply changes to other feats as well
        // Precise Strike QEffect was already updated, but also update the feat description.
        dummyflag = ModManager.TryParse("PreciseStrike", out FeatName LegacyPreciseStrike);
        if (feat.FeatName == LegacyPreciseStrike)
            PatchPreciseStrikeText(feat);
        
        // Disarming Flair (add panache for all styles)
        // QEffect is handled in AddStylishCombatant, but update feat rules text as well.
        dummyflag = ModManager.TryParse("Disarming Flair", out FeatName LegacyDisarmingFlair);
        if (feat.FeatName == LegacyDisarmingFlair)
        {
            feat.RulesText.Replace("If your swashbuckler style is gymnast and you succeed at your Athletics check to Disarm a foe, you gain panache.", "Additionally, your Disarm attempts gain the bravado trait; if you succeed, you gain panahce, and if you fail (but don't critically fail), you gain panache until the end of your turn.");
        }
        
        // One For All (use bravado trait for the reaction)
        // QEffect is handled in PatchOneForAllAid; update the rules text
        dummyflag = ModManager.TryParse("One For All", out FeatName LegacyOneForAll);
        if (feat.FeatName == LegacyOneForAll)
        {
            feat.RulesText = feat.RulesText.Replace("If your swashbuckler style is Wit, you gain panache.", "You gain panache.");   
            feat.RulesText = feat.RulesText.Replace("{b}Critical Failure:{/b}", "{b}Failure:{/b} You gain panache, but it expires at the end of your next turn.\n{b}Critical Failure:{/b}"); 
        }
        
        // Flying Blade (applies without panache)
        dummyflag = ModManager.TryParse("FlyingBlade", out FeatName LegacyFlyingBlade);
        if (feat.FeatName == LegacyFlyingBlade)
        {
            feat.RulesText = feat.RulesText.Replace("When you have panache, y", "Y");
        }
        
        
        // Remove the Swashbuckler trait from the following 
        // (but keep in mind that they are still valid for Fighters, Rangers, and/or Rogues)
        //      Dueling Parry (replaced with Extravagant Parry)
        //      Twin Parry (replaced with Extravagant Parry)
        //      Nimble Dodge (replaced with Flashy Dodge)
        //      Nimble Roll (replaced with Flashy Roll)
        dummyflag = ModManager.TryParse("DuelingParrySwash", out FeatName LegacyDuelingParry);
        if (feat.FeatName == LegacyDuelingParry)
        {
            feat.Traits.Remove(AddSwash.SwashTrait);
        }
        
        dummyflag = ModManager.TryParse("Twin Parry", out FeatName LegacyTwinParry);
        if (feat.FeatName == LegacyTwinParry)
        {
            feat.Traits.Remove(AddSwash.SwashTrait);
        }
        
        if (feat.FeatName == FeatName.NimbleDodge)
        {
            feat.Traits.Remove(AddSwash.SwashTrait);
        }
        
        if (feat.FeatName == FeatName.NimbleRoll)
        {
            feat.Traits.Remove(AddSwash.SwashTrait);
        }
        
        dummyflag = ModManager.TryParse(AddSwash.SwashTrait.ToStringOrTechnical() + "Dedication", out FeatName LegacySwashDedication);
        
        // Make similar changes for multiclass characters.
        if (feat.FeatName == LegacySwashDedication)
        {
            // Add the Add Panache action for the same reasons as the normal class.
            feat.WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
            {
                sheet.AddFeat(AddPanache!, null);
            });
            AddMCStylishCombatant(feat);
            
            // Add the ability to gain panache on a failure.
            feat.WithOnCreature(delegate (Creature creature)
            {
                creature.AddQEffect(PanacheGranterFailure());
            });
            
            // Patch One for All effect
            PatchOneForAllAid(feat);
            
            // Fix bug in legacy Swashbuckler dedication: add list of skill actions from each style to the Panache Granter.
            // Since I don't plan on monitoring the legacy mod to see if it's fixed, check to see if it already has been fixed.
            feat.WithOnCreature(delegate (Creature swash)
            {
                QEffect panacheGranter = swash.QEffects.First((QEffect fct) => fct.Key == "PanacheGranter");
                List<ActionId> list = (List<ActionId>)panacheGranter.Tag;
    
                AddSwash.SwashbucklerStyle style = (AddSwash.SwashbucklerStyle)swash.PersistentCharacterSheet.Calculated.AllFeats.Find(feat => feat.HasTrait(AddSwash.SwashStyle));
                switch (style.Name)
                {
                    case "Battledancer":
                        if (!list.Contains(AddSwash.FascinatingPerformanceActionId))
                        {
                            list.Add(AddSwash.FascinatingPerformanceActionId);
                            panacheGranter.Description += ", Fascinating Performance";
                        }
                        break;
                    case "Braggart":
                        if(!list.Contains(ActionId.Demoralize))
                        {
                            list.Add(ActionId.Demoralize);
                            panacheGranter.Description += ", Demoralize";
                        }
                        break;                        
                    case "Fencer":
                        if(!list.Contains(ActionId.Feint))
                        {
                            list.Add(ActionId.Feint);
                            list.Add(ActionId.CreateADiversion);
                            panacheGranter.Description += ", Feint";
                            panacheGranter.Description += ", Create a Diversion";
                        }
                        break;
                    case "Gymnast":
                        if(!list.Contains(ActionId.Grapple))
                        {
                            list.Add(ActionId.Grapple);
                            list.Add(ActionId.Shove);
                            list.Add(ActionId.Trip);
                            panacheGranter.Description += ", Grapple, Shove, Trip";
                        }
                        break;                        
                    case "Wit":
                        if(!list.Contains(ActionId.BonMot))
                        {
                            list.Add(ActionId.BonMot);
                            panacheGranter.Description += ", Bon Mot";
                        }
                        break;                        
                }                    
            });
        }
        
        // Patch the Opportune Riposte reaction for multiclass characters.
        dummyflag = ModManager.TryParse("SwashbucklersRiposte", out FeatName LegacySwashbucklersRiposte);
        if (feat.FeatName == LegacySwashbucklersRiposte)
        {
            feat.WithOnCreature(delegate (Creature creature)
            {
                creature.AddQEffect(PatchOpportuneRiposte());
            });
        }
        
        // Patch Precise Strike for multiclass characters.
        dummyflag = ModManager.TryParse("FinishingPrecision", out FeatName LegacyFinishingPrecision);
        if (feat.FeatName == LegacyFinishingPrecision)
        {        
            PatchPreciseStrike(feat);
        }
        

        /*
        // Guardian's Deflection (grants panache)
        // Fixme: since the QEffect is added to an ally, this is a lot more difficult.
        // It would be easier if I could monitor the QEffect which is added to allies to provide the bonus to defenses, but it does not have a specified name or ID.
        if (feat.FeatName == FeatName.GuardiansDeflection)
        {
        }
        */
    
    }
    
    //For the remaster, this feat is created to account for how bravado actions can be used with no other effect than to generate panache. In the TTRPG, there is the possibility of success and critical failure; as a compromise to avoid needing to work around immunities, this just assumes failure.
    //Consider it compensation for missing the bonus skill feats at levels 3 and 7 which could be used for assurance.
    public static Feat AddPanache = new TrueFeat(ModManager.RegisterFeatName("Add Panache", "Add Panache {icon:Action}"), 1, "Bravado workaround: You use an action with the bravado trait without a target that would result in an effect.", "You take a failure on the bravado action, giving yourself panache which expires at the end of your turn.", new Trait[1] { AddSwash.SwashTrait }, null)
        .WithPermanentQEffect(null, delegate (QEffect qf)
        {
            qf.ProvideActionIntoPossibilitySection = (qfpanache, section) =>
            {
                if (section.PossibilitySectionId == PossibilitySectionId.SkillActions)
                {
                    return new ActionPossibility(new CombatAction(qf.Owner, new ModdedIllustration("PhoenixAssets/panache.PNG"), "Add Panache", new Trait[1] { Trait.Concentrate }, "You give yourself panache which expires at the end of your next turn.", Target.Self())
                        .WithActionCost(1)
                        .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                        {
                            AddSwash.SwashbucklerStyle style = (AddSwash.SwashbucklerStyle)caster.PersistentCharacterSheet.Calculated.AllFeats.Find(feat => feat.HasTrait(AddSwash.SwashStyle));
                            target.AddQEffect(CreatePanache(ExpirationCondition.CountsDownAtEndOfYourTurn));
                        }));
                }
                else return null;
            };
        });

    /// <summary>
    /// Updates the Swaschbuckler class description for the remaster changes.
    /// </summary>
    /// <param name="subClass">The Swashbuckler Class Selection Feat</param>
    private static void UpdateClassDescription(ClassSelectionFeat classSelectionFeat)
    {
        // TODO: implement this such that it overwrites instead of duplicates the legacy swashbuckler "at higher levels" stuff.
        //classSelectionFeat.RulesText = "{b}1. Panache.{/b} You learn how to leverage your skills to enter a state of heightened ability called panache. You gain panache by performing actions with the Bravado trait, including Tumble Through and other checks determined by your style. On a success, you gain panache until you lose it, while on a failure (but not critical failure), you gain panache until  the end of your next turn (denoted in Dawnsbury Days by a panache value). While you have panache, you gain a +5 circumstance bonus to your Speed. It also allows you to use special attacks called finishers, which cause you to lose panache when performed.\n{b}2. Swashbuckler style.{/b} You choose a style that represents what kind of flair you bring to a battlefield. When you choose a style, you become trained in a skill and can use certain actions using that skill to gain panache.\n{b}3. Precise Strike.{/b} You deal an extra 2 precision damage with your agile or finesse melee weapons. If you use a finisher, the damage increases to 2d6 instead.\n{b}4. Confident Finisher.{/b} If you have panache, you can use an action to make a Strike against an enemy in melee range. If you miss, you deal half your Precise Strike damage.\n{b}5. Stylish Combatant.{/b} You gain a +1 circumstance bonus to skill checks with the bravado trait. \n{b}6. Swashbuckler feat.{/b}\n\n{b}At Higher Levels:\nLevel 2:{/b} Swashbuckler feat.\n{b}Level 3:{/b} General feat, fortitude expertise, skill increase, style skill expert, Opportune Riposte{i}(counterattack if an enemy critically fails to hit you){/i}, Vivacious Speed{i}(The status bonus from panache increases and you gain half of it even if you don't have panache){/i}\n{b}Level 4:{/b} Swashbuckler feat.\n{b}Level 5:{/b} Ability boosts, ancestry feat, skill increase, precise strike 3d6, weapon expertise {i}(You gain expertise in simple and martial weapons, and gain access to the critical specialization effects of all weapons for which you have expert proficiency.){i}\n{b}Level 6:{/b} Swashbuckler feat.\n{b}Level 7:{/b} Confident Evasion {i}(Your proficiency in Reflex saves increases to master, and when you roll a success, you get a critical success instead.){/i}, general feat, skill increase, style skill master, vivacious speed 15 feet, weapon specialization {i}(You deal 2 additional damage using weapons with which you have expert proficiency).{/i}\n{b}Level 8:{/b} Swashbuckler feat.\n{b}Level 9:{/b} Ancestry feat, exemplary finisher, precise strike 4d6, skill increase, swashbuckler expertise.";
        classSelectionFeat.RulesText = "{b}1. Panache.{/b} You learn how to leverage your skills to enter a state of heightened ability called panache. You gain panache by performing actions with the Bravado trait, including Tumble Through and other checks determined by your style. On a success, you gain panache until you lose it, while on a failure (but not critical failure), you gain panache until  the end of your next turn (denoted in Dawnsbury Days by a panache value). While you have panache, you gain a +5 circumstance bonus to your Speed. It also allows you to use special attacks called finishers, which cause you to lose panache when performed.\n{b}2. Swashbuckler style.{/b} You choose a style that represents what kind of flair you bring to a battlefield. When you choose a style, you become trained in a skill and can use certain actions using that skill to gain panache.\n{b}3. Precise Strike.{/b} You deal an extra 2 precision damage with your agile or finesse melee weapons. If you use a finisher, the damage increases to 2d6 instead.\n{b}4. Confident Finisher.{/b} If you have panache, you can use an action to make a Strike against an enemy in melee range. If you miss, you deal half your Precise Strike damage.\n{b}5. Stylish Combatant.{/b} You gain a +1 circumstance bonus to skill checks with the bravado trait. \n{b}6. Swashbuckler feat.{/b}";
    }
    
    /// <summary>
    /// Updates the Battledancer subclass to provide bonus performance skill proficiencies at levels 3 and 7.
    /// </summary>
    /// <param name="subClass">The Battledancer subclass Feat</param>
    private static void UpdateBattledancer(Feat subClass)
    {
        subClass.RulesText = subClass.RulesText.Replace("You gain panache whenever your Performance check exceeds the Will DC of an observing foe, even if that foe isn't fascinated.", "When you Perform, the action gains the bravado trait, allowing you to gain panache on any result aside from a critical failure. If the result is a failure, your panache only lasts until the end of your next turn.");
        subClass.WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
        {
            sheet.AddAtLevel(3,  (Action<CalculatedCharacterSheetValues>) (v7 => v7.GrantFeat(FeatName.ExpertPerformance)));
            sheet.AddAtLevel(7,  (Action<CalculatedCharacterSheetValues>) (v7 => v7.GrantFeat(FeatName.MasterPerformance)));
            sheet.AddAtLevel(15, (Action<CalculatedCharacterSheetValues>) (v7 => v7.GrantFeat(FeatName.LegendaryPerformance)));
        });
    }

    /// <summary>
    /// Updates the Braggart subclass to provide bonus intimidation skill proficiencies at levels 3 and 7.
    /// </summary>
    /// <param name="subClass">The Braggary subclass Feat</param>
    private static void UpdateBraggart(Feat subClass)
    {
        subClass.RulesText = subClass.RulesText.Replace("You gain panache whenever you successfully Demoralize a foe.", "When you Demoralize, the action gains the bravado trait, allowing you to gain panache on any result aside from a critical failure. If the result is a failure, your panache only lasts until the end of your next turn.");
        subClass.WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
        {
            sheet.AddAtLevel(3,  (Action<CalculatedCharacterSheetValues>) (v7 => v7.GrantFeat(FeatName.ExpertIntimidation)));
            sheet.AddAtLevel(7,  (Action<CalculatedCharacterSheetValues>) (v7 => v7.GrantFeat(FeatName.MasterIntimidation)));
            sheet.AddAtLevel(15, (Action<CalculatedCharacterSheetValues>) (v7 => v7.GrantFeat(FeatName.LegendaryIntimidation)));
        });
    }
    
    /// <summary>
    /// Updates the Fencer subclass to provide bonus deception skill proficiencies at levels 3 and 7.
    /// </summary>
    /// <param name="subClass">The Fencer subclass Feat</param>
    private static void UpdateFencer(Feat subClass)
    {
        subClass.RulesText = subClass.RulesText.Replace("You gain panache whenever you successfully Feint or Create a Diversion.", "When you Create a Distraction or Feint, the action gains the bravado trait, allowing you to gain panache on any result aside from a critical failure. If the result is a failure, your panache only lasts until the end of your next turn.");
        subClass.WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
        {
            sheet.AddAtLevel(3,  (Action<CalculatedCharacterSheetValues>) (v7 => v7.GrantFeat(FeatName.ExpertDeception)));
            sheet.AddAtLevel(7,  (Action<CalculatedCharacterSheetValues>) (v7 => v7.GrantFeat(FeatName.MasterDeception)));
            sheet.AddAtLevel(15, (Action<CalculatedCharacterSheetValues>) (v7 => v7.GrantFeat(FeatName.LegendaryDeception)));
        });
    }

    /// <summary>
    /// Updates the Gymnast subclass to provide bonus athletics skill proficiencies at levels 3 and 7.
    /// </summary>
    /// <param name="subClass">The Gymnast subclass Feat</param>
    private static void UpdateGymnast(Feat subClass)
    {
        subClass.RulesText = subClass.RulesText.Replace("You gain panache whenever you successfully Grapple, Shove, or Trip a foe.", "When you Grapple, Shove, or Trip, the action gains the bravado trait, allowing you to gain panache on any result aside from a critical failure. If the result is a failure, your panache only lasts until the end of your next turn.");
        subClass.WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
        {
            sheet.AddAtLevel(3,  (Action<CalculatedCharacterSheetValues>) (v7 => v7.GrantFeat(FeatName.ExpertAthletics)));
            sheet.AddAtLevel(7,  (Action<CalculatedCharacterSheetValues>) (v7 => v7.GrantFeat(FeatName.MasterAthletics)));
            sheet.AddAtLevel(15, (Action<CalculatedCharacterSheetValues>) (v7 => v7.GrantFeat(FeatName.LegendaryAthletics)));
        });
    }

    /// <summary>
    /// Updates the Wit subclass to provide bonus diplomacy skill proficiencies at levels 3 and 7.
    /// </summary>
    /// <param name="subClass">The Wit subclass Feat</param>
    private static void UpdateWit(Feat subClass)
    {
        subClass.RulesText = subClass.RulesText.Replace("You gain panache whenever you successfully use Bon Mot on a foe.", "When you use Bon Mot, the action gains the bravado trait, allowing you to gain panache on any result aside from a critical failure. If the result is a failure, your panache only lasts until the end of your next turn.");
        subClass.WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
        {
            sheet.AddAtLevel(3,  (Action<CalculatedCharacterSheetValues>) (v7 => v7.GrantFeat(FeatName.ExpertDiplomacy)));
            sheet.AddAtLevel(7,  (Action<CalculatedCharacterSheetValues>) (v7 => v7.GrantFeat(FeatName.MasterDiplomacy)));
            sheet.AddAtLevel(15, (Action<CalculatedCharacterSheetValues>) (v7 => v7.GrantFeat(FeatName.LegendaryDiplomacy)));
        });
    }

    /// <summary>
    /// Adds the Stylish Combatant QEffect to Swashbucklers.
    /// </summary>
    /// <param name="classSelectionFeat">The Swashbuckler Class Selection Feat</param>
    private static void AddStylishCombatant(Feat classSelectionFeat)
    {
        Feat feat;
        bool dummyflag = ModManager.TryParse("Disarming Flair", out FeatName DisarmingFlair);
        classSelectionFeat.WithOnCreature(delegate (Creature creature)
        {
            creature.AddQEffect(new QEffect("Stylish Combatant", "You have +1 circumstance bonus to bravado skill checks.")
            {
                BonusToSkillChecks = delegate (Skill skill, CombatAction action, Creature target)
                {
                    AddSwash.SwashbucklerStyle style = (AddSwash.SwashbucklerStyle)creature.PersistentCharacterSheet.Calculated.AllFeats.Find(feat => feat.HasTrait(AddSwash.SwashStyle));
                    if (style == null) return null; // no subclass selected yet
                    int bonus_val = 1;
                    if (creature.Level >= 9)
                        bonus_val = 2;
                    if (action.ActionId == ActionId.TumbleThrough || style.PanacheTriggers.Contains(action.ActionId))
                    {
                        return new Bonus(bonus_val, BonusType.Circumstance, "Stylish Combatant");
                    }
                    else if (action.HasTrait(RemasteredSwashbucklerTraits.BravadoTrait)) // note: should only apply to skill actions. Currently, only skill actions have the actual Bravado trait (although opportune riposte should).
                    {
                        return new Bonus(bonus_val, BonusType.Circumstance, "Stylish Combatant");
                    }
                    else if (creature.HasFeat(DisarmingFlair) && action.ActionId == ActionId.Disarm)
                    {
                        return new Bonus(bonus_val, BonusType.Circumstance, "Stylish Combatant");
                    }
                    else return null;
                }
            });
        });
    }

    /// <summary>
    /// Adds the Stylish Combatant QEffect to multiclass Swashbucklers.
    /// </summary>
    /// <param name="dedicationFeat">The Swashbuckler Dedication Feat</param>
    private static void AddMCStylishCombatant(Feat dedicationFeat)
    {
        Feat feat;
        bool dummyflag = ModManager.TryParse("Disarming Flair", out FeatName DisarmingFlair);
        dedicationFeat.WithOnCreature(delegate (Creature creature)
        {
            creature.AddQEffect(new QEffect("Stylish Combatant", "You have +1 circumstance bonus to bravado skill checks.")
            {
                BonusToSkillChecks = delegate (Skill skill, CombatAction action, Creature target)
                {
                    AddSwash.SwashbucklerStyle style = (AddSwash.SwashbucklerStyle)creature.PersistentCharacterSheet.Calculated.AllFeats.Find(feat => feat.HasTrait(AddSwash.SwashStyle));
                    if (style == null) return null; // no subclass selected yet
                    int bonus_val = 1;
                    if (action.ActionId == ActionId.TumbleThrough || style.PanacheTriggers.Contains(action.ActionId))
                    {
                        return new Bonus(bonus_val, BonusType.Circumstance, "Stylish Combatant");
                    }
                    else if (action.HasTrait(RemasteredSwashbucklerTraits.BravadoTrait)) // note: should only apply to skill actions. Currently, only skill actions have the actual Bravado trait (although opportune riposte should).
                    {
                        return new Bonus(bonus_val, BonusType.Circumstance, "Stylish Combatant");
                    }
                    else if (creature.HasFeat(DisarmingFlair) && action.ActionId == ActionId.Disarm)
                    {
                        return new Bonus(bonus_val, BonusType.Circumstance, "Stylish Combatant");
                    }
                    else return null;
                }
            });
        });
    }

    /// <summary>
    /// Updates the Precise Strike QEffect to provide its damage bonus even without panache.
    /// I'm having difficulty modifying the existing QEffect, so I'll just add a duplicate that only applies without panache.
    /// </summary>
    /// <param name="classSelectionFeat">The Swashbuckler Class Selection Feat</param>
    private static void PatchPreciseStrike(Feat classSelectionFeat)
    {
        /* classSelectionFeat.WithPermanentQEffect(null, (QEffect qEffect) =>
           {
               qEffect.YouAcquireQEffect = (QEffect self, QEffect effectToCheck) =>
               {
                   //if (effectToCheck.Name == "Precise Strike")
                   if (effectToCheck.Id == AddSwash.PreciseStrikeEffectId)
                   {
                       throw new Exception("Patching Precise Strike");
                       effectToCheck.Description = "You deal more damage when using agile or finesse weapons.";
                       effectToCheck.YouDealDamageWithStrike = delegate (QEffect effectToCheck, CombatAction action, DiceFormula diceFormula, Creature defender)
                       {
                           bool flag = action.HasTrait(Trait.Agile) || action.HasTrait(Trait.Finesse);
                           //bool flag2 = action.Owner.HasEffect(PanacheId);
                           bool flag2 = true;
                           bool flag3 = action.HasTrait(AddSwash.Finisher);
                           bool flag4 = !action.HasTrait(Trait.Ranged) || (action.HasTrait(Trait.Thrown) && (action.Owner.PersistentCharacterSheet?.Calculated.AllFeats.Any(feat => feat.Name == "Flying Blade") ?? false) && (defender.DistanceTo(effectToCheck.Owner) <= action.Item!.WeaponProperties!.RangeIncrement));
                           bool flag5 = defender.IsImmuneTo(Trait.PrecisionDamage);
                           if (flag && flag3 && flag4 && (!flag5))
                           {
                               return diceFormula.Add(!(effectToCheck.Owner.PersistentCharacterSheet.Class.FeatName == classSelectionFeat.FeatName) ? DiceFormula.FromText("1d6", "Precise Strike") : DiceFormula.FromText((((effectToCheck.Owner.Level - 1) / 4) + 2).ToString() + "d6", "Precise Strike"));
                           }
                           else if (flag && flag4 && (!flag5))
                           {
                               return diceFormula.Add(!(effectToCheck.Owner.PersistentCharacterSheet.Class.FeatName == classSelectionFeat.FeatName) ? DiceFormula.FromText("1", "Precise Strike") : DiceFormula.FromText((((effectToCheck.Owner.Level - 1) / 4) + 2).ToString(), "Precise Strike"));
                           }
                           return diceFormula;
                       };
                   }
                   return effectToCheck;
               };
           });
       //*/
        classSelectionFeat.WithOnCreature(delegate (Creature creature)
        {
            creature.AddQEffect(new QEffect("Remastered Precise Strike", "You deal more damage when using agile or finesse weapons, even without panache.")
            {
                YouDealDamageWithStrike = delegate (QEffect effectToCheck, CombatAction action, DiceFormula diceFormula, Creature defender)
                {
                    bool flag = action.HasTrait(Trait.Agile) || action.HasTrait(Trait.Finesse);
                    bool flag2 = !action.Owner.HasEffect(AddSwash.PanacheId);
                    bool flag3 = !action.HasTrait(AddSwash.Finisher); // finisher already is handled by the legacy version
                    bool flag4 = !action.HasTrait(Trait.Ranged) || (action.HasTrait(Trait.Thrown) && (action.Owner.PersistentCharacterSheet?.Calculated.AllFeats.Any(feat => feat.Name == "Flying Blade") ?? false) && (defender.DistanceTo(effectToCheck.Owner) <= action.Item!.WeaponProperties!.RangeIncrement));
                    bool flag5 = defender.IsImmuneTo(Trait.PrecisionDamage);
                    if (flag && flag2 && flag3 && flag4 && (!flag5))
                    {
                        return diceFormula.Add(!(effectToCheck.Owner.PersistentCharacterSheet.Class.FeatName == classSelectionFeat.FeatName) ? DiceFormula.FromText("1", "Precise Strike") : DiceFormula.FromText((((effectToCheck.Owner.Level - 1) / 4) + 2).ToString(), "Precise Strike"));
                    }
                    return diceFormula;
                }
            });
        });
    }
    
    /// <summary>
    /// Updates the Precise Strike feat text to provide its damage bonus even without panache.
    /// </summary>
    /// <param name="feat">The Precise Strike Feat</param>
    private static void PatchPreciseStrikeText(Feat feat)
    {
        // Updates the Rules Text
        feat.RulesText = "When you make a Strike with a melee agile or finesse weapon or an agile or finesse unarmed strike, you deal 2 extra damage. This damage is 2d6 instead if the Strike was part of a finisher.";
    }


    /// <summary>
    /// Patches the Opportune Riposte class feature from the legacy version to give it the bravado trait. It won't benefit from Stylish Combatant, but it will provide panache on a success (indefinitely) and failure (until the end of the next turn).
    /// </summary>
    /// <param name="classSelectionFeat">The Swashbuckler Class Selection Feat</param>
    public static QEffect PatchOpportuneRiposte()
    {
        QEffect remasterOpportuneRiposte = new QEffect("Opportune Riposte (remaster)", "When you use Opportune Riposte, it has the bravado trait.")
        {
            AfterYouTakeAction = async (qf, action) =>
            {
                if (action.HasTrait(AddSwash.OpportuneRiposteTrait))
                {
                    if (!qf.Owner.HasEffect(AddSwash.PanacheId))
                    {
                        if (action.CheckResult == CheckResult.Success || action.CheckResult == CheckResult.CriticalSuccess)
                        {
                            qf.Owner.AddQEffect(CreatePanache(ExpirationCondition.Never));
                        }
                        else if (action.CheckResult == CheckResult.Failure)
                        {
                            qf.Owner.AddQEffect(CreatePanache(ExpirationCondition.ExpiresAtEndOfYourTurn));
                        }
                    }
                }
            }
        };
        return remasterOpportuneRiposte;
    }
    
    /// <summary>
    /// This adds the QEffect to generate panache until the end of your turn on a failed bravado check. This QEffect is bypassed for most actions granted by Swashbuckler feats, but it is used to add the bravado trait to Tumble Through and other existing actions.
    /// Note that the legacy Swashbuckler PanacheGranter() already handles this for successes and critical successes.
    /// </summary>
    /// <returns>QEffect for granting panache on failure</returns>
    public static QEffect PanacheGranterFailure()
    {
        return new QEffect()
        {
            AfterYouTakeActionAgainstTarget = async delegate (QEffect qf, CombatAction action, Creature target, CheckResult result)
            {
                AddSwash.SwashbucklerStyle style = (AddSwash.SwashbucklerStyle)qf.Owner.PersistentCharacterSheet.Calculated.AllFeats.Find(feat => feat.HasTrait(AddSwash.SwashStyle));
                bool dummyflag = ModManager.TryParse("Disarming Flair", out FeatName DisarmingFlair);
                bool flag = (result == CheckResult.Failure);
                bool flag2 = (action.ActionId == ActionId.TumbleThrough || style.PanacheTriggers.Contains(action.ActionId));
                bool flag3 = !qf.Owner.HasEffect(AddSwash.PanacheId);
                bool flag1p5 = (result >= CheckResult.Success);
                bool flag2p5 = (qf.Owner.HasFeat(DisarmingFlair) && action.ActionId == ActionId.Disarm) ;
                
                if (flag && (flag2 || flag2p5) && flag3)
                {
                    qf.Owner.AddQEffect(CreatePanache(ExpirationCondition.CountsDownAtEndOfYourTurn));
                }
                else if (flag1p5 && flag2p5 && flag3) // add Panache for Disarming Flair for all styles, not just gymnasts
                {
                    qf.Owner.AddQEffect(CreatePanache());
                }
            }
        };
    }

    /// <summary>
    /// This modifies the legacy Aid reaction to add the bravado trait (providing panache on results other than critical failure).
    /// </summary>
    /// <param name="feat">The Swashbuckler ClassSelectionFeat</param>
    public static void PatchOneForAllAid(Feat feat)
    {
        bool dummyfeat = ModManager.TryParse("One For All", out FeatName LegacyOFA);
        feat.WithOnCreature(delegate (Creature creature)
        {
            if (creature.HasFeat(LegacyOFA))
            {
                QEffect qf = new QEffect("AidPanache", null, ExpirationCondition.Never, creature);
                qf.AfterYouTakeAction = async delegate (QEffect qfdum, CombatAction aid)
                {
                    if (aid.Name == "Aid")
                    {
                        if (!qf.Owner.HasEffect(AddSwash.PanacheId))
                        {
                            if (aid.CheckResult >= CheckResult.Success)
                            {
                                qf.Owner.AddQEffect(CreatePanache());
                            }
                            else if (aid.CheckResult == CheckResult.Failure)
                            {
                                qf.Owner.AddQEffect(CreatePanache(ExpirationCondition.ExpiresAtEndOfYourTurn));
                            }
                        }
                    }
                };
                creature.AddQEffect(qf);
            }
        });
    }
    
}