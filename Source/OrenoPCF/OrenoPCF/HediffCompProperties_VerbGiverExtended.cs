using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OrenoPCF;

public class HediffCompProperties_VerbGiverExtended : HediffCompProperties
{
	public string toggleLabel;

	[Description("A human-readable description given when the Def is inspected by players.")]
	[DefaultValue(null)]
	[MustTranslate]
	public string toggleDescription;

	[NoTranslate]
	public string toggleIconPath;

	public float toggleIconAngle;

	public Vector2 toggleIconOffset;

	public List<VerbProperties> verbs;

	public List<PCF_VerbProperties> verbsProperties;

	public HediffCompProperties_VerbGiverExtended()
	{
		compClass = typeof(HediffComp_VerbGiverExtended);
	}

	public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
	{
		foreach (string item in base.ConfigErrors(parentDef))
		{
			yield return item;
		}
		if (verbs == null)
		{
			yield break;
		}
		VerbProperties dupeVerb = verbs.SelectMany((VerbProperties lhs) => verbs.Where((VerbProperties rhs) => lhs != rhs && lhs.label == rhs.label)).FirstOrDefault();
		if (dupeVerb != null)
		{
			yield return $"duplicate hediff verb label {dupeVerb.label}";
		}
		PCF_VerbProperties dupeVerbProperties = verbsProperties.SelectMany((PCF_VerbProperties lhs) => verbsProperties.Where((PCF_VerbProperties rhs) => lhs != rhs && lhs.label == rhs.label)).FirstOrDefault();
		if (dupeVerbProperties != null)
		{
			yield return $"duplicate hediff verb properties label {dupeVerbProperties.label}";
		}
	}
}
