using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrenoPCF;

public class Command_HediffVerbRanged : Command
{
    public HediffComp_VerbGiverExtended rangedComp;

    public Command_HediffVerbRanged()
    {
        activateSound = SoundDefOf.Tick_Tiny;
    }

    public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
    {
        get
        {
            foreach (var rightClickFloatMenuOption in base.RightClickFloatMenuOptions)
            {
                yield return rightClickFloatMenuOption;
            }

            if (rangedComp.AllVerbs == null)
            {
                yield break;
            }

            foreach (var item in rangedComp.AllVerbs.Where(verbs => !verbs.IsMeleeAttack))
            {
                var verb = item;
                var verbLabel = verb.verbProps.label.CapitalizeFirst();
                yield return new FloatMenuOption(verbLabel, selectVerb);
                continue;

                void selectVerb()
                {
                    rangedComp.rangedVerb = verb;
                    foreach (var verbsProperty in rangedComp.Props.verbsProperties)
                    {
                        var verbProps = rangedComp.rangedVerb.verbProps;
                        if (verbProps.label != verbsProperty.label)
                        {
                            continue;
                        }

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

    public override void GizmoUpdateOnMouseover()
    {
        rangedComp.rangedVerb.verbProps.DrawRadiusRing(rangedComp.rangedVerb.caster.Position);
    }

    public override void ProcessInput(Event ev)
    {
        base.ProcessInput(ev);
        Find.Targeter.BeginTargeting(rangedComp.rangedVerb);
    }

    public override bool GroupsWith(Gizmo other)
    {
        return false;
    }
}