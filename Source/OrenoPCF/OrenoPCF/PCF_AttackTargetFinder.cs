using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace OrenoPCF;

public static class PCF_AttackTargetFinder
{
	private static List<IAttackTarget> tmpTargets = new List<IAttackTarget>();

	private static List<Pair<IAttackTarget, float>> availableShootingTargets = new List<Pair<IAttackTarget, float>>();

	private static List<float> tmpTargetScores = new List<float>();

	private static List<bool> tmpCanShootAtTarget = new List<bool>();

	public static IAttackTarget BestAttackTarget(IAttackTargetSearcher searcher, Verb verb, TargetScanFlags flags, Predicate<Thing> validator = null, float minDist = 0f, float maxDist = 9999f, IntVec3 locus = default(IntVec3), float maxTravelRadiusFromLocus = float.MaxValue, bool canBash = false, bool canTakeTargetsCloserThanEffectiveMinRange = true)
	{
		Thing searcherThing = searcher.Thing;
		Pawn searcherPawn = searcher as Pawn;
		if (verb == null)
		{
			Log.Error("BestAttackTarget with " + searcher.ToStringSafe() + " who has no attack verb.");
			return null;
		}
		bool onlyTargetMachines = verb.IsEMP();
		float minDistSquared = minDist * minDist;
		float num = maxTravelRadiusFromLocus + verb.verbProps.range;
		float maxLocusDistSquared = num * num;
		Func<IntVec3, bool> losValidator = null;
		if ((byte)(flags & TargetScanFlags.LOSBlockableByGas) != 0)
		{
			losValidator = delegate(IntVec3 vec3)
			{
				Gas gas = vec3.GetGas(searcherThing.Map);
				return gas == null || !gas.def.gas.blockTurretTracking;
			};
		}
		Predicate<IAttackTarget> innerValidator = delegate(IAttackTarget t)
		{
			Thing thing = t.Thing;
			if (t == searcher)
			{
				return false;
			}
			if (minDistSquared > 0f && (float)(searcherThing.Position - thing.Position).LengthHorizontalSquared < minDistSquared)
			{
				return false;
			}
			if (!canTakeTargetsCloserThanEffectiveMinRange)
			{
				float num2 = verb.verbProps.EffectiveMinRange(thing, searcherThing);
				if (num2 > 0f && (float)(searcherThing.Position - thing.Position).LengthHorizontalSquared < num2 * num2)
				{
					return false;
				}
			}
			if (maxTravelRadiusFromLocus < 9999f && (float)(thing.Position - locus).LengthHorizontalSquared > maxLocusDistSquared)
			{
				return false;
			}
			if (!searcherThing.HostileTo(thing))
			{
				return false;
			}
			if (validator != null && !validator(thing))
			{
				return false;
			}
			if (searcherPawn != null)
			{
				Lord lord = searcherPawn.GetLord();
				if (lord != null && !lord.LordJob.ValidateAttackTarget(searcherPawn, thing))
				{
					return false;
				}
			}
			if ((byte)(flags & TargetScanFlags.NeedLOSToAll) != 0 && !searcherThing.CanSee(thing, losValidator))
			{
				if (t is Pawn)
				{
					if ((byte)(flags & TargetScanFlags.NeedLOSToPawns) != 0)
					{
						return false;
					}
				}
				else if ((byte)(flags & TargetScanFlags.NeedLOSToNonPawns) != 0)
				{
					return false;
				}
			}
			if ((byte)(flags & TargetScanFlags.NeedThreat) != 0 && t.ThreatDisabled(searcher))
			{
				return false;
			}
			if (onlyTargetMachines && t is Pawn pawn2 && pawn2.RaceProps.IsFlesh)
			{
				return false;
			}
			if ((byte)(flags & TargetScanFlags.NeedNonBurning) != 0 && thing.IsBurning())
			{
				return false;
			}
			if (searcherThing.def.race != null && (int)searcherThing.def.race.intelligence >= 2)
			{
				CompExplosive compExplosive = thing.TryGetComp<CompExplosive>();
				if (compExplosive != null && compExplosive.wickStarted)
				{
					return false;
				}
			}
			if (thing.def.size.x == 1 && thing.def.size.z == 1)
			{
				if (thing.Position.Fogged(thing.Map))
				{
					return false;
				}
			}
			else
			{
				bool flag2 = false;
				foreach (IntVec3 item in thing.OccupiedRect())
				{
					if (item.Fogged(thing.Map))
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					return false;
				}
			}
			return true;
		};
		if (HasRangedAttack(searcher, verb))
		{
			tmpTargets.Clear();
			tmpTargets.AddRange(searcherThing.Map.attackTargetsCache.GetPotentialTargetsFor(searcher));
			if ((byte)(flags & TargetScanFlags.NeedReachable) != 0)
			{
				Predicate<IAttackTarget> oldValidator2 = innerValidator;
				innerValidator = (IAttackTarget t) => oldValidator2(t) && CanReach(searcherThing, t.Thing, canBash);
			}
			bool flag = false;
			for (int i = 0; i < tmpTargets.Count; i++)
			{
				IAttackTarget attackTarget = tmpTargets[i];
				if (attackTarget.Thing.Position.InHorDistOf(searcherThing.Position, maxDist) && innerValidator(attackTarget) && CanShootAtFromCurrentPosition(attackTarget, searcher, verb))
				{
					flag = true;
					break;
				}
			}
			IAttackTarget result;
			if (flag)
			{
				tmpTargets.RemoveAll((IAttackTarget x) => !x.Thing.Position.InHorDistOf(searcherThing.Position, maxDist) || !innerValidator(x));
				result = GetRandomShootingTargetByScore(tmpTargets, searcher, verb);
			}
			else
			{
				result = (IAttackTarget)GenClosest.ClosestThing_Global(validator: ((byte)(flags & TargetScanFlags.NeedReachableIfCantHitFromMyPos) == 0 || (byte)(flags & TargetScanFlags.NeedReachable) != 0) ? ((Predicate<Thing>)((Thing t) => innerValidator((IAttackTarget)t))) : ((Predicate<Thing>)((Thing t) => innerValidator((IAttackTarget)t) && (CanReach(searcherThing, t, canBash) || CanShootAtFromCurrentPosition((IAttackTarget)t, searcher, verb)))), center: searcherThing.Position, searchSet: tmpTargets, maxDistance: maxDist);
			}
			tmpTargets.Clear();
			return result;
		}
		if (searcherPawn != null && searcherPawn.mindState.duty != null && searcherPawn.mindState.duty.radius > 0f && !searcherPawn.InMentalState)
		{
			Predicate<IAttackTarget> oldValidator = innerValidator;
			innerValidator = (IAttackTarget t) => oldValidator(t) && t.Thing.Position.InHorDistOf(searcherPawn.mindState.duty.focus.Cell, searcherPawn.mindState.duty.radius);
		}
		IntVec3 position = searcherThing.Position;
		Map map = searcherThing.Map;
		ThingRequest thingReq = ThingRequest.ForGroup(ThingRequestGroup.Filth);
		PathEndMode peMode = PathEndMode.Touch;
		Pawn pawn = searcherPawn;
		Danger maxDanger = Danger.Deadly;
		bool canBashDoors = canBash;
		TraverseParms traverseParams = TraverseParms.For(pawn, maxDanger, TraverseMode.ByPawn, canBashDoors);
		float maxDistance = maxDist;
		int searchRegionsMax = ((maxDist <= 800f) ? 40 : (-1));
		IAttackTarget attackTarget2 = (IAttackTarget)GenClosest.ClosestThingReachable(position, map, thingReq, peMode, traverseParams, maxDistance, validator3, null, 0, searchRegionsMax);
		if (attackTarget2 != null && PawnUtility.ShouldCollideWithPawns(searcherPawn))
		{
			IAttackTarget attackTarget3 = FindBestReachableMeleeTarget(innerValidator, searcherPawn, maxDist, canBash);
			if (attackTarget3 != null)
			{
				float lengthHorizontal = (searcherPawn.Position - attackTarget2.Thing.Position).LengthHorizontal;
				float lengthHorizontal2 = (searcherPawn.Position - attackTarget3.Thing.Position).LengthHorizontal;
				if (Mathf.Abs(lengthHorizontal - lengthHorizontal2) < 50f)
				{
					attackTarget2 = attackTarget3;
				}
			}
		}
		return attackTarget2;
		bool validator3(Thing x)
		{
			return innerValidator((IAttackTarget)x);
		}
	}

