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
    private static readonly List<IAttackTarget> tmpTargets = [];

    private static readonly List<Pair<IAttackTarget, float>> availableShootingTargets =
        [];

    private static readonly List<float> tmpTargetScores = [];

    private static readonly List<bool> tmpCanShootAtTarget = [];

    public static IAttackTarget BestAttackTarget(IAttackTargetSearcher searcher, Verb verb, TargetScanFlags flags,
        Predicate<Thing> validator = null, float minDist = 0f, float maxDist = 9999f, IntVec3 locus = default,
        float maxTravelRadiusFromLocus = float.MaxValue, bool canBash = false,
        bool canTakeTargetsCloserThanEffectiveMinRange = true)
    {
        var searcherThing = searcher.Thing;
        var searcherPawn = searcher as Pawn;
        if (verb == null)
        {
            Log.Error($"BestAttackTarget with {searcher.ToStringSafe()} who has no attack verb.");
            return null;
        }

        var onlyTargetMachines = verb.IsEMP();
        var minDistSquared = minDist * minDist;
        var num = maxTravelRadiusFromLocus + verb.verbProps.range;
        var maxLocusDistSquared = num * num;
        Predicate<IAttackTarget> innerValidator = delegate(IAttackTarget t)
        {
            var thing = t.Thing;
            if (t == searcher)
            {
                return false;
            }

            if (minDistSquared > 0f &&
                (searcherThing.Position - thing.Position).LengthHorizontalSquared < minDistSquared)
            {
                return false;
            }

            if (!canTakeTargetsCloserThanEffectiveMinRange)
            {
                var num2 = verb.verbProps.EffectiveMinRange(thing, searcherThing);
                if (num2 > 0f && (searcherThing.Position - thing.Position).LengthHorizontalSquared < num2 * num2)
                {
                    return false;
                }
            }

            if (maxTravelRadiusFromLocus < 9999f &&
                (thing.Position - locus).LengthHorizontalSquared > maxLocusDistSquared)
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

            var lord = searcherPawn?.GetLord();
            if (lord != null && !lord.LordJob.ValidateAttackTarget(searcherPawn, thing))
            {
                return false;
            }

            if ((byte)(flags & TargetScanFlags.NeedLOSToAll) != 0 && !searcherThing.CanSee(thing))
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
                var compExplosive = thing.TryGetComp<CompExplosive>();
                if (compExplosive is { wickStarted: true })
                {
                    return false;
                }
            }

            if (thing.def.size is { x: 1, z: 1 })
            {
                if (thing.Position.Fogged(thing.Map))
                {
                    return false;
                }
            }
            else
            {
                var fogged = false;
                foreach (var item in thing.OccupiedRect())
                {
                    if (!item.Fogged(thing.Map))
                    {
                        continue;
                    }

                    fogged = true;
                    break;
                }

                if (!fogged)
                {
                    return false;
                }
            }

            return true;
        };
        if (HasRangedAttack(verb))
        {
            tmpTargets.Clear();
            tmpTargets.AddRange(searcherThing.Map.attackTargetsCache.GetPotentialTargetsFor(searcher));
            if ((byte)(flags & TargetScanFlags.NeedReachable) != 0)
            {
                var oldValidator2 = innerValidator;
                innerValidator = t => oldValidator2(t) && CanReach(searcherThing, t.Thing, canBash);
            }

            var canAttack = false;
            foreach (var attackTarget in tmpTargets)
            {
                if (!attackTarget.Thing.Position.InHorDistOf(searcherThing.Position, maxDist) ||
                    !innerValidator(attackTarget) || !CanShootAtFromCurrentPosition(attackTarget, searcher, verb))
                {
                    continue;
                }

                canAttack = true;
                break;
            }

            IAttackTarget result;
            if (canAttack)
            {
                tmpTargets.RemoveAll(x =>
                    !x.Thing.Position.InHorDistOf(searcherThing.Position, maxDist) || !innerValidator(x));
                result = GetRandomShootingTargetByScore(tmpTargets, searcher, verb);
            }
            else
            {
                result = (IAttackTarget)GenClosest.ClosestThing_Global(
                    validator: (byte)(flags & TargetScanFlags.NeedReachableIfCantHitFromMyPos) == 0 ||
                               (byte)(flags & TargetScanFlags.NeedReachable) != 0
                        ? t => innerValidator((IAttackTarget)t)
                        : t => innerValidator((IAttackTarget)t) && (CanReach(searcherThing, t, canBash) ||
                                                                    CanShootAtFromCurrentPosition((IAttackTarget)t,
                                                                        searcher, verb)),
                    center: searcherThing.Position, searchSet: tmpTargets, maxDistance: maxDist);
            }

            tmpTargets.Clear();
            return result;
        }

        if (searcherPawn != null && searcherPawn.mindState.duty is { radius: > 0f } &&
            !searcherPawn.InMentalState)
        {
            var oldValidator = innerValidator;
            innerValidator = t =>
                oldValidator(t) && t.Thing.Position.InHorDistOf(searcherPawn.mindState.duty.focus.Cell,
                    searcherPawn.mindState.duty.radius);
        }

        var position = searcherThing.Position;
        var map = searcherThing.Map;
        var thingReq = ThingRequest.ForGroup(ThingRequestGroup.Filth);
        var peMode = PathEndMode.Touch;
        var maxDanger = Danger.Deadly;
        var traverseParams = TraverseParms.For(searcherPawn, maxDanger, TraverseMode.ByPawn, canBash);
        var searchRegionsMax = maxDist <= 800f ? 40 : -1;
        var attackTarget2 = (IAttackTarget)GenClosest.ClosestThingReachable(position, map, thingReq, peMode,
            traverseParams, maxDist, validator3, null, 0, searchRegionsMax);
        if (attackTarget2 == null || !PawnUtility.ShouldCollideWithPawns(searcherPawn))
        {
            return attackTarget2;
        }

        var attackTarget3 = FindBestReachableMeleeTarget(innerValidator, searcherPawn, maxDist, canBash);
        if (attackTarget3 == null)
        {
            return attackTarget2;
        }

        if (searcherPawn == null)
        {
            return attackTarget2;
        }

        var lengthHorizontal = (searcherPawn.Position - attackTarget2.Thing.Position).LengthHorizontal;
        var lengthHorizontal2 = (searcherPawn.Position - attackTarget3.Thing.Position).LengthHorizontal;
        if (Mathf.Abs(lengthHorizontal - lengthHorizontal2) < 50f)
        {
            attackTarget2 = attackTarget3;
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
            var mode = canBash ? TraverseMode.PassDoors : TraverseMode.NoPassClosedDoors;
            if (!searcher.Map.reachability.CanReach(searcher.Position, target, PathEndMode.Touch,
                    TraverseParms.For(mode)))
            {
                return false;
            }
        }

        return true;
    }

    private static IAttackTarget FindBestReachableMeleeTarget(Predicate<IAttackTarget> validator, Pawn searcherPawn,
        float maxTargDist, bool canBash)
    {
        maxTargDist = Mathf.Min(maxTargDist, 30f);
        IAttackTarget reachableTarget = null;
        searcherPawn.Map.floodFiller.FloodFill(searcherPawn.Position, delegate(IntVec3 x)
        {
            if (!x.Walkable(searcherPawn.Map))
            {
                return false;
            }

            if (x.DistanceToSquared(searcherPawn.Position) > maxTargDist * maxTargDist)
            {
                return false;
            }

            return (canBash || x.GetEdifice(searcherPawn.Map) is not Building_Door building_Door ||
                    building_Door.CanPhysicallyPass(searcherPawn)) &&
                   !PawnUtility.AnyPawnBlockingPathAt(x, searcherPawn, true);
        }, delegate(IntVec3 x)
        {
            for (var j = 0; j < 8; j++)
            {
                var intVec = x + GenAdj.AdjacentCells[j];
                if (!intVec.InBounds(searcherPawn.Map))
                {
                    continue;
                }

                var attackTarget2 = bestTargetOnCell(intVec);
                if (attackTarget2 == null)
                {
                    continue;
                }

                reachableTarget = attackTarget2;
                break;
            }

            return reachableTarget != null;
        });
        return reachableTarget;

        IAttackTarget bestTargetOnCell(IntVec3 x)
        {
            var thingList = x.GetThingList(searcherPawn.Map);
            foreach (var thing in thingList)
            {
                if (thing is IAttackTarget attackTarget && validator(attackTarget) &&
                    ReachabilityImmediate.CanReachImmediate(x, thing, searcherPawn.Map, PathEndMode.Touch,
                        searcherPawn) && (searcherPawn.CanReachImmediate(thing, PathEndMode.Touch) ||
                                          searcherPawn.Map.attackTargetReservationManager.CanReserve(searcherPawn,
                                              attackTarget)))
                {
                    return attackTarget;
                }
            }

            return null;
        }
    }

    private static bool HasRangedAttack(Verb verb)
    {
        return verb != null && !verb.verbProps.IsMeleeAttack;
    }

    private static bool CanShootAtFromCurrentPosition(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
    {
        return verb?.CanHitTargetFrom(searcher.Thing.Position, target.Thing) ?? false;
    }

    private static IAttackTarget GetRandomShootingTargetByScore(List<IAttackTarget> targets,
        IAttackTargetSearcher searcher, Verb verb)
    {
        return GetAvailableShootingTargetsByScore(targets, searcher, verb)
            .TryRandomElementByWeight(x => x.Second, out var result)
            ? result.First
            : null;
    }

    private static List<Pair<IAttackTarget, float>> GetAvailableShootingTargetsByScore(List<IAttackTarget> rawTargets,
        IAttackTargetSearcher searcher, Verb verb)
    {
        availableShootingTargets.Clear();
        if (rawTargets.Count == 0)
        {
            return availableShootingTargets;
        }

        tmpTargetScores.Clear();
        tmpCanShootAtTarget.Clear();
        var num = 0f;
        IAttackTarget attackTarget = null;
        for (var i = 0; i < rawTargets.Count; i++)
        {
            tmpTargetScores.Add(float.MinValue);
            tmpCanShootAtTarget.Add(false);
            if (rawTargets[i] == searcher)
            {
                continue;
            }

            var canShoot = CanShootAtFromCurrentPosition(rawTargets[i], searcher, verb);
            tmpCanShootAtTarget[i] = canShoot;
            if (!canShoot)
            {
                continue;
            }

            var shootingTargetScore = GetShootingTargetScore(rawTargets[i], searcher, verb);
            tmpTargetScores[i] = shootingTargetScore;
            if (attackTarget != null && !(shootingTargetScore > num))
            {
                continue;
            }

            attackTarget = rawTargets[i];
            num = shootingTargetScore;
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
            var num2 = num - 30f;
            for (var j = 0; j < rawTargets.Count; j++)
            {
                if (rawTargets[j] == searcher || !tmpCanShootAtTarget[j])
                {
                    continue;
                }

                var num3 = tmpTargetScores[j];
                if (!(num3 >= num2))
                {
                    continue;
                }

                var second = Mathf.InverseLerp(num - 30f, num, num3);
                availableShootingTargets.Add(new Pair<IAttackTarget, float>(rawTargets[j], second));
            }
        }

        return availableShootingTargets;
    }

    private static float GetShootingTargetScore(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
    {
        var num = 60f;
        num -= Mathf.Min((target.Thing.Position - searcher.Thing.Position).LengthHorizontal, 40f);
        if (target.TargetCurrentlyAimingAt == searcher.Thing)
        {
            num += 10f;
        }

        if (searcher.LastAttackedTarget == target.Thing &&
            Find.TickManager.TicksGame - searcher.LastAttackTargetTick <= 300)
        {
            num += 40f;
        }

        num -= CoverUtility.CalculateOverallBlockChance(target.Thing.Position, searcher.Thing.Position,
            searcher.Thing.Map) * 10f;
        if (target is Pawn pawn && pawn.RaceProps.Animal && pawn.Faction != null && !pawn.IsFighting())
        {
            num -= 50f;
        }

        num += FriendlyFireBlastRadiusTargetScoreOffset(target, searcher, verb);
        return num + FriendlyFireConeTargetScoreOffset(target, searcher, verb);
    }

    private static float FriendlyFireBlastRadiusTargetScoreOffset(IAttackTarget target, IAttackTargetSearcher searcher,
        Verb verb)
    {
        if (verb.verbProps.ai_AvoidFriendlyFireRadius <= 0f)
        {
            return 0f;
        }

        var map = target.Thing.Map;
        var position = target.Thing.Position;
        var num = GenRadial.NumCellsInRadius(verb.verbProps.ai_AvoidFriendlyFireRadius);
        var num2 = 0f;
        for (var i = 0; i < num; i++)
        {
            var intVec = position + GenRadial.RadialPattern[i];
            if (!intVec.InBounds(map))
            {
                continue;
            }

            var checkLineOfSight = true;
            var thingList = intVec.GetThingList(map);
            foreach (var thing in thingList)
            {
                if (thing is not IAttackTarget || thing == target)
                {
                    continue;
                }

                if (checkLineOfSight)
                {
                    if (!GenSight.LineOfSight(position, intVec, map, true))
                    {
                        break;
                    }

                    checkLineOfSight = false;
                }

                var num3 = thing == searcher ? 40f :
                    thing is not Pawn ? 10f :
                    !thing.def.race.Animal ? 18f : 7f;
                num2 = !searcher.Thing.HostileTo(thing) ? num2 - num3 : num2 + (num3 * 0.6f);
            }
        }

        return num2;
    }

    private static float FriendlyFireConeTargetScoreOffset(IAttackTarget target, IAttackTargetSearcher searcher,
        Verb verb)
    {
        if (searcher.Thing is not Pawn pawn)
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

        if (verb is not Verb_Shoot verb_Shoot)
        {
            return 0f;
        }

        var defaultProjectile = verb_Shoot.verbProps.defaultProjectile;
        if (defaultProjectile == null)
        {
            return 0f;
        }

        if (defaultProjectile.projectile.flyOverhead)
        {
            return 0f;
        }

        var map = pawn.Map;
        var report = ShotReport.HitReportFor(pawn, verb, (Thing)target);
        var a = VerbUtility.CalculateAdjustedForcedMiss(verb.verbProps.ForcedMissRadius,
            report.ShootLine.Dest - report.ShootLine.Source);
        var radius = Mathf.Max(a, 1.5f);
        var dest2 = report.ShootLine.Dest;
        var source = from dest in GenRadial.RadialCellsAround(dest2, radius, true)
            where dest.InBounds(map)
            select dest;
        var source2 = source.Select(dest => new ShootLine(report.ShootLine.Source, dest));
        var source3 = source2.SelectMany(line =>
            line.Points().Concat(line.Dest).TakeWhile(pos => pos.CanBeSeenOverFast(map)));
        var enumerable = source3.Distinct();
        var num = 0f;
        foreach (var item in enumerable)
        {
            var num2 = VerbUtility.InterceptChanceFactorFromDistance(report.ShootLine.Source.ToVector3Shifted(), item);
            if (!(num2 > 0f))
            {
                continue;
            }

            var thingList = item.GetThingList(map);
            foreach (var thing in thingList)
            {
                if (thing is not IAttackTarget || thing == target)
                {
                    continue;
                }

                var num3 = thing == searcher ? 40f : thing is not Pawn ? 10f : !thing.def.race.Animal ? 18f : 7f;
                num3 *= num2;
                num3 = !searcher.Thing.HostileTo(thing) ? num3 * -1f : num3 * 0.6f;
                num += num3;
            }
        }

        return num;
    }

    public static IAttackTarget BestShootTargetFromCurrentPosition(IAttackTargetSearcher searcher, Verb verb,
        TargetScanFlags flags, Predicate<Thing> validator = null, float minDistance = 0f, float maxDistance = 9999f)
    {
        if (verb != null)
        {
            return BestAttackTarget(searcher, verb, flags, validator, Mathf.Max(minDistance, verb.verbProps.minRange),
                Mathf.Min(maxDistance, verb.verbProps.range), default, float.MaxValue, false, false);
        }

        Log.Error($"BestShootTargetFromCurrentPosition with {searcher.ToStringSafe()} who has no attack verb.");
        return null;
    }
}