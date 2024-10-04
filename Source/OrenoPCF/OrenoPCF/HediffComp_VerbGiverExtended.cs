using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace OrenoPCF;

public class HediffComp_VerbGiverExtended : HediffComp, IVerbOwner
{
	public VerbTracker verbTracker;

	public Verb rangedVerb;

	public string rangedVerbLabel;

	public string rangedVerbDescription;

	public string rangedVerbIconPath;

	public float rangedVerbIconAngle;

	public Vector2 rangedVerbIconOffset;

	public float rangedVerbWarmupTime;

	public bool canAttack = false;

	public bool canAutoAttack = true;

	private int autoAttackTick = 0;

	private readonly int autoAttackFrequency = 100;

	public HediffCompProperties_VerbGiverExtended Props => (HediffCompProperties_VerbGiverExtended)props;

	public List<Verb> AllVerbs => verbTracker.AllVerbs;

	public VerbTracker VerbTracker => verbTracker;

	public List<VerbProperties> VerbProperties => Props.verbs;

	public List<Tool> Tools => null;

	Thing IVerbOwner.ConstantCaster => base.Pawn;

	ImplementOwnerTypeDef IVerbOwner.ImplementOwnerTypeDef => ImplementOwnerTypeDefOf.Hediff;

	public HediffComp_VerbGiverExtended()
	{
		verbTracker = new VerbTracker(this);
	}

	public void InitializeRangedVerb()
	{
		rangedVerb = AllVerbs.Where((Verb verbs) => !verbs.IsMeleeAttack).FirstOrDefault();
		foreach (PCF_VerbProperties verbsProperty in Props.verbsProperties)
		{
			VerbProperties verbProps = rangedVerb.verbProps;
			if (verbProps.label == verbsProperty.label)
			{
				rangedVerbLabel = verbsProperty.label;
				rangedVerbDescription = verbsProperty.description;
				rangedVerbIconPath = verbsProperty.uiIconPath;
				rangedVerbIconAngle = verbsProperty.uiIconAngle;
				rangedVerbIconOffset = verbsProperty.uiIconOffset;
			}
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
		Scribe_Values.Look(ref canAutoAttack, "canAutoAttack", defaultValue: true);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && (rangedVerb == null || rangedVerbLabel == null))
		{
			InitializeRangedVerb();
		}
	}

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		verbTracker.VerbsTick();
		if (autoAttackTick < Find.TickManager.TicksGame)
		{
			canAttack = true;
			autoAttackTick = Find.TickManager.TicksGame + (int)Rand.Range(0.8f * (float)autoAttackFrequency, 1.2f * (float)autoAttackFrequency);
		}
	}

	public override void CompPostPostRemoved()
	{
		base.CompPostPostRemoved();
		PCF_VanillaExtender.ResetIcons();
	}

	string IVerbOwner.UniqueVerbOwnerID()
	{
		return parent.GetUniqueLoadID() + "_" + parent.comps.IndexOf(this);
	}

	bool IVerbOwner.VerbsStillUsableBy(Pawn p)
	{
		return p.health.hediffSet.hediffs.Contains(parent);
	}
}
