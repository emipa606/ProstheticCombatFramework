using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrenoPCF;

public class HediffComp_VerbGiverExtended : HediffComp, IVerbOwner
{
    private readonly int autoAttackFrequency = 100;

    private int autoAttackTick;

    public bool canAttack;

    public bool canAutoAttack = true;

    public Verb rangedVerb;

    public string rangedVerbDescription;

    public float rangedVerbIconAngle;

    public Vector2 rangedVerbIconOffset;

    public string rangedVerbIconPath;

    public string rangedVerbLabel;

    public float rangedVerbWarmupTime;
    public VerbTracker verbTracker;

    public HediffComp_VerbGiverExtended()
    {
        verbTracker = new VerbTracker(this);
    }

    public HediffCompProperties_VerbGiverExtended Props => (HediffCompProperties_VerbGiverExtended)props;

    public List<Verb> AllVerbs => verbTracker.AllVerbs;

    public VerbTracker VerbTracker => verbTracker;

    public List<VerbProperties> VerbProperties => Props.verbs;

    public List<Tool> Tools => null;

    Thing IVerbOwner.ConstantCaster => Pawn;

    ImplementOwnerTypeDef IVerbOwner.ImplementOwnerTypeDef => ImplementOwnerTypeDefOf.Hediff;

    string IVerbOwner.UniqueVerbOwnerID()
    {
        return $"{parent.GetUniqueLoadID()}_{parent.comps.IndexOf(this)}";
    }

    bool IVerbOwner.VerbsStillUsableBy(Pawn p)
    {
        return p.health.hediffSet.hediffs.Contains(parent);
    }

    public void InitializeRangedVerb()
    {
        rangedVerb = AllVerbs.FirstOrDefault(verbs => !verbs.IsMeleeAttack);
        foreach (var verbsProperty in Props.verbsProperties)
        {
            var verbProps = rangedVerb.verbProps;
            if (verbProps.label != verbsProperty.label)
            {
                continue;
            }

            rangedVerbLabel = verbsProperty.label;
            rangedVerbDescription = verbsProperty.description;
            rangedVerbIconPath = verbsProperty.uiIconPath;
            rangedVerbIconAngle = verbsProperty.uiIconAngle;
            rangedVerbIconOffset = verbsProperty.uiIconOffset;
        }
    }

    public override void CompPostMake()
    {
        base.CompPostMake();
        InitializeRangedVerb();
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Deep.Look(ref verbTracker, "verbTracker", this);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && verbTracker == null)
        {
            verbTracker = new VerbTracker(this);
        }

        Scribe_Values.Look(ref canAutoAttack, "canAutoAttack", true);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && (rangedVerb == null || rangedVerbLabel == null))
        {
            InitializeRangedVerb();
        }
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);
        verbTracker.VerbsTick();
        if (autoAttackTick >= Find.TickManager.TicksGame)
        {
            return;
        }

        canAttack = true;
        autoAttackTick = Find.TickManager.TicksGame +
                         (int)Rand.Range(0.8f * autoAttackFrequency, 1.2f * autoAttackFrequency);
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        PCF_VanillaExtender.ResetIcons();
    }
}