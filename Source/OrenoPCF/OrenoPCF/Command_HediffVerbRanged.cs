using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrenoPCF;

public class Command_HediffVerbRanged : Command
{
	public HediffComp_VerbGiverExtended rangedComp;

	public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
	{
		get
		{
			foreach (FloatMenuOption rightClickFloatMenuOption in base.RightClickFloatMenuOptions)
			{
				yield return rightClickFloatMenuOption;
			}
			if (rangedComp.AllVerbs == null)
			{
				yield break;
			}
			foreach (Verb item in rangedComp.AllVerbs.Where((Verb verbs) => !verbs.IsMeleeAttack))
			{
				Verb verb = item;
				string verbLabel = verb.verbProps.label.CapitalizeFirst();
				yield return new FloatMenuOption(verbLabel, selectVerb);
				void selectVerb()
				{
					rangedComp.rangedVerb = verb;
					foreach (PCF_VerbProperties verbsProperty in rangedComp.Props.verbsProperties)
					{
						VerbProperties verbProps = rangedComp.rangedVerb.verbProps;
						if (verbProps.label == verbsProperty.label)
						{
							rangedComp.rangedVerbLabel = verbsProperty.label;
							rangedComp.rangedVerbDescription = verbsProperty.description;
							rangedComp.rangedVerbIconPath = verbsProperty.uiIconPath;
							rangedComp.rangedVerbIconAngle = verbsProperty.uiIconAngle;
							rangedComp.rangedVerbIconOffset = verbsProperty.uiIconOffset;
						}
					}
				}
			}
		}
	}

	public override void GizmoUpdateOnMouseover()
	{
		rangedComp.rangedVerb.verbProps.DrawRadiusRing(rangedComp.rangedVerb.caster.Position);
	}

	public override void ProcessInput(Event ev)
	{
		base.ProcessInput(ev);
		Find.Targeter.BeginTargeting((ITargetingSource)rangedComp.rangedVerb, (ITargetingSource)null);
	}

	public override bool GroupsWith(Gizmo other)
	{
		return false;
	}

	public Command_HediffVerbRanged()
	{
		activateSound = SoundDefOf.Tick_Tiny;
	}
}
