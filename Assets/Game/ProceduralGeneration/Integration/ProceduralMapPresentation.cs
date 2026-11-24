using System;
using System.Collections.Generic;
using RTS.ProceduralGeneration;
using UnityEngine;
using UnityEngine.Tilemaps;

// Visuals only: no Unit, collider or navigation data is added by this component.
public sealed partial class ProceduralMapPresentation : MonoBehaviour
{
    [SerializeField] Texture2D m_BushTexture;
    [SerializeField] Texture2D m_RockTexture;
    [SerializeField] Texture2D m_WatchpostTexture;
    [SerializeField] Material m_WorldMaterial;
    readonly List<Sprite> m_Sprites = new();
    readonly List<SpriteRenderer> m_Decor = new();
    MapData m_Map;
    Tilemap m_Ground;
    FogOfWarManager m_Fog;
    Vector3Int m_Origin;
    public bool IsInitialized => m_Map != null;
    public IReadOnlyList<SpriteRenderer> Decorations => m_Decor;
    public Vector3 LandmarkPosition => CellWorld(m_Map.Layout.Landmark.X, m_Map.Layout.Landmark.Y);

    public void Initialize(MapData map, TilemapManager tilemap, Vector3Int origin, FogOfWarManager fog)
    {
        if (map == null || tilemap == null || fog == null || tilemap.WalkableTilemap == null
            || tilemap.gameObject.scene != gameObject.scene || fog.gameObject.scene != gameObject.scene
            || m_BushTexture == null || m_RockTexture == null || m_WatchpostTexture == null || m_WorldMaterial == null
            || (m_EnableDaytimeDetail && m_DetailClock == null))
            throw new ArgumentException("Map presentation requires its map/fog and authored visual assets.");
        if (IsInitialized)
        {
            if (m_Map != map || m_Ground != tilemap.WalkableTilemap || m_Fog != fog || m_Origin != origin)
                throw new InvalidOperationException("Map presentation already has a different scene context.");
            return;
        }
        m_Map = map; m_Ground = tilemap.WalkableTilemap; m_Origin = origin; m_Fog = fog;
        if (map.Layout == null) return;
        PaintGround();
        // Bush art is an eight-frame strip; use one frame, not the entire strip as a billboard.
        var bush = MakeSprite(m_BushTexture, .5f, new Rect(0, 0, m_BushTexture.height, m_BushTexture.height));
        var rock = MakeSprite(m_RockTexture, .5f);
        var watchpost = MakeSprite(m_WatchpostTexture, .15f);
        for (int region = 1; region < map.Layout.Regions.Length; region++)
        {
            var area = map.Layout.Regions[region];
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI / 8;
                int x = area.X + Mathf.RoundToInt(Mathf.Cos(angle) * (area.Radius + 1));
                int y = area.Y + Mathf.RoundToInt(Mathf.Sin(angle) * (area.Radius + 1));
                if (CanDecorate(x, y)) AddDecor(region == 1 ? bush : rock, x, y, region == 1 ? "Grove shrubs" : "Frontier stones");
            }
        }
        var landmark = map.Layout.Landmark;
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI / 6;
            int x = landmark.X + Mathf.RoundToInt(Mathf.Cos(angle) * 4);
            int y = landmark.Y + Mathf.RoundToInt(Mathf.Sin(angle) * 4);
            if (CanDecorate(x, y)) AddDecor(rock, x, y, "Watchpost stones");
        }
        // The five-cell build pad stays free; the old tower stands beside it.
        bool placed=false;
        for(int radius=4;radius<=10&&!placed;radius++)for(int i=0;i<16&&!placed;i++)
        {
            int x=landmark.X+Mathf.RoundToInt(Mathf.Cos(i*Mathf.PI/8)*radius);
            int y=landmark.Y+Mathf.RoundToInt(Mathf.Sin(i*Mathf.PI/8)*radius);
            if(CanDecorate(x,y)){AddDecor(watchpost,x,y,"Old Watchpost");placed=true;}
        }
        InitializeDaytimeDetails(bush, rock);
        m_Fog.OnFogUpdated += RefreshVisibility;
        RefreshVisibility();
    }

    void PaintGround()
    {
        for (int y = 0; y < m_Map.Height; y++) for (int x = 0; x < m_Map.Width; x++)
        {
            if (!m_Map.IsGrass(x, y)) continue;
            int index = m_Map.Index(x, y), region = m_Map.Layout.RegionIds[index];
            var area = m_Map.Layout.Regions[region];
            float distance = Vector2.Distance(new Vector2(x,y), new Vector2(area.X,area.Y));
            var tint = region == 1 ? new Color(.72f,1f,.8f) : region == 2 ? new Color(1f,.82f,.55f) : Color.white;
            var color = Color.Lerp(Color.white, tint, Mathf.Clamp01((area.Radius + 8 - distance) / 8));
            if (m_Map.Layout.Routes[index] != 0)
            {
                bool spine=IsRoute(x-2,y)&&IsRoute(x+2,y)&&IsRoute(x,y-2)&&IsRoute(x,y+2);
                color=Color.Lerp(color,new Color(1f,.65f,.4f),spine?.5f:.1f);
            }
            var landmark = m_Map.Layout.Landmark;
            if (Mathf.Abs(x-landmark.X)<=2 && Mathf.Abs(y-landmark.Y)<=2)
                color=Mathf.Abs(x-landmark.X)==2||Mathf.Abs(y-landmark.Y)==2 ? new Color(.78f,.84f,.76f) : new Color(1f,.8f,.6f);
            var cell = m_Origin + new Vector3Int(x,y,0);
            color = VaryGround(x, y, color);
            m_Ground.RemoveTileFlags(cell, TileFlags.LockColor);
            m_Ground.SetColor(cell,color);
        }
    }
    bool CanDecorate(int x,int y)
    {
        if (!m_Map.IsGrass(x,y) || m_Map.Reserved[m_Map.Index(x,y)] != 0 || m_Map.Layout.Routes[m_Map.Index(x,y)] != 0) return false;
        foreach (var resource in m_Map.Resources)
            if ((resource.X-x)*(resource.X-x)+(resource.Y-y)*(resource.Y-y)<9) return false;
        return true;
    }
    bool IsRoute(int x,int y)=>m_Map.InBounds(x,y)&&m_Map.Layout.Routes[m_Map.Index(x,y)]!=0;
    Sprite MakeSprite(Texture2D texture,float pivotY,Rect? frame=null)
    {
        var sprite=Sprite.Create(texture,frame??new Rect(0,0,texture.width,texture.height),new Vector2(.5f,pivotY),64,0,SpriteMeshType.FullRect);
        m_Sprites.Add(sprite);return sprite;
    }
    SpriteRenderer AddDecor(Sprite sprite,int x,int y,string label)
    {
        var view=new GameObject(label);view.transform.SetParent(transform,false);view.transform.position=CellWorld(x,y);
        var renderer=view.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=m_WorldMaterial;
        renderer.sortingOrder=10;m_Decor.Add(renderer);return renderer;
    }
    Vector3 CellWorld(int x,int y)=>m_Ground.GetCellCenterWorld(m_Origin+new Vector3Int(x,y,0));
    void RefreshVisibility()
    {
        foreach (var renderer in m_Decor)
        {
            var position=renderer.transform.position;
            bool explored=m_Fog.IsInitialized&&(m_Fog.IsDisabled||m_Fog.IsWorldPositionExplored(position));
            renderer.enabled=explored&&(!m_DaytimeDetails.Contains(renderer)||IsFootprintKnown(position,false));
            float alpha=1;
            var cell=m_Ground.WorldToCell(position);
            if (!m_Fog.IsDisabled&&m_Fog.TryGetCellState(cell,out var state,out _)&&state!=FogCellState.Visible) alpha=.5f;
            renderer.color=new Color(1,1,1,alpha);
        }
        RefreshAmbientVisibility();
    }
    public bool IsSiteDiscovered(int region)
    {
        if (m_Map?.Layout == null || region<1 || region>=m_Map.Layout.Regions.Length) return false;
        var site=m_Map.Layout.Regions[region];return m_Fog.IsInitialized&&(m_Fog.IsDisabled||m_Fog.IsWorldPositionExplored(CellWorld(site.X,site.Y)));
    }
    public void DrawMinimapMarkers(Texture2D texture,BoundsInt bounds)
    {
        if (m_Map?.Layout == null) return;
        for(int region=1;region<3;region++)
        {
            if(!IsSiteDiscovered(region))continue;
            var site=m_Map.Layout.Regions[region];var cell=m_Origin+new Vector3Int(site.X,site.Y,0);
            int cx=cell.x-bounds.xMin,cy=cell.y-bounds.yMin;
            for(int y=-2;y<=2;y++)for(int x=-2;x<=2;x++)
            {
                bool ink=region==1 ? (y>=0&&Mathf.Abs(x)<=2-y)||(x==0&&y<0) : (y==-2)||(Mathf.Abs(x)==1)||(y==2);
                int px=cx+x,py=cy+y;
                if(px<0||py<0||px>=texture.width||py>=texture.height)continue;
                texture.SetPixel(px,py,ink?(region==1?new Color(.3f,1,.45f):new Color(1,.75f,.2f)):new Color(.08f,.1f,.12f));
            }
        }
    }
    void OnDestroy()
    {
        if(m_Fog!=null)m_Fog.OnFogUpdated-=RefreshVisibility;
        foreach(var renderer in m_Decor)if(renderer!=null)Destroy(renderer.gameObject);
        foreach(var sprite in m_Sprites)if(sprite!=null)Destroy(sprite);
        if(m_DetailAtlas!=null)Destroy(m_DetailAtlas);
    }
}
