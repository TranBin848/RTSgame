using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public static class GameExperienceE5Validation
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const string Dir = "Docs/Modules/GameExperience/Evidence/E5/";
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static void Check(bool value, string message) { if (!value) throw new Exception("E5: " + message); }
    static object Field(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    static void Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    static void Save(string name, string result) { Directory.CreateDirectory(Dir); File.WriteAllText(Dir + name, result); }

    public static string Run()
    {
        var root = Find<GameSceneCompositionRoot>(); var depth = root.DepthPresentation;
        Check(EditorApplication.isPlaying && EditorApplication.isPaused && root.IsReady && depth.IsInitialized, "fresh Ready paused match required");
        var game = Find<GameManager>(); var bridge = Find<SwarmGameBridge>(); var fog = Find<FogOfWarManager>(); var clock = Find<DayNightCycleManager>();
        var native = Find<RTS.DOTS.SwarmInstancedRenderer>(); var map = Find<TilemapManager>();
        var rng = JsonUtility.ToJson(UnityEngine.Random.state); var wallet = (game.Wood,game.Gold,game.Meat); var ticks = bridge.Simulation.TickCount;
        int actors = bridge.Gameplay.ActorCount, nav = map.NavigationVersion;
        var views = game.GetFriendlyUnits(true).Where(u => u != null).Select(u => u.transform).ToArray();
        var poses = views.Select(t => (t.position,t.localScale)).ToArray();
        depth.Initialize(game,clock,fog,Camera.main,clock.Presentation,native,Find<ProceduralMapAuthoring>().GeneratedResourceRoot,Find<ProceduralMapPresentation>().Decorations);
        depth.RefreshPresentation();
        Check(depth.RegisteredBodies >= 150 && native.HasDepthPresentation, "body registry/native depth not enabled");
        Check(fog.OnFogUpdated.GetInvocationList().Count(d => d.Target == depth) == 1, "duplicate fog listener");
        int materials = Resources.FindObjectsOfTypeAll<Material>().Length;
        for(int i=0;i<20;i++) depth.RefreshPresentation();
        Check(materials == Resources.FindObjectsOfTypeAll<Material>().Length, "per-frame materials");
        foreach(var body in (IEnumerable)Field(depth,"m_Bodies"))
        {
            var r=(SpriteRenderer)body.GetType().GetField("Renderer").GetValue(body); var block=new MaterialPropertyBlock(); r.GetPropertyBlock(block);
            var collider=(Collider2D)body.GetType().GetField("GroundCollider").GetValue(body);
            float half=collider==null?0:collider is CapsuleCollider2D capsule?capsule.size.y*.5f:collider is BoxCollider2D box?box.size.y*.5f:collider is CircleCollider2D circle?circle.radius:0;
            float footY=collider!=null?collider.transform.TransformPoint(collider.offset-Vector2.up*half).y:r.transform.position.y;
            Check(r.sharedMaterial.shader.name=="RTS/Presentation/FootDepth" && Mathf.Approximately(block.GetFloat("_DepthFootY"),footY),"foot depth source mismatch");
        }
        Check(depth.ContactCount<=512 && depth.SelectionRingCount==0,"contact budget or ghost selection");
        var oldPhase=clock.CurrentPhase;float oldProgress=clock.CurrentPhaseProgress;
        typeof(DayNightCycleManager).GetProperty("CurrentPhase").SetValue(clock,DayNightPhase.Dusk);
        typeof(DayNightCycleManager).GetProperty("CurrentPhaseProgress").SetValue(clock,.5f);
        Check(Mathf.Approximately(depth.ShadowStretch,1.8f),"Dusk contact stretch");
        typeof(DayNightCycleManager).GetProperty("CurrentPhase").SetValue(clock,oldPhase);
        typeof(DayNightCycleManager).GetProperty("CurrentPhaseProgress").SetValue(clock,oldProgress);
        Check(poses.SequenceEqual(views.Select(t=>(t.position,t.localScale))) && rng==JsonUtility.ToJson(UnityEngine.Random.state)
            && wallet==(game.Wood,game.Gold,game.Meat) && ticks==bridge.Simulation.TickCount && actors==bridge.Gameplay.ActorCount && nav==map.NavigationVersion,"presentation mutated gameplay/poses/RNG");
        foreach(string path in new[]{"Assets/Game/ProceduralGeneration/Integration/FootDepth.shader","Assets/Game/ProceduralGeneration/Integration/ContactShadow.shader","Assets/Game/DOTS/Resources/DOTS/SwarmSprite.shader"})
            Check(!ShaderUtil.GetShaderMessages(AssetDatabase.LoadAssetAtPath<Shader>(path)).Any(m=>m.severity.ToString()=="Error"),"shader errors: "+path);
        string result=$"PASS E5 root/repeat init; registry={depth.RegisteredBodies}, native enabled; one fog listener; no materials added over20refreshes; foot depth property matches render anchor; contacts={depth.ContactCount}<=512; no ghost rings; Dusk midpoint stretch1.8; actor transforms/scales, wallet/actors/ticks/nav/RNG unchanged;3shaders no compile errors.\n";
        Save("integration.txt",result);return result;
    }

    public static string GpuProbe()
    {
        Directory.CreateDirectory(Dir);
        var root=new GameObject("E5 synthetic GPU fixture");
        var camera=root.AddComponent<Camera>(); camera.orthographic=true;camera.orthographicSize=2;camera.aspect=1;
        camera.transform.position=new Vector3(0,1,-10); camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.blue;camera.cullingMask=1<<31;
        var extra=root.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();extra.renderPostProcessing=false;
        var texture=new Texture2D(4,4,TextureFormat.RGBA32,false);texture.SetPixels(Enumerable.Repeat(Color.white,16).ToArray());texture.Apply();
        var sprite=Sprite.Create(texture,new Rect(0,0,4,4),new Vector2(.5f,0),2,0,SpriteMeshType.FullRect);
        var body=new GameObject("Synthetic canopy");body.layer=31;var sr=body.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=Color.red;
        var foot=new Material(Shader.Find("RTS/Presentation/FootDepth"));sr.sharedMaterial=foot; var props=new MaterialPropertyBlock();
        props.SetFloat("_DepthFootY",0);props.SetFloat("_DepthRoofY",1);sr.SetPropertyBlock(props);
        var unit=new GameObject("Synthetic instanced-shader quad");unit.layer=31;var mesh=unit.AddComponent<MeshFilter>();
        var quad=new Mesh();quad.vertices=new[]{new Vector3(-.3f,0),new Vector3(.3f,0),new Vector3(-.3f,1),new Vector3(.3f,1)};
        quad.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one};quad.triangles=new[]{0,2,1,2,3,1};quad.RecalculateBounds();mesh.sharedMesh=quad;
        var renderer=unit.AddComponent<MeshRenderer>();var native=new Material(Shader.Find("RTS/DOTS/SwarmSprite")){renderQueue=3003};native.mainTexture=texture;
        native.SetColor("_SwarmLight",Color.white);renderer.sharedMaterial=native;
        var block=new MaterialPropertyBlock();block.SetVector("_UVRect",new Vector4(0,0,1,1));block.SetVector("_InstanceColor",Color.green);renderer.SetPropertyBlock(block);
        var target=RenderTexture.GetTemporary(192,192,24,RenderTextureFormat.ARGB32);var image=new Texture2D(192,192,TextureFormat.RGB24,false);
        Texture2D fog=null;Material contact=null;
        void Render(string name)
        {
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            var old=RenderTexture.active;RenderTexture.active=target;image.ReadPixels(new Rect(0,0,192,192),0,0);image.Apply();RenderTexture.active=old;
            File.WriteAllBytes(Dir+name+".png",image.EncodeToPNG());
        }
        Color At(float x,float y){var uv=camera.WorldToViewportPoint(new Vector3(x,y,0));return image.GetPixel(Mathf.Clamp((int)(uv.x*192),0,191),Mathf.Clamp((int)(uv.y*192),0,191));}
        try
        {
            unit.transform.position=new Vector3(0,.6f,-1+.6f*.001f);Render("gpu-behind-solid");Check(At(0,1.3f).r>.8f&&At(0,1.3f).g<.1f,"native behind canopy not occluded");
            unit.transform.position=new Vector3(0,-.3f,-1-.3f*.001f);Render("gpu-front-solid");Check(At(0,.3f).g>.8f&&At(0,.3f).r<.1f,"native in front not visible");
            unit.transform.position=new Vector3(0,.6f,-1+.6f*.001f);props.SetFloat("_DepthFade",1);sr.SetPropertyBlock(props);Render("gpu-behind-faded-top");
            int red=0,green=0;for(float y=1.25f;y<1.5f;y+=.025f)for(float x=-.2f;x<.2f;x+=.025f){var c=At(x,y);if(c.r>.8f)red++;if(c.g>.8f)green++;}
            Check(red>10&&green>10,"roof stipple does not expose unit through real depth holes");Check(At(0,.8f).r>.8f,"fade changed solid lower body/trunk");
            renderer.enabled=false;props.SetFloat("_DepthFade",0);sr.SetPropertyBlock(props);
            fog=new Texture2D(3,1,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};fog.SetPixels(new[]{Color.white,new Color(.45f,.45f,.45f),Color.black});fog.Apply();
            foot.SetTexture("_DepthFog",fog);foot.SetVector("_DepthFogBounds",new Vector4(-1.5f,0,3,2));foot.SetFloat("_DepthFogEnabled",1);Render("gpu-static-fog-memory");
            Check(At(-.7f,.5f).r>.8f&&At(.7f,.5f).b>.8f&&At(.7f,.5f).r<.1f&&At(0,.5f).r>.2f,"static pixel fog memory/unexplored mask");
            props.SetFloat("_DepthMobile",1);sr.SetPropertyBlock(props);Render("gpu-mobile-fog-memory-hidden");Check(At(0,.5f).b>.8f&&At(0,.5f).r<.1f,"mobile actor visible in explored-only fog");
            sr.enabled=false;renderer.enabled=true;unit.transform.position=new Vector3(0,0,-.8f);
            contact=new Material(Shader.Find("RTS/Presentation/ContactShadow"));renderer.sharedMaterial=contact;contact.SetColor("_ContactColor",new Color(0,0,0,1));
            contact.SetTexture("_ContactFog",fog);contact.SetVector("_ContactFogBounds",new Vector4(-.45f,0,.9f,1));contact.SetFloat("_ContactFogEnabled",1);
            Render("gpu-contact-fog-mask");Check(At(-.2f,.5f).b<.8f&&At(.2f,.5f).b>.95f,"contact leaks across fog mask");
            contact.SetFloat("_ContactFogEnabled",0);contact.SetFloat("_ContactRing",1);contact.SetFloat("_ContactZTest",8);contact.SetColor("_ContactColor",Color.green);
            Render("gpu-selection-ring");Check(At(.25f,.5f).g>.8f&&At(0,.5f).b>.8f,"analytic selection ring invalid");
            renderer.enabled=false;sr.enabled=true;foot.SetFloat("_DepthFogEnabled",0);props.SetFloat("_DepthMobile",0);sr.SetPropertyBlock(props);
            texture.filterMode=FilterMode.Bilinear;texture.SetPixels(Enumerable.Range(0,16).Select(i=>(i+i/4)%2==0?Color.white:Color.black).ToArray());texture.Apply();
            foreach(float zoom in new[]{2f,3f,4f})
            {
                camera.orthographicSize=zoom;camera.transform.position=new Vector3(0,1,-10);Render("gpu-point-zoom-"+zoom);
                Check(!image.GetPixels32().Any(c=>c.r>10&&c.r<245),"point shader blurred bilinear source");
                var before=image.GetPixels32();camera.transform.position+=new Vector3(.001f,0,0);Render("gpu-point-pan-"+zoom);
                Check(before.SequenceEqual(image.GetPixels32()),"subpixel camera pan jitter");
            }
            string result=$"PASS synthetic192x192 GPU: native-shader quad behind/front correctly depth tested against SpriteRenderer; upper fade leaves red={red}/green={green} samples, lower body stays opaque; static visible/explored/unexplored per-pixel mask and mobile explored-only rejection. Uses real shaders via MeshRenderer quad, not a native simulation workload.\n";
            result+="PASS contact per-pixel fog mask, analytic ring; bilinear source texture stays sharp through point shader at ortho2/3/4;0.001world camera pan yields identical pixels after render-anchor snap.\n";
            Save("gpu-probe.txt",result);return result;
        }
        finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(body);UnityEngine.Object.DestroyImmediate(unit);foreach(var o in new UnityEngine.Object[]{texture,sprite,foot,native,contact,quad,image,fog})if(o!=null)UnityEngine.Object.DestroyImmediate(o);RenderTexture.ReleaseTemporary(target);}
    }
}
