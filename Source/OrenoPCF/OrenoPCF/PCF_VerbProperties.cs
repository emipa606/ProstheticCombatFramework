using UnityEngine;
using Verse;

namespace OrenoPCF;

public class PCF_VerbProperties
{
    [Description("A human-readable description given when the Verb is inspected by players.")]
    [DefaultValue(null)]
    [MustTranslate]
    public string description;

    public string label;

    public float uiIconAngle;

    public Vector2 uiIconOffset;

    [NoTranslate] public string uiIconPath;
}