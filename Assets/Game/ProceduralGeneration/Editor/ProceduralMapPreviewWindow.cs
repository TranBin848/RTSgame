using RTS.ProceduralGeneration;
using UnityEditor;
using UnityEngine;

public sealed class ProceduralMapPreviewWindow : EditorWindow
{
    ProceduralMapSettings m_Settings;
    int m_Seed = 1;
    MapLayoutPreset m_Preset = MapLayoutPreset.StrategicRegions;
    Texture2D m_Texture;
    string m_Report = "Chọn preset và bấm Sinh preview. Preview không thay đổi scene.";
    public MapData Map { get; private set; }
    public Texture2D PreviewTexture => m_Texture;

    [MenuItem("RTS/Procedural Generation/Preview Regions")]
    public static void Open() => GetWindow<ProceduralMapPreviewWindow>("Map Regions");

    public void Generate(MapGenerationSettingsData settings)
    {
        Map = null;
        if(m_Texture != null) DestroyImmediate(m_Texture);
        if(!new ProceduralMapGenerator().TryGenerate(settings,out var map,out var error))
        {m_Report=error;Repaint();return;}
        Map=map;m_Texture=Render(map);
        m_Report=$"Seed {map.Seed} / attempt {map.Attempt} / generator v{map.GeneratorVersion}\n"
            + $"{map.Width}×{map.Height} / {map.Resources.Count} resources / repairs {map.Validation.Repairs}";
        Repaint();
    }
    void OnGUI()
    {
        m_Settings=(ProceduralMapSettings)EditorGUILayout.ObjectField("Settings (không sửa asset)",m_Settings,typeof(ProceduralMapSettings),false);
        m_Seed=EditorGUILayout.IntField("Seed",m_Seed);
        m_Preset=(MapLayoutPreset)EditorGUILayout.EnumPopup("Preset",m_Preset);
        if(GUILayout.Button("Sinh preview"))
        {
            var data=m_Settings != null?m_Settings.ToData():new MapGenerationSettingsData();
            data.Seed=m_Seed;data.Preset=m_Preset;Generate(data);
        }
        EditorGUILayout.HelpBox(m_Report,Map==null?MessageType.Info:MessageType.None);
        GUILayout.Label("Vàng: xuất phát | xanh: kinh tế | cam: mỏ | trắng: đường đề xuất\nTím: landmark | đỏ: cửa đề xuất | xanh ngọc: tâm vùng\nChấm xanh đậm/vàng/trắng: cây/vàng/cừu; chưa phân bố theo budget vùng.",EditorStyles.wordWrappedLabel);
        if(m_Texture!=null)
        {
            var rect=GUILayoutUtility.GetRect(100,10000,100,10000,GUILayout.ExpandWidth(true),GUILayout.ExpandHeight(true));
            EditorGUI.DrawPreviewTexture(rect,m_Texture,null,ScaleMode.ScaleToFit);
        }
    }
    void OnDisable() {if(m_Texture!=null)DestroyImmediate(m_Texture);}

    public static Texture2D Render(MapData map)
    {
        var texture=new Texture2D(map.Width,map.Height,TextureFormat.RGB24,false) {filterMode=FilterMode.Point};
        var pixels=new Color[map.Width*map.Height];
        Color[] roles={new Color(0.7f,0.65f,0.24f),new Color(0.18f,0.48f,0.28f),new Color(0.65f,0.36f,0.17f)};
        for(int i=0;i<pixels.Length;i++)
        {
            pixels[i]=map.Terrain[i]==(byte)TerrainCell.Water?new Color(0.06f,0.15f,0.3f)
                :map.Layout==null?new Color(0.22f,0.4f,0.22f):roles[map.Layout.RegionIds[i]];
            if(map.Layout!=null&&map.Layout.Routes[i]!=0)pixels[i]=new Color(0.8f,0.8f,0.8f);
        }
        foreach(var resource in map.Resources)pixels[map.Index(resource.X,resource.Y)]=resource.Kind==ProceduralResourceKind.Tree?new Color(0,0.18f,0)
            :resource.Kind==ProceduralResourceKind.Gold?Color.yellow:Color.white;
        if(map.Layout!=null)
        {
            foreach(var region in map.Layout.Regions)Cross(region.X,region.Y,2,Color.cyan);
            foreach(var gate in map.Layout.Gateways)Cross(gate.X,gate.Y,3,Color.red);
            var landmark=map.Layout.Landmark;Cross(landmark.X,landmark.Y,landmark.Radius,Color.magenta);
        }
        texture.SetPixels(pixels);texture.Apply();return texture;
        void Cross(int x,int y,int radius,Color color)
        {for(int offset=-radius;offset<=radius;offset++){Set(x+offset,y,color);Set(x,y+offset,color);}}
        void Set(int x,int y,Color color){if(map.InBounds(x,y))pixels[map.Index(x,y)]=color;}
    }
}
