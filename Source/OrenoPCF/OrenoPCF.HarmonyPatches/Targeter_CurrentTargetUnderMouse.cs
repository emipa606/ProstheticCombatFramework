using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace OrenoPCF.HarmonyPatches;

[HarmonyPatch(typeof(Targeter), nameof(Targeter.OrderPawnForceTarget))]
internal class Targeter_CurrentTargetUnderMouse
{
    public static bool Prefix(Targeter __instance, ITargetingSource targetingSource)
    {
        var getVerb = targetingSource.GetVerb;
        if (getVerb == null)
        {
            return true;
        }

        if (getVerb.EquipmentSource != null || getVerb.EquipmentCompSource != null)
        {
            return true;
        }

        if (getVerb.HediffSource != null || getVerb.HediffCompSource != null)
        {
            return true;
        }

        if (getVerb.TerrainSource != null || getVerb.TerrainDefSource != null)
        {
            return true;
        }

        if (getVerb is Verb_CastPsycast)
        {
            return true;
        }

        if (getVerb.verbProps.IsMeleeAttack)
        {
            Traverse.Create(__instance).Field("targetParams").SetValue(TargetingParameters.ForAttackAny());
        }

        var localTargetInfo = CurrentTargetUnderMouse(__instance, true);
        if (!localTargetInfo.IsValid)
        {
            return true;
        }

        if (getVerb.CasterPawn == localTargetInfo.Thing)
        {
            return true;
        }

        if (getVerb.verbProps.IsMeleeAttack)
        {
            var job = new Job(JobDefOf.AttackMelee, localTargetInfo)
            {
                verbToUse = getVerb,
                playerForced = true
            };
            if (localTargetInfo.Thing is Pawn pawn)
            {
                job.killIncappedTarget = pawn.Downed;
            }

            getVerb.CasterPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
        else
        {
            var num = getVerb.verbProps.EffectiveMinRange(localTargetInfo, getVerb.CasterPawn);
            if (getVerb.CasterPawn.Position.DistanceToSquared(localTargetInfo.Cell) < num * num &&
                getVerb.CasterPawn.Position.AdjacentTo8WayOrInside(localTargetInfo.Cell))
            {
                Messages.Message("MessageCantShootInMelee".Translate(), getVerb.CasterPawn,
                    MessageTypeDefOf.RejectInput, false);
            }
            else
            {
                var def = !getVerb.verbProps.ai_IsWeapon
                    ? JobDefOf.UseVerbOnThing
                    : PCF_JobDefOf.PCF_AttackStaticExtended;
                var job2 = new Job(def)
                {
                    verbToUse = getVerb,
                    targetA = localTargetInfo,
                    endIfCantShootInMelee = true
                };
                getVerb.CasterPawn.jobs.TryTakeOrderedJob(job2, JobTag.Misc);
            }
        }

        return false;
    }

    private static LocalTargetInfo CurrentTargetUnderMouse(Targeter targeter, bool mustBeHittableNowIfNotMelee)
    {
        if (!targeter.IsTargeting)
        {
            return LocalTargetInfo.Invalid;
        }

        var value = Traverse.Create(targeter).Field("targetParams").GetValue<TargetingParameters>();
        var clickParams = targeter.targetingSource == null
            ? value
            : targeter.targetingSource.GetVerb.verbProps.targetParams;
        var localTargetInfo = LocalTargetInfo.Invalid;
        using (var enumerator = GenUI.TargetsAtMouse(clickParams).GetEnumerator())
        {
            if (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                localTargetInfo = current;
            }
        }

        if (!localTargetInfo.IsValid || !mustBeHittableNowIfNotMelee || localTargetInfo.Thing is Pawn ||
            targeter.targetingSource == null || targeter.targetingSource.GetVerb.verbProps.IsMeleeAttack)
        {
            return localTargetInfo;
        }

        if (targeter.targetingSourceAdditionalPawns != null && targeter.targetingSourceAdditionalPawns.Any())
        {
            var canHitTarget = false;
            foreach (var pawn in targeter.targetingSourceAdditionalPawns)
            {
                var targetingVerb = GetTargetingVerb(targeter, pawn);
                if (targetingVerb == null || !targetingVerb.CanHitTarget(localTargetInfo))
                {
                    continue;
                }

                canHitTarget = true;
                break;
            }

            if (!canHitTarget)
            {
                localTargetInfo = LocalTargetInfo.Invalid;
            }
        }
        else if (!targeter.targetingSource.CanHitTarget(localTargetInfo))
        {
            localTargetInfo = LocalTargetInfo.Invalid;
        }

        return localTargetInfo;
    }

    private static Verb GetTargetingVerb(Targeter targeter, Pawn pawn)
    {
        return pawn.equipment.AllEquipmentVerbs.FirstOrDefault(x =>
            x.verbProps == targeter.targetingSource.GetVerb.verbProps && x is not Verb_CastPsycast);
    }
}