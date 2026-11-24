using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
public static class E5GlowSceneProbe {
public static string Run() {
var hall=UnityEngine.Object.FindFirstObjectByType<TownHall>();var c=Camera.main;var extra=c.GetComponent<UniversalAdditionalCameraData>();var oldPP=extra.renderPostProcessing;extra.renderPostProcessing=false;var pos=c.transform.position;float size=c.orthographicSize,aspect=c.aspect;
var g=hall.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Hall window core");var b=new MaterialPropertyBlock();g.GetPropertyBlock(b);float foot=b.GetFloat("_NightFootY");var rt=RenderTexture.GetTemporary(128,128,24);var t=new Texture2D(128,128,TextureFormat.RGB24,false);var old=RenderTexture.active;string result="";
try {c.transform.position=g.transform.position+new Vector3(0,0,-10);c.orthographicSize=.5f;c.aspect=1;
foreach(var mode in new[]{"normal","near","off"}) {g.enabled=mode!="off";b.SetFloat("_NightFootY",mode=="near"?-1000:foot);g.SetPropertyBlock(b);
RenderPipeline.SubmitRenderRequest(c,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,128,128),0,0);t.Apply();File.WriteAllBytes("Docs/Modules/GameExperience/Evidence/E5/E2-glow-"+mode+".png",t.EncodeToPNG());result+=mode+":"+t.GetPixel(64,64).ToString("R")+";";}
File.WriteAllText("Docs/Modules/GameExperience/Evidence/E5/E2-scene-glow-probe.txt",result);return result;
} finally {g.enabled=true;b.SetFloat("_NightFootY",foot);g.SetPropertyBlock(b);c.transform.position=pos;c.orthographicSize=size;c.aspect=aspect;extra.renderPostProcessing=oldPP;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(t);}
}}
