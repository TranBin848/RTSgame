using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public static class E9GameViewCapture
{
    public static void Save(string name)
    {
        const string dir="Docs/Modules/GameExperience/Evidence/E9/Realtime/";
        var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var view=EditorWindow.GetWindow(type);view.Repaint();
        var rt=(RenderTexture)type.GetField("m_RenderTexture",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
        var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);var old=RenderTexture.active;
        try
        {
            RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();
            if(SystemInfo.graphicsUVStartsAtTop)
            {
                var pixels=image.GetPixels32();
                for(int y=0;y<rt.height/2;y++)for(int x=0;x<rt.width;x++)
                {int a=y*rt.width+x,b=(rt.height-1-y)*rt.width+x;var p=pixels[a];pixels[a]=pixels[b];pixels[b]=p;}
                image.SetPixels32(pixels);image.Apply();
            }
            Directory.CreateDirectory(dir);File.WriteAllBytes(dir+name,image.EncodeToPNG());
        }
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}
    }
}
