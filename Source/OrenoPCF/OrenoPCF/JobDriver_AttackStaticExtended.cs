using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace OrenoPCF;

public class JobDriver_AttackStaticExtended : JobDriver
{
	private bool startedIncapacitated;

	private int numAttacksMade;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref startedIncapacitated, "startedIncapacitated", defaultValue: false);
		Scribe_Values.Look(ref numAttacksMade, "numAttacksMade", 0);
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
				if (base.TargetThingA is Pawn pawn2)
				{
					startedIncapacitated = pawn2.Downed;
				}
				pawn.pather.StopDead();
			},
			tickAction = delegate
			{
				if (!base.TargetA.IsValid)
				{
					EndJobWith(JobCondition.Succeeded);
				}
				else
				{
					if (base.TargetA.HasThing)
					{
						Pawn pawn = base.TargetA.Thing as Pawn;
						if (base.TargetA.Thing.Destroyed || (pawn != null && !startedIncapacitated && pawn.Downed))
						{
							EndJobWith(JobCondition.Succeeded);
							return;
						}
					}
					if (numAttacksMade >= job.maxNumStaticAttacks && !base.pawn.stances.FullBodyBusy)
					{
						EndJobWith(JobCondition.Succeeded);
					}
					else if (TryStartAttack(base.TargetA))
					{
						numAttacksMade++;
					}
					else if (!base.pawn.stances.FullBodyBusy)
					{
						Verb verbToUse = job.verbToUse;
						if (job.endIfCantShootTargetFromCurPos && (verbToUse == null || !verbToUse.CanHitTargetFrom(base.pawn.Position, base.TargetA)))
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
								float num = verbToUse.verbProps.EffectiveMinRange(base.TargetA, base.pawn);
								if ((float)base.pawn.Position.DistanceToSquared(base.TargetA.Cell) < num * num && base.pawn.Position.AdjacentTo8WayOrInside(base.TargetA.Cell))
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
		bool flag = !pawn.IsColonist;
		return job.verbToUse?.TryStartCastOn(targ, false, true, false) ?? false;
	}
}
