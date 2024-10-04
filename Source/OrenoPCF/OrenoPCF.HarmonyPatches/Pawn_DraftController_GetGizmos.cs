using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace OrenoPCF.HarmonyPatches;

[HarmonyPatch(typeof(Pawn_DraftController), "GetGizmos")]
internal class Pawn_DraftController_GetGizmos
{
    private static void Postfix(ref IEnumerable<Gizmo> __result, Pawn_DraftController __instance)
    {
        var first = new List<Gizmo>(__result);
        var list = new List<Gizmo>();
        var list2 = new List<Gizmo>();
        foreach (var hediffComp in from c in
                     __instance.pawn.health.hediffSet.GetAllComps()
                 where c is HediffComp_VerbGiverExtended
                 select c)
        {
            var verbGiverExtended = (HediffComp_VerbGiverExtended)hediffComp;
            var command_Toggle = new Command_Toggle
            {
                isActive = () => verbGiverExtended.canAutoAttack,
                toggleAction = delegate { verbGiverExtended.canAutoAttack = !verbGiverExtended.canAutoAttack; },
                defaultLabel = verbGiverExtended.Props.toggleLabel,
                defaultDesc = verbGiverExtended.Props.toggleDescription.CapitalizeFirst(),
                icon = PCF_VanillaExtender.GetIcon(
                    $"{verbGiverExtended.Pawn.GetUniqueLoadID()}_{verbGiverExtended.parent.GetUniqueLoadID()}",
                    verbGiverExtended.Props.toggleIconPath),
                iconAngle = verbGiverExtended.Props.toggleIconAngle,
                iconOffset = verbGiverExtended.Props.toggleIconOffset
            };
            if (__instance.pawn.Faction != Faction.OfPlayer)
            {
                command_Toggle.Disable("CannotOrderNonControlled".Translate());
            }

            if (__instance.pawn.Downed)
            {
                command_Toggle.Disable("IsIncapped".Translate(__instance.pawn.LabelShort, __instance.pawn));
            }

            list.Add(command_Toggle);
            var command_HediffVerbRanged = new Command_HediffVerbRanged
            {
                rangedComp = verbGiverExtended,
                defaultLabel = verbGiverExtended.rangedVerbLabel,
                defaultDesc = verbGiverExtended.rangedVerbDescription.CapitalizeFirst(),
                icon = PCF_VanillaExtender.GetIcon(
                    $"{verbGiverExtended.Pawn.GetUniqueLoadID()}_{verbGiverExtended.rangedVerb.loadID}",
                    verbGiverExtended.rangedVerbIconPath),
                iconAngle = verbGiverExtended.rangedVerbIconAngle,
                iconOffset = verbGiverExtended.rangedVerbIconOffset
            };
            if (__instance.pawn.Faction != Faction.OfPlayer)
            {
                command_HediffVerbRanged.Disable("CannotOrderNonControlled".Translate());
            }
            else if (__instance.pawn.IsColonist)
            {
                if (__instance.pawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent))
                {
                    command_HediffVerbRanged.Disable(
                        "IsIncapableOfViolence".Translate(__instance.pawn.LabelShort, __instance.pawn));
                }
                else if (!__instance.pawn.drafter.Drafted)
                {
                    command_HediffVerbRanged.Disable("IsNotDrafted".Translate(__instance.pawn.LabelShort,
                        __instance.pawn));
                }
            }

            list2.Add(command_HediffVerbRanged);
        }

        __result = first.Concat(list).Concat(list2);
    }
}