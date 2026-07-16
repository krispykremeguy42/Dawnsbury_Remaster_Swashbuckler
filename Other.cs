using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Dawnsbury.Mods.RemasteredSwashbuckler.RegisteredComponents;
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
using Dawnsbury.Core.Intelligence;

namespace Dawnsbury.Mods.RemasteredSwashbuckler;

/// <summary>
/// Content needed by but not exclusive to the remastered Swashbuckler
/// </summary>
public class Other
{
    /// <summary>
    /// The QEffect ID of the persistent effect that makes creatures capable of performing the Dirty Trick action
    /// </summary>
    public static readonly QEffectId DirtyTrickCapabilityQEID = ModManager.RegisterEnumMember<QEffectId>("Dirty Trick Capability QEID");

    /// <summary>
    /// The QEffect ID of the persistent effect for creatures affected by a use of Dirty Trick  
    /// </summary>
    public static readonly QEffectId DirtyTrickEffectQEID = ModManager.RegisterEnumMember<QEffectId>("Dirty Trick Effect QEID");

    /// <summary>
    /// The Dirty Trick Action ID
    /// </summary>
    public static readonly ActionId DirtyTrickActionId = ModManager.RegisterEnumMember<ActionId>("Dirty Trick");

    private static string DirtyTrickFlavourText = "You hook a foe's bootlaces together, pull their hat over their eyes, loosen their belt, or otherwise confound their mobility through an underhanded tactic.";
    private static string DirtyTrickRulesText = "{b}Requirements{/b} You have a hand free and are within melee reach of an opponent.\n\n" +
            "Attempt a Thievery check against the target's Reflex DC." + 
            S.FourDegreesOfSuccess(
                "The target is clumsy 1 until they use an Interact action to end the impediment.",
                "As critical success, but the condition ends automatically after 1 round.",
                null,
                "You fall prone as your attempt backfires."
            );

    public static CombatAction CreateDirtyTrickCombatAction(Creature creature, Item item)
    {
        return new CombatAction(
            creature,
            IllustrationName.Clumsy,
            "Dirty Trick",
            new Trait[] { Trait.Attack, Trait.Manipulate },
            DirtyTrickRulesText,
            Target.AdjacentCreature().WithAdditionalConditionOnTargetCreature(delegate (Creature user, Creature target)
            {
                if (!user.HasFreeHand)
                {
                    return Usability.CommonReasons.NoFreeHandForManeuver;
                }
                if (!target.EnemyOf(user))
                {
                    return Usability.NotUsableOnThisCreature("Dirty Trick must target an opponent.");
                }
                if (target.DistanceTo(user) > user.UnarmedStrike.DetermineReach(user))
                {
                    return Usability.CommonReasons.TargetOutOfReach;
                }
                return Usability.Usable;
            })
            .WithAdditionalConditionOnTargetCreature(new CombatManeuverCorporealityCreatureTargetingRequirement())
        )
        .WithActionCost(1)
        .WithActionId(DirtyTrickActionId)
        .WithShortDescription("Attempt to play a dirty trick on a target, making them clumsy.")
        .WithActiveRollSpecification(new ActiveRollSpecification(TaggedChecks.SkillCheck(Skill.Thievery), Checks.DefenseDC(Defense.Reflex)))
        .WithEffectOnEachTarget(async (action, user, target, result) =>
        {
            var dirtyTrickEffect = QEffect.Clumsy(1);
            dirtyTrickEffect.Id = DirtyTrickEffectQEID;
            dirtyTrickEffect.ExpiresAt = ExpirationCondition.ExpiresAtStartOfSourcesTurn;

            if (result == CheckResult.CriticalSuccess)
            {
                dirtyTrickEffect.ExpiresAt = ExpirationCondition.Never;
                dirtyTrickEffect.ProvideContextualAction = delegate (QEffect qe)
                {
                    return new ActionPossibility(CreateEndDirtyTrickEffectCombatAction(target, dirtyTrickEffect));
                };
            }

            switch(result)
            {
                case CheckResult.Failure:
                    break;
                case CheckResult.CriticalFailure:
                    await user.FallProne();
                    break;
                default:
                    target.AddQEffect(dirtyTrickEffect);
                    break;
            }
        })
        .WithItem(item);
    }

    public static CombatAction CreateEndDirtyTrickEffectCombatAction(Creature creature, QEffect trickEffect)
    {
        return new CombatAction(
            creature,
            IllustrationName.Clumsy,
            "End Dirty Trick",
            new Trait[] { Trait.Manipulate },
            "Fix a dirty trick that was played on you, which made you clumsy.",
            Target.Self(delegate (Creature creature, AI ai)
            {
                return ai.AlwaysIfSmartAndTakingCareOfSelf;
            })
        )
        .WithActionCost(1)
        .WithEffectOnSelf(delegate (Creature self)
        {
            self.RemoveAllQEffects(delegate (QEffect qe)
            {
               return qe == trickEffect; 
            });
        });
    }

    /// <summary>
    /// Create the Feat that provides the Dirty Trick action in combat. Note that this assumes a free hand is being used
    /// to perform dirty tricks; if items with a dirty trick trait become popular then this will need to be revisited
    /// </summary>
    /// <returns></returns>
    public static Feat CreateDirtyTrickFeat()
    {
        return new TrueFeat(
            OtherFeatNames.DirtyTrick, 
            1, 
            DirtyTrickFlavourText,
            DirtyTrickRulesText,
            new Trait[] { Trait.Attack, Trait.General, Trait.Manipulate, Trait.Skill }
        )
        .WithPrerequisite(values => values.GetProficiency(Trait.Thievery) >= Proficiency.Trained, "You must be trained in Thievery.")
        .WithPermanentQEffect(null, delegate (QEffect qf)
        {
            qf.ProvideActionIntoPossibilitySection = (effect, section) =>
            {
                if (section.PossibilitySectionId == PossibilitySectionId.AttackManeuvers)
                {
                    return new ActionPossibility(CreateDirtyTrickCombatAction(effect.Owner, effect.Owner.UnarmedStrike));
                }
                return null;
            };
        });
    }
}