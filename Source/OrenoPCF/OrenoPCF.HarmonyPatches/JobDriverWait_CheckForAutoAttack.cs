using HarmonyLib;
using RimWorld;
using Verse.AI;

namespace OrenoPCF.HarmonyPatches;

[HarmonyPatch(typeof(JobDriver_Wait), "CheckForAutoAttack")]
internal class JobDriverWait_CheckForAutoAttack
{
    private static void Postfix(JobDriver_Wait __instance)
    {
        if (!__instance.pawn.Downed && __instance.pawn.Faction != null && __instance.job.def == JobDefOf.Wait_Combat &&
            (__instance.pawn.drafter == null || __instance.pawn.drafter.FireAtWill))
        {
            PCF_VanillaExtender.CheckForAutoAttack(__instance);
        }
    }
}