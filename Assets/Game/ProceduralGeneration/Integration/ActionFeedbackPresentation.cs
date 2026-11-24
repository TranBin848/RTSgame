using System;
using RTS.DOTS;
using TMPro;
using UnityEngine;

// Read-only consumer: events are drained by the bridge once; snapshots only position persistent badges.
[DefaultExecutionOrder(180)]
public sealed class ActionFeedbackPresentation : MonoBehaviour
{
    sealed class Pulse
    {
        public SpriteRenderer View;
        public Vector3 Position;
        public Color Tint;
        public float Age, Duration, Size;
    }
    SwarmGameBridge m_Bridge;
    SwarmSimulationSystem m_Simulation;
    SwarmInstancedRenderer m_Native;
    GameManager m_Game;
    FogOfWarManager m_Fog;
    Camera m_Camera;
    IResearchService m_Research;
    Pulse[] m_Pulses;
    SpriteRenderer[] m_Badges;
    Sprite m_Impact, m_Dust, m_Ring, m_Crate, m_Fortify;
    Texture2D m_Texture;
    Material m_Material;
    TextMeshProUGUI m_Toast;
    float m_ToastRemaining;
    public int ImpactCount { get; private set; }
    public int GatherCount { get; private set; }
    public int DepositCount { get; private set; }
    public int ResearchCount { get; private set; }
    public int DroppedCount { get; private set; }
    public int BadgeCount { get; private set; }
    public int ActivePulseCount { get; private set; }
    public string Notification => m_Toast != null ? m_Toast.text : "";
    public bool IsInitialized => m_Bridge != null;

    public void Initialize(SwarmGameBridge bridge, GameManager game, FogOfWarManager fog, Camera camera,
        IResearchService research, EnvironmentPresentationDefinition palette, Canvas canvas, SwarmInstancedRenderer native)
    {
        if (bridge == null || game == null || fog == null || camera == null || research == null || palette == null || canvas == null || native == null)
            throw new ArgumentException("Action feedback requires its scene context and HUD.");
        if (IsInitialized)
        {
            if (m_Bridge != bridge) throw new InvalidOperationException("Feedback belongs to another match.");
            return;
        }
        m_Bridge = bridge; m_Game = game; m_Fog = fog; m_Camera = camera; m_Research = research;
        m_Simulation = bridge.Simulation;
        m_Native = native;
        var shader = Resources.Load<Shader>("E6Feedback");
        if (shader == null || !shader.isSupported) throw new InvalidOperationException("E6 feedback shader is unavailable.");
        m_Material = new Material(shader) { name = "E6 feedback (owned)" };
        BuildSprites();
        m_Pulses = new Pulse[Mathf.Clamp(palette.TransientVfxBudget, 0, 128)];
        for (int i = 0; i < m_Pulses.Length; i++) m_Pulses[i] = new Pulse { View = NewView("Action pulse"), Duration = 0 };
        // shortcut: 128 on-screen cargo/fortification badges; raise only after profiling a larger army.
        m_Badges = new SpriteRenderer[128];
        for (int i = 0; i < m_Badges.Length; i++) m_Badges[i] = NewView("Cargo or fortification");
        var rect = new GameObject("Research completed", typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
        rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, -245); rect.sizeDelta = new Vector2(620, 64);
        var notificationCanvas = rect.gameObject.AddComponent<Canvas>();
        notificationCanvas.overrideSorting = true; notificationCanvas.sortingOrder = 3100;
        m_Toast = rect.gameObject.AddComponent<TextMeshProUGUI>();
        m_Toast.font = TMP_Settings.defaultFontAsset; m_Toast.fontSize = 21; m_Toast.alignment = TextAlignmentOptions.Center;
        m_Toast.text = ""; m_Toast.color = new Color(.6f, 1, .85f); m_Toast.raycastTarget = false; m_Toast.gameObject.SetActive(false);
        bridge.Simulation.ClearPresentationImpacts(); bridge.Simulation.CapturePresentationImpacts = true;
        bridge.PresentationEvent += OnEvent;
    }

    SpriteRenderer NewView(string name)
    {
        var view = new GameObject(name).AddComponent<SpriteRenderer>();
        view.transform.SetParent(transform, false); view.sharedMaterial = m_Material;
        view.sortingOrder = 99; view.enabled = false; return view;
    }

