using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Presentation/Environment Palette")]
public sealed class EnvironmentPresentationDefinition : ScriptableObject
{
    [Serializable]
    public struct Palette
    {
        public Color Filter;
        [Range(.55f, 1.2f)] public float Intensity;
        [Range(0, .4f)] public float Vignette;

        public static Palette Lerp(Palette a, Palette b, float t) => new()
        {
            Filter = Color.Lerp(a.Filter, b.Filter, t),
            Intensity = Mathf.Lerp(a.Intensity, b.Intensity, t),
            Vignette = Mathf.Lerp(a.Vignette, b.Vignette, t)
        };
    }

    public Palette Day = new() { Filter = new Color(1, .98f, .91f), Intensity = 1, Vignette = .06f };
    public Palette Dusk = new() { Filter = new Color(1, .76f, .64f), Intensity = .9f, Vignette = .16f };
    public Palette Night = new() { Filter = new Color(.68f, .76f, 1), Intensity = .72f, Vignette = .22f };
    [Tooltip("Blend to the next palette during the final fraction of each phase; uses the gameplay clock.")]
    [Range(.05f, 1)] public float TransitionFraction = .2f;
    [Header("Night lighting and VFX budgets")]
    public Material NightGlowMaterial;
    public Color WarmEmissive = new(1, .64f, .28f);
    [Range(0, 8)] public int LocalLightBudget = 8;
    [Range(0, 128)] public int TransientVfxBudget = 32;

    public Palette Evaluate(DayNightPhase phase, float progress)
    {
        progress = float.IsFinite(progress) ? Mathf.Clamp01(progress) : 0;
        float t = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1 - TransitionFraction, 1, progress));
        return phase switch
        {
            DayNightPhase.Day => Palette.Lerp(Day, Dusk, t),
            DayNightPhase.Dusk => Palette.Lerp(Dusk, Night, t),
            _ => Palette.Lerp(Night, Day, t)
        };
    }

    public void Validate()
    {
        foreach (var palette in new[] { Day, Dusk, Night })
            if (!float.IsFinite(palette.Intensity) || palette.Intensity < .55f || palette.Intensity > 1.2f
                || !float.IsFinite(palette.Vignette) || palette.Vignette < 0 || palette.Vignette > .4f
                || !ValidColor(palette.Filter))
                throw new InvalidOperationException("Environment palette has invalid color, intensity or vignette.");
        if (!float.IsFinite(TransitionFraction) || TransitionFraction < .05f || TransitionFraction > 1
            || !ValidColor(WarmEmissive) || LocalLightBudget < 0 || LocalLightBudget > 8
            || TransientVfxBudget < 0 || TransientVfxBudget > 128)
            throw new InvalidOperationException("Environment transition or presentation budget is invalid.");
    }

    static bool ValidColor(Color c) => float.IsFinite(c.r) && float.IsFinite(c.g) && float.IsFinite(c.b)
        && c.r >= 0 && c.g >= 0 && c.b >= 0 && c.r <= 1 && c.g <= 1 && c.b <= 1 && c.a == 1;
}
