using UnityEngine;
using Verse;

namespace OrenoPCF;

public class PCF_VerbProperties
{
	public string label;

	[Description("A human-readable description given when the Verb is inspected by players.")]
	[DefaultValue(null)]
	[MustTranslate]
	public string description;

	[NoTranslate]
	public string uiIconPath;

	public float uiIconAngle;

	public Vector2 uiIconOffset;
}
