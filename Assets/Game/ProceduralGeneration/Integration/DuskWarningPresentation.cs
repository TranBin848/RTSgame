using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presentation only: the clock, wave schedule and explored gateway cells remain authoritative.
[DisallowMultipleComponent]
public sealed class DuskWarningPresentation : MonoBehaviour
{
    DayNightCycleManager m_Clock;
    EnemyWaveManager m_Waves;
    ProceduralMapAuthoring m_Map;
    FogOfWarManager m_Fog;
    Camera m_Camera;
    RectTransform m_Canvas;
    GameObject m_Banner;
    TextMeshProUGUI m_Title, m_Detail;
    RectTransform[] m_Markers;
    RectTransform[] m_Arrows;
    AudioSource m_Audio;
    AudioClip m_Bell;
    DayNightPhase m_LastPhase;
    int m_LastSeconds = -1, m_LastWaves = -1, m_LastKnown = -1;
    DayNightPhase m_DisplayedPhase = (DayNightPhase)(-1);

    public bool IsInitialized => m_Clock != null;
    public int DuskSignalCount { get; private set; }
    public int KnownApproachCount { get; private set; }
    public int SecondsUntilNight => m_Clock != null && m_Clock.CurrentPhase == DayNightPhase.Dusk
        ? Mathf.CeilToInt(Mathf.Max(0, (1 - m_Clock.CurrentPhaseProgress) * m_Clock.Definition.DuskDuration)) : 0;
    public string WarningText => m_Title != null ? m_Title.text + "\n" + m_Detail.text : "";