    void BuildSprites()
    {
        m_Texture = new Texture2D(80, 16, TextureFormat.RGBA32, false) { name = "E6 pixel glyphs (owned)", filterMode = FilterMode.Point };
        var pixels = new Color[80 * 16];
        for (int shape = 0; shape < 5; shape++)
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
        {
            float dx = x - 7.5f, dy = y - 7.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
            bool on = shape == 0 ? (Mathf.Abs(dx - dy) < 1.1f || Mathf.Abs(dx + dy) < 1.1f) && r < 7
                : shape == 1 ? (x / 3 + y / 3) % 3 == 0 && r < 6
                : shape == 2 ? r > 5 && r < 7
                : shape == 3 ? Mathf.Abs(dx) < 6 && Mathf.Abs(dy) < 6 && (Mathf.Abs(dx) > 4 || Mathf.Abs(dy) > 4 || Mathf.Abs(dx - dy) < 1)
                : Mathf.Abs(dy + Mathf.Abs(dx) - 3) < 1.5f || (Mathf.Abs(dx) > 5 && Mathf.Abs(dy) < 5);
            pixels[y * 80 + shape * 16 + x] = on ? Color.white : Color.clear;
        }
        m_Texture.SetPixels(pixels); m_Texture.Apply(false, true);
        Sprite Make(int i) => Sprite.Create(m_Texture, new Rect(i * 16, 0, 16, 16), new Vector2(.5f, .5f), 16);
        m_Impact = Make(0); m_Dust = Make(1); m_Ring = Make(2); m_Crate = Make(3); m_Fortify = Make(4);
    }

    bool Visible(Vector3 position)
    {
        if (!m_Fog.IsInitialized) return false;
        if (!m_Fog.IsDisabled && (!m_Fog.TryGetCellState(m_Fog.FogTilemap.WorldToCell(position), out var state, out _) || state != FogCellState.Visible)) return false;
        var point = m_Camera.WorldToViewportPoint(position);
        return point.z > 0 && point.x >= 0 && point.x <= 1 && point.y >= 0 && point.y <= 1;
    }

    void Emit(Vector3 position, Sprite sprite, Color tint, float size, float duration)
    {
        if (!Visible(position)) return;
        foreach (var pulse in m_Pulses)
        {
            if (pulse.Duration > 0) continue;
            pulse.Position = position; pulse.View.sprite = sprite; pulse.Tint = tint;
            pulse.Size = size; pulse.Duration = duration; pulse.Age = 0; return;
        }
        DroppedCount++;
    }

    void OnEvent(GameplayEvent value)
    {
        var position = new Vector3(value.Position.x, value.Position.y, 0);
        switch (value.Kind)
        {
            case GameplayEventKind.Gather:
                GatherCount++; Emit(position, m_Dust, new Color(.85f, .7f, .43f), .4f, .32f); break;
            case GameplayEventKind.Deposit:
                DepositCount++; Emit(position, m_Ring, new Color(1, .85f, .2f), .7f, .6f); break;
            case GameplayEventKind.ResearchComplete:
                ResearchCount++;
                var id = m_Bridge.Gameplay.GetTechnologyId(value.RequestId);
                m_Toast.text = ResearchMessage(id); m_ToastRemaining = 5; m_Toast.gameObject.SetActive(true);
                Emit(position, m_Ring, m_Toast.color, 1.1f, .9f); break;
        }
    }

    static string ResearchMessage(string id) => id switch
    {
        "economy_tools" => "RESEARCH COMPLETE · Improved gathering tools",
        "economy_cargo" => "RESEARCH COMPLETE · Larger carrying loads",
        "military_archery" => "UNLOCKED · Archery Range and Archers",
        "military_training" => "RESEARCH COMPLETE · Faster training for new orders",
        "expansion_tower" => "UNLOCKED · Outposts / defensive towers",
        "expansion_fortify" => "RESEARCH COMPLETE · Fortified tower shots",
        _ => "RESEARCH COMPLETE"
    };

    void LateUpdate() => RefreshPresentation(Time.deltaTime);