	private static bool CanReach(Thing searcher, Thing target, bool canBash)
	{
		if (searcher is Pawn pawn)
		{
			if (!pawn.CanReach(target, PathEndMode.Touch, Danger.Some, canBash, canBash))
			{
				return false;
			}
		}
		else
		{
			TraverseMode mode = (canBash ? TraverseMode.PassDoors : TraverseMode.NoPassClosedDoors);
			if (!searcher.Map.reachability.CanReach(searcher.Position, target, PathEndMode.Touch, TraverseParms.For(mode)))
			{
				return false;
			}
		}
		return true;
	}

	private static IAttackTarget FindBestReachableMeleeTarget(Predicate<IAttackTarget> validator, Pawn searcherPawn, float maxTargDist, bool canBash)
	{
		maxTargDist = Mathf.Min(maxTargDist, 30f);
		IAttackTarget reachableTarget = null;
		searcherPawn.Map.floodFiller.FloodFill(searcherPawn.Position, delegate(IntVec3 x)
		{
			if (!x.Walkable(searcherPawn.Map))
			{
				return false;
			}
			if ((float)x.DistanceToSquared(searcherPawn.Position) > maxTargDist * maxTargDist)
			{
				return false;
			}
			return (canBash || !(x.GetEdifice(searcherPawn.Map) is Building_Door building_Door) || building_Door.CanPhysicallyPass(searcherPawn)) && !PawnUtility.AnyPawnBlockingPathAt(x, searcherPawn, actAsIfHadCollideWithPawnsJob: true);
		}, delegate(IntVec3 x)
		{
			for (int j = 0; j < 8; j++)
			{
				IntVec3 intVec = x + GenAdj.AdjacentCells[j];
				if (intVec.InBounds(searcherPawn.Map))
				{
					IAttackTarget attackTarget2 = bestTargetOnCell(intVec);
					if (attackTarget2 != null)
					{
						reachableTarget = attackTarget2;
						break;
					}
				}
			}
			return reachableTarget != null;
		});
		return reachableTarget;
		IAttackTarget bestTargetOnCell(IntVec3 x)
		{
			List<Thing> thingList = x.GetThingList(searcherPawn.Map);
			for (int i = 0; i < thingList.Count; i++)
			{
				Thing thing = thingList[i];
				if (thing is IAttackTarget attackTarget && validator(attackTarget) && ReachabilityImmediate.CanReachImmediate(x, thing, searcherPawn.Map, PathEndMode.Touch, searcherPawn) && (searcherPawn.CanReachImmediate(thing, PathEndMode.Touch) || searcherPawn.Map.attackTargetReservationManager.CanReserve(searcherPawn, attackTarget)))
				{
					return attackTarget;
				}
			}
			return null;
		}
	}

