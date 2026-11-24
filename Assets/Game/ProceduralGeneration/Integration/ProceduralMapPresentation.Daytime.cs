using System;
using System.Collections.Generic;
using RTS.ProceduralGeneration;
using UnityEngine;

public sealed partial class ProceduralMapPresentation
{
    [Header("E4 presentation only")]
    [SerializeField] bool m_EnableDaytimeDetail = true;
    [SerializeField] int m_PresentationSeed = 314159;
    public const int DaytimeDetailBudget = 220;
    public const int AmbientBudget = 12;
    readonly HashSet<SpriteRenderer> m_DaytimeDetails = new();
    readonly HashSet<int> m_DetailCells = new();
    readonly List<AmbientDetail> m_Ambient = new();
    readonly List<Vector2Int> m_ClusterCenters = new();
    Texture2D m_DetailAtlas;
    DayNightCycleManager m_DetailClock;
    Sprite[] m_DetailSprites;
    public int DaytimeDetailCount => m_DaytimeDetails.Count;
    public int AmbientDetailCount => m_Ambient.Count;
    public int PresentationSeed => m_PresentationSeed;

    sealed class AmbientDetail
    {
        public SpriteRenderer Renderer;
        public Vector3 Home;
        public float Offset;
        public bool Butterfly, Visible;
    }

    public void InitializeDaytimeClock(DayNightCycleManager clock)
    {
        if (clock == null) throw new ArgumentNullException(nameof(clock));
        if (m_DetailClock != null && m_DetailClock != clock) throw new InvalidOperationException("Decor already has another clock.");
        m_DetailClock = clock;
    }

    float PatchNoise(int x, int y)
    {
        int seed = DeterministicRandom.DeriveSeed(m_Map.Seed ^ m_PresentationSeed, 0x45344445, 0);
        float Sample(int px, int py) => new DeterministicRandom(DeterministicRandom.DeriveSeed(seed, unchecked((uint)px), py)).NextFloat();
        int cx = x / 5, cy = y / 5;
        float tx = Mathf.SmoothStep(0, 1, x % 5 / 5f), ty = Mathf.SmoothStep(0, 1, y % 5 / 5f);
        return Mathf.Lerp(Mathf.Lerp(Sample(cx, cy), Sample(cx + 1, cy), tx), Mathf.Lerp(Sample(cx, cy + 1), Sample(cx + 1, cy + 1), tx), ty);
    }

