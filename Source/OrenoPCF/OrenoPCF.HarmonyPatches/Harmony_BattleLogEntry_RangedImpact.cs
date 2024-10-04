using HarmonyLib;
using Verse;

namespace OrenoPCF.HarmonyPatches;

internal class Harmony_BattleLogEntry_RangedImpact
{
	[HarmonyPatch(typeof(BattleLogEntry_RangedImpact))]
	[HarmonyPatch("GenerateGrammarRequest")]
	internal class GenerateGrammarRequest
	{
		[HarmonyPrefix]
		public static bool MissingWeaponDefFix(BattleLogEntry_RangedImpact __instance)
		{
			Traverse traverse = Traverse.Create(__instance);
			ThingDef value = traverse.Field("weaponDef").GetValue<ThingDef>();
			ThingDef value2 = traverse.Field("projectileDef").GetValue<ThingDef>();
			if (value == null)
			{
				ThingDef named = DefDatabase<ThingDef>.GetNamed("PCF_LogPlaceholder");
				named.Verbs[0].defaultProjectile = value2;
				traverse.Field("weaponDef").SetValue(named);
			}
			return true;
		}
	}
}