    public void Initialize(DayNightCycleManager clock, EnemyWaveManager waves, ProceduralMapAuthoring map,
        FogOfWarManager fog, Camera viewCamera, Canvas canvas)
    {
        if (clock == null || clock.Definition == null || waves == null || map == null || fog == null
            || viewCamera == null || canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            throw new ArgumentException("Dusk warning requires clock, waves, map, fog, camera and an overlay HUD.");
        if (IsInitialized)
        {
            if (m_Clock != clock || m_Waves != waves || m_Map != map || m_Fog != fog
                || m_Camera != viewCamera || m_Canvas != canvas.transform)
                throw new InvalidOperationException("Dusk warning already belongs to another scene context.");
            return;
        }
        m_Clock = clock; m_Waves = waves; m_Map = map; m_Fog = fog;
        m_Camera = viewCamera; m_Canvas = (RectTransform)canvas.transform;
        BuildHud();
        m_Bell = CreateBell();
        m_Audio = gameObject.AddComponent<AudioSource>();
        m_Audio.playOnAwake = false; m_Audio.loop = false; m_Audio.spatialBlend = 0;
        m_Audio.volume = 0.28f; m_Audio.clip = m_Bell;
        m_LastPhase = clock.CurrentPhase;
        clock.OnPhaseChanged += OnPhaseChanged;
        RefreshPresentation();
    }

    void OnPhaseChanged(DayNightPhase phase)
    {
        if (phase == m_LastPhase) return;
        m_LastPhase = phase;
        if (phase == DayNightPhase.Dusk)
        {
            DuskSignalCount++;
            if (isActiveAndEnabled) m_Audio.Play();
        }
        RefreshPresentation();
    }

    void LateUpdate() => RefreshPresentation();

    public void RefreshPresentation()
    {
        if (!IsInitialized) return;
        var phase = m_Clock.CurrentPhase;
        bool show = phase != DayNightPhase.Day;
        m_Banner.SetActive(show);
        KnownApproachCount = 0;
        for (int i = 0; i < m_Markers.Length; i++)
        {
            var gate = m_Map.GeneratedMap.Layout.Gateways[i];
            var cell = m_Map.OriginCell + new Vector3Int(gate.X, gate.Y, 0);
            // Check the gateway itself, not a visible point on the route leading to it.
            bool known = m_Fog.IsInitialized && m_Fog.TryGetCellState(cell, out var state, out _)
                && state != FogCellState.Unexplored;
            if (known) KnownApproachCount++;
            m_Markers[i].gameObject.SetActive(show && known);
            if (!show || !known) continue;
            var world = m_Fog.FogTilemap.GetCellCenterWorld(cell);
            var screen = m_Camera.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Canvas, screen, null, out var point);
            var rect = m_Canvas.rect;
            m_Markers[i].anchoredPosition = new Vector2(
                Mathf.Clamp(point.x, rect.xMin + 95, rect.xMax - 95),
                Mathf.Clamp(point.y, rect.yMin + 250, rect.yMax - 150));
            var direction = m_Camera.WorldToScreenPoint(world) - m_Camera.WorldToScreenPoint(m_Camera.transform.position + m_Camera.transform.forward * 10);
            m_Arrows[i].localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90);
        }
        int seconds = SecondsUntilNight, waves = m_Waves.CurrentWaveCount;
        if (seconds == m_LastSeconds && waves == m_LastWaves && KnownApproachCount == m_LastKnown && phase == m_DisplayedPhase) return;
        m_LastSeconds = seconds; m_LastWaves = waves; m_LastKnown = KnownApproachCount; m_DisplayedPhase = phase;
        m_Title.text = phase == DayNightPhase.Dusk ? $"! DUSK  |  Night in {seconds / 60:00}:{seconds % 60:00}" : "! NIGHT  |  Defend your Town Hall";
        m_Detail.text = KnownApproachCount == 0
            ? $"{waves} waves tonight. Enemy approaches unknown - scout the border."
            : $"{waves} waves tonight. Markers: known possible approaches.\nRoutes chosen on spawn; stay alert for unexplored approaches.";
        if (m_Waves.TacticalPlan != null)
            m_Detail.text = $"{waves} waves · {m_Waves.TacticalBriefing}\n"
                + (KnownApproachCount == 0 ? "Approaches unknown - scout the border." : "Known possible approaches marked; blocked routes may reroute.");
    }

    void BuildHud()
    {
        var panel = NewRect("Dusk warning", m_Canvas, new Vector2(570, 88));
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1);
        panel.pivot = new Vector2(0.5f, 1); panel.anchoredPosition = new Vector2(0, -140);
        m_Banner = panel.gameObject;
        var background = m_Banner.AddComponent<Image>();
        background.color = new Color(0.11f, 0.07f, 0.055f, 0.94f); background.raycastTarget = false;
        m_Title = Label("Phase and countdown", panel, new Vector2(550, 32), new Vector2(0, 22), 23);
        m_Title.color = new Color(1, 0.8f, 0.38f);
        m_Detail = Label("Warning details", panel, new Vector2(550, 43), new Vector2(0, -16), 16);
        m_Markers = new RectTransform[m_Map.GatewayCount]; m_Arrows = new RectTransform[m_Markers.Length];
        for (int i = 0; i < m_Markers.Length; i++)
        {
            var marker = NewRect("Known possible approach", m_Canvas, new Vector2(150, 65));
            var arrow = Label("Direction", marker, new Vector2(36, 28), new Vector2(0, 20), 30);
            arrow.text = "^"; arrow.color = m_Title.color; m_Arrows[i] = arrow.rectTransform;
            var label = Label("Approach label", marker, new Vector2(150, 40), new Vector2(0, -12), 15);
            label.text = "! POSSIBLE\nAPPROACH"; label.color = m_Title.color;
            m_Markers[i] = marker;
        }
    }

    static RectTransform NewRect(string name, Transform parent, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.sizeDelta = size;
        return rect;
    }

    static TextMeshProUGUI Label(string name, Transform parent, Vector2 size, Vector2 position, float fontSize)
    {
        var rect = NewRect(name, parent, size); rect.anchoredPosition = position;
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset; text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        text.color = new Color(1, 0.94f, 0.8f); text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    static AudioClip CreateBell()
    {
        const int rate = 22050;
        var samples = new float[rate * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / rate;
            float attack = Mathf.Min(1, t / 0.008f);
            samples[i] = attack * (0.55f * Mathf.Sin(2 * Mathf.PI * 440 * t) * Mathf.Exp(-3 * t)
                + 0.24f * Mathf.Sin(2 * Mathf.PI * 880 * t) * Mathf.Exp(-4.5f * t)
                + 0.12f * Mathf.Sin(2 * Mathf.PI * 1193 * t) * Mathf.Exp(-6 * t));
        }
        var clip = AudioClip.Create("Dusk bell (deterministic)", samples.Length, 1, rate, false);
        clip.SetData(samples, 0); return clip;
    }

    void OnDestroy()
    {
        if (m_Clock != null) m_Clock.OnPhaseChanged -= OnPhaseChanged;
        if (m_Banner != null) Destroy(m_Banner);
        if (m_Markers != null) foreach (var marker in m_Markers) if (marker != null) Destroy(marker.gameObject);
        if (m_Audio != null) Destroy(m_Audio);
        if (m_Bell != null) Destroy(m_Bell);
    }
}
