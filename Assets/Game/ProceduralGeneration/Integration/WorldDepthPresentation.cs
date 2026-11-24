using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// All offsets are render data. Actor transforms, pivots, colliders and ECS state are never moved.
[DefaultExecutionOrder(150)]
public sealed class WorldDepthPresentation : MonoBehaviour
{
    sealed class Body
    {
        public SpriteRenderer Renderer;
        public Unit Unit;
        public ResourceNodeBase Resource;
        public Material OriginalMaterial;
        public int OriginalOrder;
        public bool TallDecor;
        public Collider2D GroundCollider;
        public readonly MaterialPropertyBlock Original = new(), Properties = new();
    }
    readonly List<Body> m_Bodies = new();
    readonly HashSet<SpriteRenderer> m_Seen = new();
    readonly List<ResourceNodeBase> m_Resources = new();
    readonly List<Unit> m_ResourceUnits = new();
    readonly Matrix4x4[] m_Matrices = new Matrix4x4[1023];
    readonly Vector3[] m_Vertices = new Vector3[4092];
    readonly Vector2[] m_Uvs = new Vector2[4092];
    readonly int[] m_Indices = new int[6138];
    MaterialPropertyBlock m_Draw;
    GameManager m_Game;
    DayNightCycleManager m_Clock;
    FogOfWarManager m_Fog;
    Camera m_Camera;
    RTS.DOTS.SwarmInstancedRenderer m_Native;
    Transform m_ResourceRoot;
    IReadOnlyList<SpriteRenderer> m_Scenery;
    EnvironmentPresentationDefinition m_Definition;
    Material m_BodyMaterial, m_ShadowMaterial, m_RingMaterial;
    Texture2D m_Memory;
    Color32[] m_FogPixels;
    Mesh m_ShadowMesh, m_RingMesh;
    MeshRenderer m_ShadowView, m_RingView;
    float m_NextScan;
    public bool IsInitialized => m_Game != null;
    public int RegisteredBodies => m_Bodies.Count;
    public int FadedOccluders { get; private set; }
    public int ContactCount { get; private set; }
    public int SelectionRingCount { get; private set; }
    public float ShadowStretch => m_Clock.CurrentPhase == DayNightPhase.Dusk
        ? 1 + .8f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(m_Clock.CurrentPhaseProgress)) : 1;

    public void Initialize(GameManager game, DayNightCycleManager clock, FogOfWarManager fog, Camera camera,
        EnvironmentPresentationDefinition definition, RTS.DOTS.SwarmInstancedRenderer native, Transform resourceRoot, IReadOnlyList<SpriteRenderer> scenery)
    {
        if (game == null || clock == null || fog == null || camera == null || native == null || definition == null
            || definition.FootDepthMaterial == null || definition.ContactShadowMaterial == null || !camera.orthographic
            || resourceRoot == null || scenery == null || resourceRoot.gameObject.scene != gameObject.scene
            || game.gameObject.scene != gameObject.scene || fog.gameObject.scene != gameObject.scene)
            throw new ArgumentException("World depth requires its scene game/clock/fog/camera/native and retained presentation materials.");
        if (IsInitialized)
        {
            if (m_Game != game || m_Clock != clock || m_Fog != fog || m_Camera != camera || m_Native != native || m_Definition != definition || m_ResourceRoot != resourceRoot || m_Scenery != scenery)
                throw new InvalidOperationException("World depth already has another scene context.");
            return;
        }
        m_Game = game; m_Clock = clock; m_Fog = fog; m_Camera = camera; m_Native = native;
        m_Draw = new MaterialPropertyBlock();
        m_ResourceRoot = resourceRoot; m_Definition = definition; m_Scenery = scenery;
        m_BodyMaterial = new Material(definition.FootDepthMaterial) { name = "E5 shared foot depth" };
        m_ShadowMaterial = new Material(definition.ContactShadowMaterial) { name = "E5 shared contacts", enableInstancing = true };
        m_RingMaterial = new Material(definition.ContactShadowMaterial) { name = "E5 selection rings", renderQueue = 3005, enableInstancing = true };
        m_RingMaterial.SetColor("_ContactColor", new Color(.35f,1,.55f,.95f)); m_RingMaterial.SetFloat("_ContactRing",1);
        m_RingMaterial.SetFloat("_ContactZTest",8);
        for(int i=0;i<1023;i++)
        {
            int v=i*4,j=i*6;m_Uvs[v]=Vector2.zero;m_Uvs[v+1]=Vector2.right;m_Uvs[v+2]=Vector2.up;m_Uvs[v+3]=Vector2.one;
            m_Indices[j]=v;m_Indices[j+1]=v+2;m_Indices[j+2]=v+1;m_Indices[j+3]=v+2;m_Indices[j+4]=v+3;m_Indices[j+5]=v+1;
        }
        m_ShadowView=CreateBatch("E5 contact batch",m_ShadowMaterial,20,out m_ShadowMesh);
        m_RingView=CreateBatch("E5 selection batch",m_RingMaterial,100,out m_RingMesh);
        native.SetDepthPresentation(definition.ContactShadowMaterial);
        fog.OnFogUpdated += RefreshFog;
        RefreshFog();
    }

    void RefreshFog()
    {
        if (!m_Fog.IsInitialized) return;
        var bounds = m_Fog.Bounds;
        if (m_Memory == null)
        {
            m_Memory = new Texture2D(bounds.size.x, bounds.size.y, TextureFormat.RGBA32, false)
                { name = "E5 explored visibility", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            m_FogPixels = new Color32[bounds.size.x * bounds.size.y];
        }
        for (int y = 0; y < bounds.size.y; y++) for (int x = 0; x < bounds.size.x; x++)
        {
            m_Fog.TryGetCellState(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0), out var state, out _);
            byte value = state == FogCellState.Visible ? (byte)255 : state == FogCellState.Explored ? (byte)115 : (byte)0;
            m_FogPixels[y * bounds.size.x + x] = new Color32(value, value, value, 255);
        }
        m_Memory.SetPixels32(m_FogPixels); m_Memory.Apply(false, false);
        var min = m_Fog.FogTilemap.CellToWorld(bounds.min); var max = m_Fog.FogTilemap.CellToWorld(bounds.max);
        m_BodyMaterial.SetTexture("_DepthFog", m_Memory);
        m_BodyMaterial.SetVector("_DepthFogBounds", new Vector4(min.x, min.y, max.x - min.x, max.y - min.y));
        m_BodyMaterial.SetFloat("_DepthFogEnabled", m_Fog.IsDisabled ? 0 : 1);
    }

    void Add(SpriteRenderer renderer, Unit unit, ResourceNodeBase resource, bool tallDecor = false)
    {
        if (renderer == null || !m_Seen.Add(renderer)) return;
        foreach (var body in m_Bodies) if (body.Renderer == renderer) return;
        var groundCollider=renderer.GetComponent<CapsuleCollider2D>() as Collider2D;
        if(groundCollider==null)foreach(var candidate in renderer.GetComponents<Collider2D>())if(!candidate.isTrigger){groundCollider=candidate;break;}
        var b = new Body { Renderer = renderer, Unit = unit, Resource = resource, TallDecor = tallDecor, GroundCollider=groundCollider, OriginalMaterial = renderer.sharedMaterial, OriginalOrder = renderer.sortingOrder };
        renderer.GetPropertyBlock(b.Original); m_Bodies.Add(b);
    }

    void Scan()
    {
        m_Seen.Clear();
        foreach (bool player in new[] { true, false }) foreach (var unit in m_Game.GetFriendlyUnits(player))
            if (unit != null && unit.IsSceneInitialized && unit.isActiveAndEnabled) Add(unit.SpriteRenderer, unit, null);
        foreach (var structure in m_Game.GetPlayerStructures())
            if (structure != null && structure.IsSceneInitialized && structure.isActiveAndEnabled) Add(structure.SpriteRenderer, structure, null);
        if (m_ResourceRoot != null)
        {
            m_Resources.Clear(); m_ResourceRoot.GetComponentsInChildren(false, m_Resources);
            foreach (var resource in m_Resources) if (resource.IsInitialized) Add(resource.GetComponent<SpriteRenderer>(), null, resource);
            m_ResourceUnits.Clear(); m_ResourceRoot.GetComponentsInChildren(false, m_ResourceUnits);
            foreach (var unit in m_ResourceUnits) if (unit.IsSceneInitialized && unit.isActiveAndEnabled) Add(unit.SpriteRenderer, unit, null);
        }
        foreach (var renderer in m_Scenery) if (renderer != null && renderer.name == "Old Watchpost") Add(renderer, null, null, true);
        for (int i = m_Bodies.Count - 1; i >= 0; i--)
            if (m_Bodies[i].Renderer == null || !m_Seen.Contains(m_Bodies[i].Renderer)) { Restore(m_Bodies[i]); m_Bodies.RemoveAt(i); }
    }

    bool Visible(Vector3 position) => m_Fog.IsInitialized && (m_Fog.IsDisabled || m_Fog.TryGetCellState(m_Fog.FogTilemap.WorldToCell(position), out var state, out _) && state == FogCellState.Visible);

    static Vector3 Ground(SpriteRenderer renderer,Collider2D collider)
    {
        if(collider==null)return renderer.transform.position;
        float half=collider is CapsuleCollider2D capsule?capsule.size.y*.5f:collider is BoxCollider2D box?box.size.y*.5f:collider is CircleCollider2D circle?circle.radius:0;
        return collider.transform.TransformPoint(collider.offset-Vector2.up*half);
    }

    bool Occluded(Body body, float roofY)
    {
        var bounds = body.Renderer.bounds;
        bounds.Expand(new Vector3(-bounds.size.x*.25f,0,0));
        foreach (var unit in m_Game.SelectedUnits)
        {
            if (unit == null || unit.IsBuilding || !unit.IsSelectable || unit.SpriteRenderer == null || !unit.SpriteRenderer.enabled
                || unit.SpriteRenderer.color.a < .9f || !Visible(unit.transform.position)
                || Ground(unit.SpriteRenderer,unit.Collider).y <= Ground(body.Renderer,body.GroundCollider).y + .05f) continue;
            var selected = unit.Collider != null ? unit.Collider.bounds : unit.SpriteRenderer.bounds;
            if(unit.Collider!=null)selected.Expand(new Vector3(.15f,.6f,0));
            if (selected.max.x > bounds.min.x && selected.min.x < bounds.max.x && selected.max.y > roofY
                && selected.min.y < bounds.max.y && selected.max.y > bounds.min.y) return true;
        }
        return false;
    }

    void LateUpdate() => RefreshPresentation();
    public void RefreshPresentation()
    {
        if (!IsInitialized) return;
        if (Time.unscaledTime >= m_NextScan) { Scan(); m_NextScan = Time.unscaledTime + .25f; }
        FadedOccluders = 0; ContactCount = 0; SelectionRingCount = 0;
        m_Native.ContactStretch = ShadowStretch;
        int count = 0;
        foreach (var b in m_Bodies)
        {
            var r = b.Renderer; if (r == null || !r.gameObject.activeInHierarchy) continue;
            var foot = Ground(r,b.GroundCollider);
            float roof = Mathf.Max(foot.y + .2f, r.bounds.min.y + r.bounds.size.y * .43f);
            bool canFade = b.Resource is Tree || b.Unit is StructureUnit || b.TallDecor;
            bool fade = r.enabled && r.color.a > .9f && Visible(foot) && canFade && Occluded(b, roof);
            if (fade) FadedOccluders++;
            r.sharedMaterial = m_BodyMaterial; r.sortingOrder = 25;
            r.GetPropertyBlock(b.Properties);
            b.Properties.SetFloat("_DepthFootY", foot.y); b.Properties.SetFloat("_DepthRoofY", roof);
            b.Properties.SetFloat("_DepthFade", fade ? 1 : 0);
            b.Properties.SetFloat("_DepthMobile", b.Unit != null && !b.Unit.IsBuilding ? 1 : 0); r.SetPropertyBlock(b.Properties);
            if (!r.enabled || r.color.a < .9f || !Visible(foot) || b.Unit != null && b.Unit.CurrentState == UnitState.Dead) continue;
            float vertical = m_Camera.orthographicSize + 4, horizontal = vertical * m_Camera.aspect;
            if (Mathf.Abs(foot.x - m_Camera.transform.position.x) > horizontal || Mathf.Abs(foot.y - m_Camera.transform.position.y) > vertical) continue;
            if (ContactCount >= 512) continue;
            float width = Mathf.Clamp(r.bounds.size.x * .55f, .35f, b.Unit is StructureUnit ? 3 : 1.5f);
            m_Matrices[count] = Matrix4x4.TRS(foot + new Vector3((ShadowStretch - 1) * .12f, 0, -.8f), Quaternion.Euler(0,0,-20), new Vector3(width * ShadowStretch, width * .32f, 1));
            count++; ContactCount++;
        }
        Draw(m_ShadowMesh,m_ShadowView,count);
        count = 0;
        foreach (var unit in m_Game.SelectedUnits)
        {
            if (unit == null || !unit.IsSelectable || unit.SpriteRenderer == null || !unit.SpriteRenderer.enabled || !Visible(unit.transform.position)) continue;
            if (count == 1023) break;
            float width = Mathf.Clamp(unit.Collider != null ? unit.Collider.bounds.size.x * 1.6f : unit.SpriteRenderer.bounds.size.x * .4f, .65f, 3);
            m_Matrices[count] = Matrix4x4.TRS(Ground(unit.SpriteRenderer,unit.Collider) + new Vector3(0,0,-2), Quaternion.identity, new Vector3(width,.28f,1));
            count++; SelectionRingCount++;
        }
        Draw(m_RingMesh,m_RingView,count);
    }

    MeshRenderer CreateBatch(string name,Material material,int order,out Mesh mesh)
    {
        var view=new GameObject(name);view.transform.SetParent(transform,true);
        mesh=new Mesh{name=name};mesh.MarkDynamic();view.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=view.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=order;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.enabled=false;return renderer;
    }
    void Draw(Mesh mesh,MeshRenderer renderer, int count)
    {
        renderer.enabled=count>0&&m_Fog.IsInitialized;if(!renderer.enabled)return;
        for(int i=0;i<count;i++)
        {
            var m=m_Matrices[i];int v=i*4;
            m_Vertices[v]=m.MultiplyPoint3x4(new Vector3(-.5f,-.5f));m_Vertices[v+1]=m.MultiplyPoint3x4(new Vector3(.5f,-.5f));
            m_Vertices[v+2]=m.MultiplyPoint3x4(new Vector3(-.5f,.5f));m_Vertices[v+3]=m.MultiplyPoint3x4(new Vector3(.5f,.5f));
        }
        mesh.Clear();mesh.SetVertices(m_Vertices,0,count*4);mesh.SetUVs(0,m_Uvs,0,count*4);mesh.SetTriangles(m_Indices,0,count*6,0);mesh.RecalculateBounds();
        m_Draw.SetTexture("_ContactFog", m_Native.FogTexture != null ? m_Native.FogTexture : Texture2D.blackTexture);
        m_Draw.SetVector("_ContactFogBounds", m_Native.FogBounds); m_Draw.SetFloat("_ContactFogEnabled", m_Fog.IsDisabled ? 0 : 1);
        renderer.SetPropertyBlock(m_Draw);
    }

    static void Restore(Body body)
    {
        if (body.Renderer == null) return;
        body.Renderer.sharedMaterial = body.OriginalMaterial; body.Renderer.sortingOrder = body.OriginalOrder;
        body.Renderer.SetPropertyBlock(body.Original);
    }
    void OnDestroy()
    {
        if (m_Fog != null) m_Fog.OnFogUpdated -= RefreshFog;
        foreach (var body in m_Bodies) Restore(body);
        if (m_Native != null) m_Native.SetDepthPresentation(null);
        if(m_ShadowView!=null)Destroy(m_ShadowView.gameObject);if(m_RingView!=null)Destroy(m_RingView.gameObject);
        foreach (var owned in new UnityEngine.Object[] { m_BodyMaterial,m_ShadowMaterial,m_RingMaterial,m_ShadowMesh,m_RingMesh,m_Memory }) if (owned != null) Destroy(owned);
    }
}
