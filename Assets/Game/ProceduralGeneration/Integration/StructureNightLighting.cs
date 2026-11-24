using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

// Presentation only. Completed player buildings are read from the injected scene registry.
[DisallowMultipleComponent, DefaultExecutionOrder(50)]
public sealed class StructureNightLighting : MonoBehaviour
{
    static readonly ProfilerMarker UpdateMarker = new("RTS.StructureNightLighting");
    sealed class Emitter
    {
        public StructureUnit Owner;
        public GameObject Group;
        public Light2D Light;
        public SpriteRenderer[] Glows;
        public float Phase;
    }
    readonly List<Emitter> m_Emitters = new();
    readonly HashSet<StructureUnit> m_Registered = new();
    GameManager m_Game;
    DayNightCycleManager m_Clock;
    FogOfWarManager m_Fog;
    TilemapManager m_Map;
    Camera m_Camera;
    Material m_RuntimeMaterial;
    RTS.DOTS.SwarmInstancedRenderer m_FogSource;
    EnvironmentPresentationDefinition m_Definition;
    Texture2D m_GlowTexture, m_CoreTexture;
    Sprite m_GlowSprite, m_CoreSprite;
    float m_NextRegistryRefresh;
    public bool IsInitialized => m_Game != null;
    public int RegisteredEmitters => m_Emitters.Count;
    public int ActiveLocalLights { get; private set; }
    public int ActiveGlowGroups { get; private set; }
    public float Strength => IsInitialized ? EvaluateStrength(m_Clock.CurrentPhase, m_Clock.CurrentPhaseProgress,
        m_Definition.TransitionFraction) : 0;

