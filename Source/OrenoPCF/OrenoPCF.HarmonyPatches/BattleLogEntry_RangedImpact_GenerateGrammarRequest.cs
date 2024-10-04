using HarmonyLib;
using Verse;

namespace OrenoPCF.HarmonyPatches;

[HarmonyPatch(typeof(BattleLogEntry_RangedImpact), "GenerateGrammarRequest")]
internal class BattleLogEntry_RangedImpact_GenerateGrammarRequest
{
    public static void Prefix(ref ThingDef ___weaponDef, ThingDef ___projectileDef)
    {
        if (___weaponDef != null)
        {
            return;
        }

        ProstheticCombatFramework.LogPlaceholder.Verbs[0].defaultProjectile = ___projectileDef;
        ___weaponDef = ProstheticCombatFramework.LogPlaceholder;
    }
}