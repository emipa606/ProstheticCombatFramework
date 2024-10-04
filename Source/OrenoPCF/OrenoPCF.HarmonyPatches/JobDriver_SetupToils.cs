using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace OrenoPCF.HarmonyPatches;

[HarmonyPatch(typeof(JobDriver), "SetupToils")]
internal class JobDriver_SetupToils
{
    private static void Postfix(JobDriver __instance, List<Toil> ___toils)
    {
        if (__instance is not JobDriver_Goto jobDriver_Goto)
        {
            return;
        }

        if (!___toils.Any())
        {
            return;
        }

        var toil = ___toils.First();
        toil.AddPreTickAction(delegate
        {
            if (!jobDriver_Goto.pawn.Downed && jobDriver_Goto.pawn.Faction != null &&
                (jobDriver_Goto.pawn.drafter == null || jobDriver_Goto.pawn.drafter.FireAtWill) &&
                jobDriver_Goto.pawn.IsHashIntervalTick(10))
            {
                PCF_VanillaExtender.CheckForAutoAttack(jobDriver_Goto);
            }
        });
    }
}