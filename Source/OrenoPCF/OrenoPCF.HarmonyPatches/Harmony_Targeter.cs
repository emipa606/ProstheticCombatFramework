using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace OrenoPCF.HarmonyPatches;

public class Harmony_Targeter
{
	[HarmonyPatch(typeof(Targeter))]
	[HarmonyPatch("OrderPawnForceTarget")]
	internal class Targeter_CurrentTargetUnderMouse
	{
		[HarmonyPrefix]
		public static bool VerbGiverExtended(Targeter __instance, ITargetingSource targetingSource)
		{
			Verb getVerb = targetingSource.GetVerb;
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
			LocalTargetInfo localTargetInfo = CurrentTargetUnderMouse(__instance, mustBeHittableNowIfNotMelee: true);
			if (!localTargetInfo.IsValid)
			{
				return true;
			}
			if (getVerb.CasterPawn != localTargetInfo.Thing)
			{
				if (getVerb.verbProps.IsMeleeAttack)
				{
					Job job = new Job(JobDefOf.AttackMelee, localTargetInfo)
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
					float num = getVerb.verbProps.EffectiveMinRange(localTargetInfo, getVerb.CasterPawn);
					if ((float)getVerb.CasterPawn.Position.DistanceToSquared(localTargetInfo.Cell) < num * num && getVerb.CasterPawn.Position.AdjacentTo8WayOrInside(localTargetInfo.Cell))
					{
						Messages.Message("MessageCantShootInMelee".Translate(), getVerb.CasterPawn, MessageTypeDefOf.RejectInput, historical: false);
					}
					else
					{
						JobDef def = ((!getVerb.verbProps.ai_IsWeapon) ? JobDefOf.UseVerbOnThing : PCF_JobDefOf.PCF_AttackStaticExtended);
						Job job2 = new Job(def)
						{
							verbToUse = getVerb,
							targetA = localTargetInfo,
							endIfCantShootInMelee = true
						};
						getVerb.CasterPawn.jobs.TryTakeOrderedJob(job2, JobTag.Misc);
					}
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
			TargetingParameters value = Traverse.Create(targeter).Field("targetParams").GetValue<TargetingParameters>();
			TargetingParameters clickParams = ((targeter.targetingSource == null) ? value : targeter.targetingSource.GetVerb.verbProps.targetParams);
			LocalTargetInfo localTargetInfo = LocalTargetInfo.Invalid;
			using (IEnumerator<LocalTargetInfo> enumerator = GenUI.TargetsAtMouse(clickParams).GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					LocalTargetInfo current = enumerator.Current;
					localTargetInfo = current;
				}
			}
			if (localTargetInfo.IsValid && mustBeHittableNowIfNotMelee && !(localTargetInfo.Thing is Pawn) && targeter.targetingSource != null && !targeter.targetingSource.GetVerb.verbProps.IsMeleeAttack)
			{
				if (targeter.targetingSourceAdditionalPawns != null && targeter.targetingSourceAdditionalPawns.Any())
				{
					bool flag = false;
					for (int i = 0; i < targeter.targetingSourceAdditionalPawns.Count; i++)
					{
						Verb targetingVerb = GetTargetingVerb(targeter, targeter.targetingSourceAdditionalPawns[i]);
						if (targetingVerb != null && targetingVerb.CanHitTarget(localTargetInfo))
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						localTargetInfo = LocalTargetInfo.Invalid;
					}
				}
				else if (!targeter.targetingSource.CanHitTarget(localTargetInfo))
				{
					localTargetInfo = LocalTargetInfo.Invalid;
				}
			}
			return localTargetInfo;
		}

		private static Verb GetTargetingVerb(Targeter targeter, Pawn pawn)
		{
			return pawn.equipment.AllEquipmentVerbs.FirstOrDefault((Verb x) => x.verbProps == targeter.targetingSource.GetVerb.verbProps && !(x is Verb_CastPsycast));
		}
	}
}
