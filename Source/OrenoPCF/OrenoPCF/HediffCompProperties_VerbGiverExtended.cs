using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OrenoPCF;

public class HediffCompProperties_VerbGiverExtended : HediffCompProperties
{
    [Description("A human-readable description given when the Def is inspected by players.")]
    [DefaultValue(null)]
    [MustTranslate]
    public string toggleDescription;

    public float toggleIconAngle;

    public Vector2 toggleIconOffset;

    [NoTranslate] public string toggleIconPath;

    public string toggleLabel;

    public List<VerbProperties> verbs;

    public List<PCF_VerbProperties> verbsProperties;

    public HediffCompProperties_VerbGiverExtended()
    {
        compClass = typeof(HediffComp_VerbGiverExtended);
    }

    public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
    {
        foreach (var item in base.ConfigErrors(parentDef))
        {
            yield return item;
        }

        if (verbs == null)
        {
            yield break;
        }

        var dupeVerb = verbs.SelectMany(lhs => verbs.Where(rhs => lhs != rhs && lhs.label == rhs.label))
            .FirstOrDefault();
        if (dupeVerb != null)
        {
            yield return $"duplicate hediff verb label {dupeVerb.label}";
        }

        var dupeVerbProperties = verbsProperties
            .SelectMany(lhs => verbsProperties.Where(rhs => lhs != rhs && lhs.label == rhs.label)).FirstOrDefault();
        if (dupeVerbProperties != null)
        {
            yield return $"duplicate hediff verb properties label {dupeVerbProperties.label}";
        }
    }
}