	private static bool HasRangedAttack(IAttackTargetSearcher t, Verb verb)
	{
		return verb != null && !verb.verbProps.IsMeleeAttack;
	}

	private static bool CanShootAtFromCurrentPosition(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
	{
		return verb?.CanHitTargetFrom(searcher.Thing.Position, target.Thing) ?? false;
	}

	private static IAttackTarget GetRandomShootingTargetByScore(List<IAttackTarget> targets, IAttackTargetSearcher searcher, Verb verb)
	{
		if (GetAvailableShootingTargetsByScore(targets, searcher, verb).TryRandomElementByWeight((Pair<IAttackTarget, float> x) => x.Second, out var result))
		{
			return result.First;
		}
		return null;
	}

	private static List<Pair<IAttackTarget, float>> GetAvailableShootingTargetsByScore(List<IAttackTarget> rawTargets, IAttackTargetSearcher searcher, Verb verb)
	{
		availableShootingTargets.Clear();
		if (rawTargets.Count == 0)
		{
			return availableShootingTargets;
		}
		tmpTargetScores.Clear();
		tmpCanShootAtTarget.Clear();
		float num = 0f;
		IAttackTarget attackTarget = null;
		for (int i = 0; i < rawTargets.Count; i++)
		{
			tmpTargetScores.Add(float.MinValue);
			tmpCanShootAtTarget.Add(item: false);
			if (rawTargets[i] == searcher)
			{
				continue;
			}
			bool flag = CanShootAtFromCurrentPosition(rawTargets[i], searcher, verb);
			tmpCanShootAtTarget[i] = flag;
			if (flag)
			{
				float shootingTargetScore = GetShootingTargetScore(rawTargets[i], searcher, verb);
				tmpTargetScores[i] = shootingTargetScore;
				if (attackTarget == null || shootingTargetScore > num)
				{
					attackTarget = rawTargets[i];
					num = shootingTargetScore;
				}
			}
		}
		if (num < 1f)
		{
			if (attackTarget != null)
			{
				availableShootingTargets.Add(new Pair<IAttackTarget, float>(attackTarget, 1f));
			}
		}
		else
		{
			float num2 = num - 30f;
			for (int j = 0; j < rawTargets.Count; j++)
			{
				if (rawTargets[j] != searcher && tmpCanShootAtTarget[j])
				{
					float num3 = tmpTargetScores[j];
					if (num3 >= num2)
					{
						float second = Mathf.InverseLerp(num - 30f, num, num3);
						availableShootingTargets.Add(new Pair<IAttackTarget, float>(rawTargets[j], second));
					}
				}
			}
		}
		return availableShootingTargets;
	}

	private static float GetShootingTargetScore(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
	{
		float num = 60f;
		num -= Mathf.Min((target.Thing.Position - searcher.Thing.Position).LengthHorizontal, 40f);
		if (target.TargetCurrentlyAimingAt == searcher.Thing)
		{
			num += 10f;
		}
		if (searcher.LastAttackedTarget == target.Thing && Find.TickManager.TicksGame - searcher.LastAttackTargetTick <= 300)
		{
			num += 40f;
		}
		num -= CoverUtility.CalculateOverallBlockChance(target.Thing.Position, searcher.Thing.Position, searcher.Thing.Map) * 10f;
		if (target is Pawn pawn && pawn.RaceProps.Animal && pawn.Faction != null && !pawn.IsFighting())
		{
			num -= 50f;
		}
		num += FriendlyFireBlastRadiusTargetScoreOffset(target, searcher, verb);
		return num + FriendlyFireConeTargetScoreOffset(target, searcher, verb);
	}

	private static float FriendlyFireBlastRadiusTargetScoreOffset(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
	{
		if (verb.verbProps.ai_AvoidFriendlyFireRadius <= 0f)
		{
			return 0f;
		}
		Map map = target.Thing.Map;
		IntVec3 position = target.Thing.Position;
		int num = GenRadial.NumCellsInRadius(verb.verbProps.ai_AvoidFriendlyFireRadius);
		float num2 = 0f;
		for (int i = 0; i < num; i++)
		{
			IntVec3 intVec = position + GenRadial.RadialPattern[i];
			if (!intVec.InBounds(map))
			{
				continue;
			}
			bool flag = true;
			List<Thing> thingList = intVec.GetThingList(map);
			for (int j = 0; j < thingList.Count; j++)
			{
				if (!(thingList[j] is IAttackTarget) || thingList[j] == target)
				{
					continue;
				}
				if (flag)
				{
					if (!GenSight.LineOfSight(position, intVec, map, skipFirstCell: true))
					{
						break;
					}
					flag = false;
				}
				float num3 = ((thingList[j] == searcher) ? 40f : ((!(thingList[j] is Pawn)) ? 10f : ((!thingList[j].def.race.Animal) ? 18f : 7f)));
				num2 = ((!searcher.Thing.HostileTo(thingList[j])) ? (num2 - num3) : (num2 + num3 * 0.6f));
			}
		}
		return num2;
	}

	private static float FriendlyFireConeTargetScoreOffset(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
	{
		if (!(searcher.Thing is Pawn pawn))
		{
			return 0f;
		}
		if ((int)pawn.RaceProps.intelligence < 1)
		{
			return 0f;
		}
		if (pawn.RaceProps.IsMechanoid)
		{
			return 0f;
		}
		if (!(verb is Verb_Shoot verb_Shoot))
		{
			return 0f;
		}
		ThingDef defaultProjectile = verb_Shoot.verbProps.defaultProjectile;
		if (defaultProjectile == null)
		{
			return 0f;
		}
		if (defaultProjectile.projectile.flyOverhead)
		{
			return 0f;
		}
		Map map = pawn.Map;
		ShotReport report = ShotReport.HitReportFor(pawn, verb, (Thing)target);
		float a = VerbUtility.CalculateAdjustedForcedMiss(verb.verbProps.ForcedMissRadius, report.ShootLine.Dest - report.ShootLine.Source);
		float radius = Mathf.Max(a, 1.5f);
		IntVec3 dest2 = report.ShootLine.Dest;
		IEnumerable<IntVec3> source = from dest in GenRadial.RadialCellsAround(dest2, radius, useCenter: true)
			where dest.InBounds(map)
			select dest;
		IEnumerable<ShootLine> source2 = source.Select((IntVec3 dest) => new ShootLine(report.ShootLine.Source, dest));
		IEnumerable<IntVec3> source3 = source2.SelectMany((ShootLine line) => line.Points().Concat(line.Dest).TakeWhile((IntVec3 pos) => pos.CanBeSeenOverFast(map)));
		IEnumerable<IntVec3> enumerable = source3.Distinct();
		float num = 0f;
		foreach (IntVec3 item in enumerable)
		{
			float num2 = VerbUtility.InterceptChanceFactorFromDistance(report.ShootLine.Source.ToVector3Shifted(), item);
			if (!(num2 > 0f))
			{
				continue;
			}
			List<Thing> thingList = item.GetThingList(map);
			for (int i = 0; i < thingList.Count; i++)
			{
				Thing thing = thingList[i];
				if (thing is IAttackTarget && thing != target)
				{
					float num3 = ((thing == searcher) ? 40f : ((!(thing is Pawn)) ? 10f : ((!thing.def.race.Animal) ? 18f : 7f)));
					num3 *= num2;
					num3 = ((!searcher.Thing.HostileTo(thing)) ? (num3 * -1f) : (num3 * 0.6f));
					num += num3;
				}
			}
		}
		return num;
	}

	public static IAttackTarget BestShootTargetFromCurrentPosition(IAttackTargetSearcher searcher, Verb verb, TargetScanFlags flags, Predicate<Thing> validator = null, float minDistance = 0f, float maxDistance = 9999f)
	{
		if (verb == null)
		{
			Log.Error("BestShootTargetFromCurrentPosition with " + searcher.ToStringSafe() + " who has no attack verb.");
			return null;
		}
		return BestAttackTarget(searcher, verb, flags, validator, Mathf.Max(minDistance, verb.verbProps.minRange), Mathf.Min(maxDistance, verb.verbProps.range), default(IntVec3), float.MaxValue, canBash: false, canTakeTargetsCloserThanEffectiveMinRange: false);
	}
}