    public void Initialize(GameManager game, DayNightCycleManager clock, FogOfWarManager fog, TilemapManager map,
        Camera camera, EnvironmentPresentationDefinition definition, RTS.DOTS.SwarmInstancedRenderer fogSource)
    {
        if (game == null || clock == null || fog == null || map == null || map.WalkableTilemap == null
            || map.gameObject.scene != gameObject.scene || camera == null || definition == null || definition.NightGlowMaterial == null
            || fogSource == null || fogSource.gameObject.scene != gameObject.scene
            || !camera.orthographic || game.gameObject.scene != gameObject.scene || clock.gameObject.scene != gameObject.scene
            || fog.gameObject.scene != gameObject.scene || camera.gameObject.scene != gameObject.scene || fog.FogTilemap == null)
            throw new ArgumentException("Night lighting requires its scene game/clock/fog/camera, palette and sprite material.");
        if (IsInitialized)
        {
            if (m_Game != game || m_Clock != clock || m_Fog != fog || m_Map != map || m_Camera != camera || m_Definition != definition || m_FogSource != fogSource)
                throw new InvalidOperationException("Night lighting already belongs to another scene context.");
            return;
        }
        definition.Validate();
        if (definition.NightGlowMaterial.shader == null || definition.NightGlowMaterial.shader.name != "RTS/Presentation/NightGlow")
            throw new ArgumentException("Night glow material must use the visibility-masked NightGlow shader.");
        m_Game = game; m_Clock = clock; m_Fog = fog; m_Map = map; m_Camera = camera; m_Definition = definition;
        m_FogSource = fogSource;
        m_RuntimeMaterial = new Material(definition.NightGlowMaterial) { name = "Scene night glow" };
        m_GlowTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "Shared night halo", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
        {
            float radius = new Vector2((x + .5f) / 16 - 1, (y + .5f) / 16 - 1).magnitude;
            float alpha = Mathf.Pow(Mathf.Clamp01(1 - radius), 2) * .5f;
            m_GlowTexture.SetPixel(x, y, new Color(1, 1, 1, alpha));
        }
        m_GlowTexture.Apply(false, true);
        m_CoreTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = "Shared lamp core", filterMode = FilterMode.Point };
        m_CoreTexture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); m_CoreTexture.Apply(false, true);
        m_GlowSprite = Sprite.Create(m_GlowTexture, new Rect(0, 0, 32, 32), Vector2.one * .5f, 32);
        m_CoreSprite = Sprite.Create(m_CoreTexture, new Rect(0, 0, 2, 2), Vector2.one * .5f, 2);
    }

    public static float EvaluateStrength(DayNightPhase phase, float progress, float transitionFraction)
    {
        progress = float.IsFinite(progress) ? Mathf.Clamp01(progress) : 0;
        if (phase == DayNightPhase.Day) return 0;
        if (phase == DayNightPhase.Dusk) return Mathf.SmoothStep(0, 1, progress);
        return 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1 - transitionFraction, 1, progress));
    }

    static bool Supported(StructureUnit unit) => unit is TownHall || unit is TowerUnit
        || unit.CanStoreGold || unit.CanStoreWood || unit.CanStoreMeat;
    static int Priority(StructureUnit unit) => unit is TownHall ? 0 : unit is TowerUnit ? 1 : 2;
    static bool Alive(StructureUnit unit) => unit != null && unit.isActiveAndEnabled && unit.IsPlayer
        && unit.CurrentState != UnitState.Dead && unit.CurrentHealth > 0 && !unit.isUnderConstruction;

    void LateUpdate() => RefreshPresentation();
    public void RefreshPresentation()
    {
        using var marker = UpdateMarker.Auto();
        if (!IsInitialized || !isActiveAndEnabled) return;
        // Reuse the authoritative visibility texture, masking each halo pixel even above building sorting layers.
        var fogMin = m_Fog.FogTilemap.CellToWorld(m_Fog.Bounds.min);
        var fogMax = m_Fog.FogTilemap.CellToWorld(m_Fog.Bounds.max);
        m_RuntimeMaterial.SetTexture("_NightFog", m_FogSource.FogTexture != null ? m_FogSource.FogTexture : Texture2D.blackTexture);
        m_RuntimeMaterial.SetVector("_NightFogBounds", new Vector4(fogMin.x, fogMin.y, fogMax.x - fogMin.x, fogMax.y - fogMin.y));
        m_RuntimeMaterial.SetFloat("_NightFogEnabled", m_Fog.IsDisabled ? 0 : 1);
        if (Time.unscaledTime >= m_NextRegistryRefresh)
        {
            SynchronizeRegistry();
            m_NextRegistryRefresh = Time.unscaledTime + .25f;
        }
        ActiveLocalLights = 0; ActiveGlowGroups = 0;
        float strength = Strength;
        var center = m_Camera.transform.position;
        float halfHeight = m_Camera.orthographicSize, halfWidth = halfHeight * m_Camera.aspect;
        foreach (var emitter in m_Emitters)
        {
            bool visible = strength > .001f && Alive(emitter.Owner) && emitter.Owner.SpriteRenderer != null
                && emitter.Owner.SpriteRenderer.enabled && emitter.Owner.SpriteRenderer.color.a > .01f;
            var position = emitter.Owner != null ? emitter.Owner.transform.position : Vector3.zero;
            visible &= Mathf.Abs(position.x - center.x) <= halfWidth + 3 && Mathf.Abs(position.y - center.y) <= halfHeight + 3;
            if (visible && !m_Fog.IsDisabled)
                visible = m_Fog.IsInitialized && m_Fog.TryGetCellState(m_Fog.FogTilemap.WorldToCell(position), out var state, out _)
                    && state == FogCellState.Visible;
            if (emitter.Group == null) continue;
            emitter.Group.SetActive(visible);
            if (!visible) { emitter.Light.enabled = false; continue; }
            ActiveGlowGroups++;
            // Time.time freezes flicker on pause; the offset uses position, never gameplay RNG.
            float flicker = 1 + .035f * Mathf.Sin(Time.time * 7.1f + emitter.Phase)
                + .015f * Mathf.Sin(Time.time * 13.3f + emitter.Phase * 1.7f);
            foreach (var glow in emitter.Glows)
            {
                var tint = m_Definition.WarmEmissive * (1.8f * flicker);
                tint.a = strength; glow.color = tint;
            }
            emitter.Light.color = m_Definition.WarmEmissive;
            emitter.Light.intensity = .45f * strength * flicker;
            emitter.Light.enabled = ActiveLocalLights < m_Definition.LocalLightBudget;
            if (emitter.Light.enabled) ActiveLocalLights++;
        }
    }

    void SynchronizeRegistry()
    {
        m_Registered.Clear();
        foreach (var structure in m_Game.GetPlayerStructures())
            if (Alive(structure) && Supported(structure)) m_Registered.Add(structure);
        for (int i = m_Emitters.Count - 1; i >= 0; i--)
            if (!m_Registered.Contains(m_Emitters[i].Owner) || m_Emitters[i].Group == null)
            {
                if (m_Emitters[i].Group != null) { m_Emitters[i].Group.SetActive(false); Destroy(m_Emitters[i].Group); }
                m_Emitters.RemoveAt(i);
            }
            else m_Registered.Remove(m_Emitters[i].Owner);
        foreach (var structure in m_Registered)
            if (structure.SpriteRenderer != null) CreateEmitter(structure);
        m_Emitters.Sort((a, b) => {
            int priority = Priority(a.Owner).CompareTo(Priority(b.Owner));
            return priority != 0 ? priority : a.Phase.CompareTo(b.Phase);
        });
    }

    void CreateEmitter(StructureUnit owner)
    {
        var source = owner.SpriteRenderer;
        var footCollider = owner.GetComponent<CapsuleCollider2D>() as Collider2D;
        if(footCollider==null)foreach(var candidate in owner.GetComponents<Collider2D>())if(!candidate.isTrigger){footCollider=candidate;break;}
        float half=footCollider is CapsuleCollider2D capsule?capsule.size.y*.5f:footCollider is BoxCollider2D box?box.size.y*.5f:0;
        float footY=footCollider!=null?footCollider.transform.TransformPoint(footCollider.offset-Vector2.up*half).y:owner.transform.position.y;
        var group = new GameObject("Night light presentation"); group.SetActive(false); group.transform.SetParent(owner.transform, false);
        var bounds = source.bounds;
        Vector3 Point(float x, float y) => new(bounds.min.x + bounds.size.x * x, bounds.min.y + bounds.size.y * y, source.transform.position.z);
        var glows = new List<SpriteRenderer>();
        SpriteRenderer Glow(string name, Vector3 position, Vector2 scale, bool core, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(group.transform, false); go.transform.position = position;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1);
            var sprite = go.AddComponent<SpriteRenderer>(); sprite.sprite = core ? m_CoreSprite : m_GlowSprite;
            sprite.sharedMaterial = m_RuntimeMaterial; sprite.sortingLayerID = source.sortingLayerID; sprite.sortingOrder = order;
            var depth = new MaterialPropertyBlock(); depth.SetFloat("_NightFootY", footY); sprite.SetPropertyBlock(depth);
            glows.Add(sprite); return sprite;
        }
        void Lamp(string name, Vector3 position)
        {
            Glow(name + " halo", position, Vector2.one * .7f, false, source.sortingOrder + 1);
            Glow(name + " core", position, new Vector2(.09f, .14f), true, source.sortingOrder + 1);
        }
        Glow("Ground warm pool", Point(.5f, .13f), new Vector2(4, 2), false, source.sortingOrder - 1);
        if (owner is TownHall) { Lamp("Hall window", Point(.5f, .35f)); Lamp("Entrance lamp", Point(.25f, .18f)); }
        else if (owner is TowerUnit) Lamp("Tower torch", Point(.72f, .70f));
        else Lamp("Depot lantern", Point(.23f, .30f));
        var lightObject = new GameObject("Ground Light2D"); lightObject.transform.SetParent(group.transform, false);
        lightObject.transform.position = Point(.5f, .13f);
        var light = lightObject.AddComponent<Light2D>(); light.enabled = false; light.lightType = Light2D.LightType.Point;
        light.targetSortingLayers = new[] { m_Map.WalkableTilemap.GetComponent<TilemapRenderer>().sortingLayerID };
        light.pointLightInnerRadius = .25f; light.pointLightOuterRadius = 2.5f; light.falloffIntensity = .65f;
        light.overlapOperation = Light2D.OverlapOperation.Additive;
        m_Emitters.Add(new Emitter { Owner = owner, Group = group, Light = light, Glows = glows.ToArray(),
            Phase = owner.transform.position.x * 1.731f + owner.transform.position.y * 2.417f });
    }

    void OnDisable()
    {
        foreach (var emitter in m_Emitters) if (emitter.Group != null) emitter.Group.SetActive(false);
        ActiveLocalLights = 0; ActiveGlowGroups = 0;
    }
    void OnDestroy()
    {
        OnDisable();
        foreach (var emitter in m_Emitters) if (emitter.Group != null) Destroy(emitter.Group);
        if (m_GlowSprite != null) Destroy(m_GlowSprite); if (m_CoreSprite != null) Destroy(m_CoreSprite);
        if (m_GlowTexture != null) Destroy(m_GlowTexture); if (m_CoreTexture != null) Destroy(m_CoreTexture);
        if (m_RuntimeMaterial != null) Destroy(m_RuntimeMaterial);
    }
}
