using System.Reflection;
using HarmonyLib;
using Verse;

namespace OrenoPCF;

[StaticConstructorOnStartup]
public static class ProstheticCombatFramework
{
    public static readonly ThingDef LogPlaceholder = DefDatabase<ThingDef>.GetNamed("PCF_LogPlaceholder");

    static ProstheticCombatFramework()
    {
        new Harmony("OrenoPCF").PatchAll(Assembly.GetExecutingAssembly());
    }
}