    Color VaryGround(int x, int y, Color color)
    {
        int index = m_Map.Index(x, y);
        if (!m_EnableDaytimeDetail || m_Map.Reserved[index] != 0 || m_Map.Layout.Routes[index] != 0) return color;
        int region = m_Map.Layout.RegionIds[index];
        float patch = PatchNoise(x, y);
        Color shade = region == 2 ? new Color(.79f, .66f, .43f) : new Color(.73f, .88f, .66f);
        return Color.Lerp(color, color * shade, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.3f, .8f, patch)) * (region == 0 ? .08f : .24f));
    }

    void InitializeDaytimeDetails(Sprite bush, Sprite rock)
    {
        if (!m_EnableDaytimeDetail) return;
        MakeDetailAtlas();
        foreach (var decor in m_Decor)
        {
            var cell = m_Ground.WorldToCell(decor.transform.position) - m_Origin;
            m_DetailCells.Add(m_Map.Index(cell.x, cell.y));
        }
        var random = new DeterministicRandom(m_Map.Seed ^ m_PresentationSeed, 0x45344445);
        for (int region = 0; region < m_Map.Layout.Regions.Length; region++)
        {
            var area = m_Map.Layout.Regions[region];
            int wanted = region == 0 ? 5 : 16, clusters = 0, radius = area.Radius + 13;
            for (int attempt = 0; attempt < 1200 && clusters < wanted && DaytimeDetailCount < DaytimeDetailBudget - AmbientBudget; attempt++)
            {
                int x = area.X + random.NextInt(-radius, radius + 1), y = area.Y + random.NextInt(-radius, radius + 1);
                if (!CanPlaceDetail(x, y) || m_Map.Layout.RegionIds[m_Map.Index(x, y)] != region) continue;
                var center = new Vector2Int(x, y);
                if (m_ClusterCenters.Exists(c => (c - center).sqrMagnitude < 25)) continue;
                m_ClusterCenters.Add(center); clusters++;
                int count = random.NextInt(3, 7);
                for (int member = 0, tries = 0; member < count && tries < 24 && DaytimeDetailCount < DaytimeDetailBudget - AmbientBudget; tries++)
                {
                    int px = x + random.NextInt(-2, 3), py = y + random.NextInt(-2, 3);
                    if (!CanPlaceDetail(px, py) || m_Map.Layout.RegionIds[m_Map.Index(px, py)] != region) continue;
                    int kind = random.NextInt(0, 100);
                    Sprite sprite; string name;
                    if (region == 2)
                    {
                        sprite = kind < 60 ? m_DetailSprites[2] : kind < 85 ? m_DetailSprites[3] : rock;
                        name = kind < 60 ? "Dry earth" : "Frontier pebbles";
                    }
                    else
                    {
                        sprite = kind < 45 ? m_DetailSprites[0] : kind < 85 ? m_DetailSprites[1] : region == 1 ? bush : m_DetailSprites[0];
                        name = kind < 45 ? "Meadow grass" : kind < 85 ? "Meadow flowers" : "Grove shrubs";
                    }
                    var renderer = AddDetail(sprite, px, py, "E4 " + name);
                    float scale = sprite == bush ? .65f : random.NextFloat() * .35f + .85f;
                    renderer.transform.localScale = Vector3.one * scale; renderer.flipX = random.NextInt(0, 2) == 0;
                    renderer.sortingOrder = sprite == bush || sprite == rock ? 12 : 11;
                    member++;
                    if (region == 1 && sprite == m_DetailSprites[1] && m_Ambient.Count < 4
                        && !m_Ambient.Exists(a => (a.Home - renderer.transform.position).sqrMagnitude < 36))
                        AddAmbient(m_DetailSprites[5], px, py, true, random.NextFloat() * 6.28f);
                }
            }
        }
        // Inland water near a shore, never the foam edge or any protected gameplay cell.
        for (int attempt = 0; attempt < 5000 && m_Ambient.Count < AmbientBudget && DaytimeDetailCount < DaytimeDetailBudget; attempt++)
        {
            int x = random.NextInt(2, m_Map.Width - 2), y = random.NextInt(2, m_Map.Height - 2);
            if (!CanPlaceRipple(x, y)) continue;
            AddAmbient(m_DetailSprites[4], x, y, false, random.NextFloat() * 6.28f);
        }
    }

    bool CanPlaceDetail(int x, int y)
    {
        if (!CanDecorate(x, y) || m_DetailCells.Contains(m_Map.Index(x, y))) return false;
        for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
        {
            int px = x + dx, py = y + dy;
            if (!m_Map.IsGrass(px, py)) return false;
            int i = m_Map.Index(px, py);
            if (m_Map.Reserved[i] != 0 || m_Map.Layout.Routes[i] != 0) return false;
        }
        return true;
    }

    bool CanPlaceRipple(int x, int y)
    {
        if (m_DetailCells.Contains(m_Map.Index(x, y))) return false;
        for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
        {
            int i = m_Map.Index(x + dx, y + dy);
            if (m_Map.Terrain[i] != (byte)TerrainCell.Water || m_Map.Reserved[i] != 0 || m_Map.Layout.Routes[i] != 0) return false;
        }
        foreach (var a in m_Ambient) if ((a.Home - CellWorld(x, y)).sqrMagnitude < 25) return false;
        return m_Map.IsGrass(x - 3, y) || m_Map.IsGrass(x + 3, y) || m_Map.IsGrass(x, y - 3) || m_Map.IsGrass(x, y + 3);
    }

    SpriteRenderer AddDetail(Sprite sprite, int x, int y, string name)
    {
        var renderer = AddDecor(sprite, x, y, name);
        m_DaytimeDetails.Add(renderer); m_DetailCells.Add(m_Map.Index(x, y)); return renderer;
    }

    void AddAmbient(Sprite sprite, int x, int y, bool butterfly, float offset)
    {
        // A butterfly shares its flower's cleared cell; it never changes resource occupancy.
        var renderer = AddDetail(sprite, x, y, butterfly ? "E4 Meadow butterfly" : "E4 Water ripple");
        renderer.sortingOrder = butterfly ? 13 : 6;
        m_Ambient.Add(new AmbientDetail { Renderer = renderer, Home = renderer.transform.position, Butterfly = butterfly, Offset = offset });
    }

    bool IsFootprintKnown(Vector3 position, bool visibleOnly)
    {
        if (!m_Fog.IsInitialized) return false;
        if (m_Fog.IsDisabled) return true;
        var cell = m_Ground.WorldToCell(position);
        for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
            if (!m_Fog.TryGetCellState(cell + new Vector3Int(x, y, 0), out var state, out _)
                || (visibleOnly ? state != FogCellState.Visible : state == FogCellState.Unexplored)) return false;
        return true;
    }

    void RefreshAmbientVisibility()
    {
        foreach (var a in m_Ambient) a.Visible = IsFootprintKnown(a.Home, true);
    }

    void LateUpdate() => UpdateAmbient(Time.time);

    void UpdateAmbient(float time)
    {
        foreach (var a in m_Ambient)
        {
            bool enabled = a.Visible && (!a.Butterfly || m_DetailClock.CurrentPhase != DayNightPhase.Night);
            a.Renderer.enabled = enabled;
            if (!enabled) continue;
            float t = time + a.Offset;
            if (a.Butterfly)
            {
                a.Renderer.transform.position = a.Home + new Vector3(Mathf.Sin(t * .8f) * .16f, .18f + Mathf.Sin(t * 1.4f) * .1f);
                a.Renderer.sprite = m_DetailSprites[5 + ((int)(t * 7) & 1)];
                a.Renderer.color = Color.white;
            }
            else
            {
                float pulse = Mathf.Sin(t * 1.4f) * .5f + .5f;
                a.Renderer.transform.localScale = new Vector3(.8f + pulse * .5f, .8f + pulse * .2f, 1);
                a.Renderer.color = new Color(1, 1, 1, .18f + pulse * .35f);
            }
        }
    }

    void MakeDetailAtlas()
    {
        m_DetailAtlas = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = "E4 pixel motifs", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[64 * 64];
        void Dot(int x, int y, Color32 c) { pixels[y * 64 + x] = c; }
        Color32 green = new(100, 139, 59, 255), light = new(168, 178, 84, 255), earth = new(131, 111, 70, 150);
        for (int i = 0; i < 5; i++)
        {
            int x = 3 + i * 6, y = 2 + i % 3 * 3;
            Dot(x, y, green); Dot(x + 1, y + 1, light); Dot(x + 1, y + 2, green); Dot(x + 2, y, green);
            Dot(x + 32, y, green); Dot(x + 32, y + 1, green);
            var petal = i % 2 == 0 ? new Color32(246, 215, 127, 255) : new Color32(239, 204, 190, 255);
            Dot(x + 31, y + 3, petal); Dot(x + 33, y + 3, petal); Dot(x + 32, y + 2, petal); Dot(x + 32, y + 4, petal);
            Dot(x + 32, y + 3, new Color32(186, 140, 53, 255));
        }
        for (int y = 1; y < 15; y++) for (int x = 1; x < 31; x++)
            if (Mathf.Pow((x - 16) / 14f, 2) + Mathf.Pow((y - 8) / 6f, 2) < 1 && (x + y * 3) % 7 != 0)
                Dot(x, y + 16, (x * 3 + y) % 5 == 0 ? new Color32(170, 144, 89, 130) : earth);
        for (int i = 0; i < 7; i++)
        {
            int x = 34 + i * 4, y = 18 + i % 3 * 4;
            Dot(x, y, new Color32(88, 100, 88, 255)); Dot(x + 1, y, new Color32(165, 159, 126, 255)); Dot(x + 1, y + 1, new Color32(193, 184, 143, 255));
        }
        for (int i = 0; i < 3; i++) for (int x = 5 + i * 2; x < 26 - i * 3; x++)
            Dot(x, 35 + i * 4 + (x / 7 % 2), new Color32(168, 213, 205, 255));
        for (int frame = 0; frame < 2; frame++)
        {
            int cx = frame * 16 + 8;
            for (int side = -1; side <= 1; side += 2)
                for (int y = 53; y <= 57; y++)
                {
                    int span = (y == 53 || y == 57) ? 1 : frame == 0 ? 4 : 2;
                    for (int x = 1; x <= span; x++) Dot(cx + x * side, y, x == span ? new Color32(181, 117, 75, 255) : new Color32(244, 208, 125, 255));
                }
            for (int y = 52; y <= 57; y++) Dot(cx, y, new Color32(70, 64, 58, 255));
            Dot(cx - 1, 58, new Color32(70, 64, 58, 255)); Dot(cx + 1, 58, new Color32(70, 64, 58, 255));
        }
        m_DetailAtlas.SetPixels32(pixels); m_DetailAtlas.Apply(false, true);
        Rect[] frames = { new(0, 0, 32, 16), new(32, 0, 32, 16), new(0, 16, 32, 16), new(32, 16, 32, 16), new(0, 32, 32, 16), new(0, 48, 16, 16), new(16, 48, 16, 16) };
        m_DetailSprites = new Sprite[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            var sprite = Sprite.Create(m_DetailAtlas, frames[i], new Vector2(.5f, .5f), 32, 0, SpriteMeshType.FullRect);
            sprite.name = "E4 motif " + i; m_Sprites.Add(sprite); m_DetailSprites[i] = sprite;
        }
    }
}