    public void RefreshPresentation(float deltaTime)
    {
        if (!IsInitialized || !m_Bridge.IsSceneBound) return;
        m_Material.SetTexture("_FeedbackFog", m_Native.FogTexture != null ? m_Native.FogTexture : Texture2D.whiteTexture);
        m_Material.SetVector("_FeedbackFogBounds", m_Native.FogBounds);
        m_Material.SetFloat("_FeedbackFogEnabled", m_Native.FogEnabled ? 1 : 0);
        while (m_Bridge.Simulation.TryTakePresentationImpact(out var impact))
        {
            ImpactCount++;
            Emit(new Vector3(impact.Position.x, impact.Position.y), m_Impact,
                impact.Faction == 0 ? new Color(.45f, .85f, 1) : new Color(1, .4f, .22f), .32f, .23f);
        }
        float dt = Time.timeScale <= 0 || m_Bridge.Simulation.Paused || m_Bridge.Gameplay.IsMatchOver ? 0 : Mathf.Max(0, deltaTime);
        ActivePulseCount = 0;
        foreach (var pulse in m_Pulses)
        {
            pulse.Age += dt;
            if (pulse.Age >= pulse.Duration) pulse.Duration = 0;
            bool active = pulse.Duration > 0 && Visible(pulse.Position);
            pulse.View.enabled = active;
            if (!active) continue;
            ActivePulseCount++;
            float t = pulse.Age / pulse.Duration;
            pulse.View.transform.position = pulse.Position + Vector3.up * (.18f * t);
            pulse.View.transform.localScale = Vector3.one * pulse.Size * (1 + .5f * t);
            var color = Color.Lerp(Color.white, pulse.Tint, Mathf.Min(1, t * 5)); color.a = 1 - t;
            pulse.View.color = color;
        }
        m_ToastRemaining = Mathf.Max(0, m_ToastRemaining - dt); m_Toast.gameObject.SetActive(m_ToastRemaining > 0);
        BadgeCount = 0;
        foreach (var unit in m_Game.GetFriendlyUnits(true)) Badge(unit);
        var structures = m_Game.GetPlayerStructures();
        for (int i = 0; i < structures.Count; i++) Badge(structures[i]);
        for (int i = BadgeCount; i < m_Badges.Length; i++) m_Badges[i].enabled = false;
    }

    void Badge(Unit unit)
    {
        if (unit == null || !unit.isActiveAndEnabled || !unit.IsDotsControlled || BadgeCount >= m_Badges.Length
            || !m_Bridge.Gameplay.TryGetActor(unit.DotsEntity, out var actor, out var body) || body.Health <= 0 || body.Targetable == 0) return;
        bool cargo = m_Bridge.Gameplay.TryGetWorker(unit.DotsEntity, out var worker) && worker.Carried > 0;
        bool fortify = actor.Kind == GameplayKind.Tower && m_Research.IsUnlocked("expansion_fortify");
        if (!cargo && !fortify || !Visible(unit.transform.position)) return;
        var view = m_Badges[BadgeCount++]; view.enabled = true; view.sprite = cargo ? m_Crate : m_Fortify;
        view.transform.position = unit.transform.position + new Vector3(cargo ? .38f : 0, cargo ? -.27f : .5f, 0);
        view.transform.localScale = Vector3.one * (cargo ? (m_Research.IsUnlocked("economy_cargo") ? .32f : .23f) : .42f);
        view.color = cargo ? (worker.ResourceType == 0 ? new Color(.83f, .55f, .25f) : worker.ResourceType == 1 ? Color.yellow : new Color(1, .5f, .45f)) : new Color(1, .82f, .35f);
    }

    void OnDestroy()
    {
        if (m_Bridge != null)
        {
            m_Bridge.PresentationEvent -= OnEvent;
        }
        if (m_Simulation != null && m_Simulation.World != null && m_Simulation.World.IsCreated)
        { m_Simulation.CapturePresentationImpacts = false; m_Simulation.ClearPresentationImpacts(); }
        if (m_Pulses != null) foreach (var pulse in m_Pulses) if (pulse.View != null) Destroy(pulse.View.gameObject);
        if (m_Badges != null) foreach (var badge in m_Badges) if (badge != null) Destroy(badge.gameObject);
        if (m_Toast != null) Destroy(m_Toast.gameObject);
        foreach (var sprite in new[] { m_Impact, m_Dust, m_Ring, m_Crate, m_Fortify }) if (sprite != null) Destroy(sprite);
        if (m_Texture != null) Destroy(m_Texture);
        if (m_Material != null) Destroy(m_Material);
    }
}
