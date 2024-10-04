using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace OrenoPCF;

public static class PCF_VanillaExtender
{
    public static readonly Dictionary<string, Texture2D> iconsCache = new Dictionary<string, Texture2D>();

    public static void CheckForAutoAttack(JobDriver jobDriver)
    {
        var hediffs = jobDriver.pawn.health.hediffSet.hediffs;
        foreach (var item in hediffs)
        {
            var hediffComp_VerbGiverExtended = item.TryGetComp<HediffComp_VerbGiverExtended>();
            if (hediffComp_VerbGiverExtended == null)
            {
                continue;
            }

            var list = new List<Verb>(
                hediffComp_VerbGiverExtended.AllVerbs.SkipWhile(verb => verb is Verb_CastPsycast));
            var index = Random.Range(0, list.Count);
            if (list[index] != null && hediffComp_VerbGiverExtended.canAutoAttack &&
                hediffComp_VerbGiverExtended.canAttack)
            {
                var targetScanFlags = TargetScanFlags.NeedLOSToAll | TargetScanFlags.NeedThreat;
                if (list[index].IsIncendiary_Ranged() || list[index].IsIncendiary_Melee())
                {
                    targetScanFlags |= TargetScanFlags.NeedNonBurning;
                }

                var thing = (Thing)PCF_AttackTargetFinder.BestShootTargetFromCurrentPosition(jobDriver.pawn,
                    list[index], targetScanFlags);
                if (thing != null && !list[index].IsMeleeAttack)
                {
                    hediffComp_VerbGiverExtended.rangedVerbWarmupTime = list[index].verbProps.warmupTime;
                    list[index].verbProps.warmupTime = 0f;
                    list[index].TryStartCastOn((LocalTargetInfo)thing);
                    list[index].verbProps.warmupTime = hediffComp_VerbGiverExtended.rangedVerbWarmupTime;
                }
            }

            hediffComp_VerbGiverExtended.canAttack = false;
        }
    }

    public static Texture2D GetIcon(string loadID, string iconPath)
    {
        if (iconsCache.TryGetValue(loadID, out var icon))
        {
            return icon;
        }

        var texture2D = BaseContent.BadTex;
        if (iconPath.NullOrEmpty())
        {
            return texture2D;
        }

        texture2D = ContentFinder<Texture2D>.Get(iconPath);
        iconsCache.Add(loadID, texture2D);

        return texture2D;
    }

    public static void ResetIcons()
    {
        iconsCache.Clear();
    }
}