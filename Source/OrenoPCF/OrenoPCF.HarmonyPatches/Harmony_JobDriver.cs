using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace OrenoPCF.HarmonyPatches;

public class Harmony_JobDriver
{
	[HarmonyPatch(typeof(JobDriver))]
	[HarmonyPatch("SetupToils")]
	internal class JobDriver_SetupToils
	{
		[HarmonyPostfix]
		private static void VerbGiverExtended(JobDriver __instance)
		{
			JobDriver_Goto jobDriver_Goto = __instance as JobDriver_Goto;
			if (jobDriver_Goto == null)
			{
				return;
			}
			List<Toil> value = Traverse.Create(jobDriver_Goto).Field("toils").GetValue<List<Toil>>();
			if (value.Count() <= 0)
			{
				return;
			}
			Toil toil = value.ElementAt(0);
			toil.AddPreTickAction(delegate
			{
				if (!jobDriver_Goto.pawn.Downed && jobDriver_Goto.pawn.Faction != null && (jobDriver_Goto.pawn.drafter == null || jobDriver_Goto.pawn.drafter.FireAtWill) && jobDriver_Goto.pawn.IsHashIntervalTick(10))
				{
					PCF_VanillaExtender.CheckForAutoAttack(jobDriver_Goto);
				}
			});
		}
	}
}
