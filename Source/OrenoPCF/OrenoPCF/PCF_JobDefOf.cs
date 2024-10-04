using RimWorld;
using Verse;

namespace OrenoPCF;

[DefOf]
public static class PCF_JobDefOf
{
	public static JobDef PCF_AttackStaticExtended;

	static PCF_JobDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(PCF_JobDefOf));
	}
}
