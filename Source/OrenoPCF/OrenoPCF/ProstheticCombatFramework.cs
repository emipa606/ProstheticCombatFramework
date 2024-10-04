using System.Reflection;
using HarmonyLib;
using Verse;

namespace OrenoPCF;

[StaticConstructorOnStartup]
public class ProstheticCombatFramework
{
	static ProstheticCombatFramework()
	{
		Harmony harmony = new Harmony("OrenoPCF");
		harmony.PatchAll(Assembly.GetExecutingAssembly());
	}
}
