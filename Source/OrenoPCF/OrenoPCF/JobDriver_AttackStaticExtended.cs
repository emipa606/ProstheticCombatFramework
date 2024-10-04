using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace OrenoPCF;

public class JobDriver_AttackStaticExtended : JobDriver
{
    private int numAttacksMade;
    private bool startedIncapacitated;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref startedIncapacitated, "startedIncapacitated");
        Scribe_Values.Look(ref numAttacksMade, "numAttacksMade");
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
        yield return new Toil
        {
            initAction = delegate
            {
                if (TargetThingA is Pawn pawn2)
                {
                    startedIncapacitated = pawn2.Downed;
                }

                pawn.pather.StopDead();
            },
            tickAction = delegate
            {
                if (!TargetA.IsValid)
                {
                    EndJobWith(JobCondition.Succeeded);
                }
                else
                {
                    if (TargetA.HasThing)
                    {
                        var thing = TargetA.Thing as Pawn;
                        if (TargetA.Thing.Destroyed || thing != null && !startedIncapacitated && thing.Downed)
                        {
                            EndJobWith(JobCondition.Succeeded);
                            return;
                        }
                    }

                    if (numAttacksMade >= job.maxNumStaticAttacks && !pawn.stances.FullBodyBusy)
                    {
                        EndJobWith(JobCondition.Succeeded);
                    }
                    else if (TryStartAttack(TargetA))
                    {
                        numAttacksMade++;
                    }
                    else if (!pawn.stances.FullBodyBusy)
                    {
                        var verbToUse = job.verbToUse;
                        if (job.endIfCantShootTargetFromCurPos &&
                            (verbToUse == null || !verbToUse.CanHitTargetFrom(pawn.Position, TargetA)))
                        {
                            EndJobWith(JobCondition.Ongoing | JobCondition.Succeeded);
                        }
                        else if (job.endIfCantShootInMelee)
                        {
                            if (verbToUse == null)
                            {
                                EndJobWith(JobCondition.Ongoing | JobCondition.Succeeded);
                            }
                            else
                            {
                                var num = verbToUse.verbProps.EffectiveMinRange(TargetA, pawn);
                                if (pawn.Position.DistanceToSquared(TargetA.Cell) < num * num &&
                                    pawn.Position.AdjacentTo8WayOrInside(TargetA.Cell))
                                {
                                    EndJobWith(JobCondition.Ongoing | JobCondition.Succeeded);
                                }
                            }
                        }
                    }
                }
            },
            defaultCompleteMode = ToilCompleteMode.Never
        };
    }

    public bool TryStartAttack(LocalTargetInfo targ)
    {
        if (pawn.stances.FullBodyBusy)
        {
            return false;
        }

        if (pawn.story != null && pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent))
        {
            return false;
        }

        return job.verbToUse?.TryStartCastOn(targ) ?? false;
    }
}