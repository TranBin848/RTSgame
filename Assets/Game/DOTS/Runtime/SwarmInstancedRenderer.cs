using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTS.DOTS
{
    [DefaultExecutionOrder(200)]
    public sealed class SwarmInstancedRenderer : MonoBehaviour
    {
        public SwarmVisualLibrary Library;
        public bool DrawEntities = true;
        public SwarmSimulationSystem Simulation { get; set; }
        public int VisibleInstances { get; private set; }
        public int DrawCalls { get; private set; }
        public Texture FogTexture { get; set; }
        public Vector4 FogBounds { get; set; }
        public bool FogEnabled { get; set; }
        public Color EnvironmentColor { get; set; } = Color.white;
        Material m_ContactMaterial;
        Batch m_Contact;
        Mesh m_ContactMesh;
        MeshRenderer m_ContactView;
        readonly Vector3[] m_ContactVertices = new Vector3[4092];
        readonly Vector2[] m_ContactUvs = new Vector2[4092];
        readonly int[] m_ContactIndices = new int[6138];
        public float ContactStretch { get; set; } = 1;
        public int ContactInstances { get; private set; }
        public bool HasDepthPresentation => m_ContactMaterial != null;

        public void SetDepthPresentation(Material contact)
        {
            if (m_ContactMaterial != null) Destroy(m_ContactMaterial);
            m_ContactMaterial = contact != null ? new Material(contact) { enableInstancing = true } : null;
            if(contact!=null&&m_ContactView==null)
            {
                var view=new GameObject("Native contact batch");view.transform.SetParent(transform,true);
                m_ContactMesh=new Mesh{name="Native contact batch"};m_ContactMesh.MarkDynamic();view.AddComponent<MeshFilter>().sharedMesh=m_ContactMesh;
                m_ContactView=view.AddComponent<MeshRenderer>();m_ContactView.sortingOrder=20;m_ContactView.shadowCastingMode=ShadowCastingMode.Off;m_ContactView.receiveShadows=false;
                for(int i=0;i<1023;i++){int v=i*4,j=i*6;m_ContactUvs[v]=Vector2.zero;m_ContactUvs[v+1]=Vector2.right;m_ContactUvs[v+2]=Vector2.up;m_ContactUvs[v+3]=Vector2.one;m_ContactIndices[j]=v;m_ContactIndices[j+1]=v+2;m_ContactIndices[j+2]=v+1;m_ContactIndices[j+3]=v+2;m_ContactIndices[j+4]=v+3;m_ContactIndices[j+5]=v+1;}
            }
            if(m_ContactView!=null){m_ContactView.sharedMaterial=m_ContactMaterial;m_ContactView.enabled=false;}
            if(contact==null)
            {
                if(m_ContactView!=null)Destroy(m_ContactView.gameObject);if(m_ContactMesh!=null)Destroy(m_ContactMesh);
                m_ContactView=null;m_ContactMesh=null;
            }
            foreach (var batch in m_Batches.Values)
            {
                batch.Material.renderQueue = contact != null ? 3003 : 3001;
                batch.Material.SetFloat("_SwarmPointSampling", contact != null ? 1 : 0);
            }
        }

        sealed class Batch
        {
            public readonly Matrix4x4[] Matrices = new Matrix4x4[1023];
            public readonly Vector4[] UV = new Vector4[1023], Colors = new Vector4[1023];
            public readonly MaterialPropertyBlock Properties = new();
            public Material Material;
            public int Count;
        }
        struct FrameInfo { public Texture Texture; public Vector4 UV; public Vector2 Size, Center; }
        readonly Dictionary<Texture, Batch> m_Batches = new();
        readonly Dictionary<Sprite, FrameInfo> m_Frames = new();
        Mesh m_Quad;
        Shader m_Shader;
        Texture2D m_White;
        Camera m_Camera;
        static readonly int UVId = Shader.PropertyToID("_UVRect"), ColorId = Shader.PropertyToID("_InstanceColor");

        void Awake()
        {
            m_Contact = new Batch();
            if (Library == null) Library = Resources.Load<SwarmVisualLibrary>("DOTS/SwarmVisualLibrary");
            var retainedMaterial = Resources.Load<Material>("DOTS/SwarmSprite");
            m_Shader = retainedMaterial != null ? retainedMaterial.shader : Shader.Find("RTS/DOTS/SwarmSprite");
            m_Quad = new Mesh { name = "Swarm shared quad" };
            m_Quad.vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f) };
            m_Quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            m_Quad.triangles = new[] { 0, 1, 2, 2, 3, 0 };
            m_Quad.RecalculateBounds();
            m_White = new Texture2D(1, 1); m_White.SetPixel(0, 0, Color.white); m_White.Apply();
            if (Library != null)
                foreach (var visual in Library.Visuals)
                { Cache(visual.Idle); Cache(visual.Move); Cache(visual.Attack); Cache(visual.Dead); }
            EnsureBatch(m_White);
        }
        void Cache(Sprite[] frames)
        {
            if (frames == null) return;
            foreach (var sprite in frames)
            {
                if (sprite == null || m_Frames.ContainsKey(sprite)) continue;
                // Tight sprites trim transparent padding. Use matching mesh and UV bounds instead of
                // stretching their trimmed textureRect over the full source canvas.
                var vertices = sprite.vertices; var uv = sprite.uv;
                if (vertices.Length == 0 || uv.Length == 0) continue;
                Vector2 min = vertices[0], max = vertices[0], uvMin = uv[0], uvMax = uv[0];
                for (int i = 1; i < vertices.Length; i++) { min = Vector2.Min(min, vertices[i]); max = Vector2.Max(max, vertices[i]); }
                for (int i = 1; i < uv.Length; i++) { uvMin = Vector2.Min(uvMin, uv[i]); uvMax = Vector2.Max(uvMax, uv[i]); }
                m_Frames.Add(sprite, new FrameInfo
                {
                    Texture = sprite.texture,
                    UV = new Vector4(uvMin.x, uvMin.y, uvMax.x - uvMin.x, uvMax.y - uvMin.y),
                    Size = max - min, Center = (min + max) * 0.5f
                });
                EnsureBatch(sprite.texture);
            }
        }
        public void SetLibrary(SwarmVisualLibrary library)
        {
            if (library == null) return;
            Library = library;
            foreach (var visual in Library.Visuals)
            { Cache(visual.Idle); Cache(visual.Move); Cache(visual.Attack); Cache(visual.Dead); }
        }
        void EnsureBatch(Texture texture)
        {
            if (m_Batches.ContainsKey(texture) || m_Shader == null) return;
            var material = new Material(m_Shader) { name = "Swarm instanced sprites", enableInstancing = true };
            material.mainTexture = texture;
            if (HasDepthPresentation) { material.renderQueue = 3003; material.SetFloat("_SwarmPointSampling", 1); }
            m_Batches.Add(texture, new Batch { Material = material });
        }
        void LateUpdate()
        {
            VisibleInstances = 0; DrawCalls = 0; ContactInstances = 0; m_Contact.Count = 0;
            if(m_ContactView!=null)m_ContactView.enabled=false;
            if (!DrawEntities || Simulation == null || Simulation.World == null || !Simulation.World.IsCreated || m_Shader == null || !SystemInfo.supportsInstancing) return;
            if (m_Camera == null) m_Camera = Camera.main;
            if (m_Camera == null) return;
            foreach (var batch in m_Batches.Values) batch.Count = 0;
            float vertical = m_Camera.orthographicSize + 5, horizontal = vertical * m_Camera.aspect + 5;
            Vector3 cameraPosition = m_Camera.transform.position;
            var data = Simulation.RenderData;
            for (int i = 0; i < data.Length; i++)
            {
                var item = data[i];
                if (math.abs(item.Position.x - cameraPosition.x) > horizontal || math.abs(item.Position.y - cameraPosition.y) > vertical) continue;
                SwarmVisualLibrary.Visual visual = Library != null && item.VisualId >= 0 && item.VisualId < Library.Visuals.Length ? Library.Visuals[item.VisualId] : null;
                Sprite sprite = visual?.Frame(item.State, item.Age, item.State == SwarmState.Attacking ? 0 : item.Entity.Index % 7);
                FrameInfo frame = sprite != null && m_Frames.TryGetValue(sprite, out var info) ? info : new FrameInfo
                { Texture = m_White, UV = new Vector4(0, 0, 1, 1), Size = Vector2.one * (item.Projectile != 0 ? 0.16f : 0.5f) };
                if (!m_Batches.TryGetValue(frame.Texture, out var batch)) continue;
                if (batch.Count == 1023) Flush(batch);
                float scale = (visual?.Scale ?? 1) * (item.VisualScale > 0 ? item.VisualScale : 1);
                float footY=item.Position.y+(visual?.FootOffset??0)*scale;
                if (HasDepthPresentation && item.Projectile == 0 && item.State != SwarmState.Dead && ContactInstances<1023)
                {
                    float width = Mathf.Clamp(frame.Size.x * scale * .55f, .35f, 1.5f);
                    int s = m_Contact.Count++;
                    m_Contact.Matrices[s] = Matrix4x4.TRS(new Vector3(item.Position.x + (ContactStretch - 1) * .12f, footY, -.8f),
                        Quaternion.Euler(0, 0, -20), new Vector3(width * ContactStretch, width * .32f, 1));
                    ContactInstances++;
                }
                bool rotate = visual != null && visual.RotateWithVelocity;
                float angle = item.Spinning != 0 ? item.Age * 720 : rotate ? math.degrees(math.atan2(item.Direction.y, item.Direction.x)) : 0;
                float flip = !rotate && item.Direction.x < 0 ? -1 : 1;
                Vector3 position = new(item.Position.x + frame.Center.x * flip * scale, item.Position.y + item.Height + frame.Center.y * scale,
                    -1f + (HasDepthPresentation&&item.Projectile==0?footY:item.Position.y) * 0.001f - (item.Projectile != 0 ? 0.1f : 0));
                int n = batch.Count++;
                batch.Matrices[n] = Matrix4x4.TRS(position, Quaternion.Euler(0, 0, angle), new Vector3(frame.Size.x * scale * flip, frame.Size.y * scale, 1));
                batch.UV[n] = frame.UV;
                Color tint = sprite != null ? Color.white : item.Faction == 0 ? Color.cyan : item.Projectile != 0 ? Color.yellow : new Color(0.8f, 0.25f, 0.2f);
                if (item.State == SwarmState.Dead) tint *= new Color(0.6f, 0.6f, 0.6f, Mathf.Clamp01(1 - item.Age / 0.8f));
                batch.Colors[n] = tint;
                VisibleInstances++;
            }
            foreach (var batch in m_Batches.Values) Flush(batch);
            FlushContact();
        }
        void FlushContact()
        {
            if (m_Contact.Count == 0 || m_ContactMaterial == null) return;
            for(int i=0;i<m_Contact.Count;i++)
            {
                var m=m_Contact.Matrices[i];int v=i*4;
                m_ContactVertices[v]=m.MultiplyPoint3x4(new Vector3(-.5f,-.5f));m_ContactVertices[v+1]=m.MultiplyPoint3x4(new Vector3(.5f,-.5f));m_ContactVertices[v+2]=m.MultiplyPoint3x4(new Vector3(-.5f,.5f));m_ContactVertices[v+3]=m.MultiplyPoint3x4(new Vector3(.5f,.5f));
            }
            m_ContactMesh.Clear();m_ContactMesh.SetVertices(m_ContactVertices,0,m_Contact.Count*4);m_ContactMesh.SetUVs(0,m_ContactUvs,0,m_Contact.Count*4);m_ContactMesh.SetTriangles(m_ContactIndices,0,m_Contact.Count*6,0);m_ContactMesh.RecalculateBounds();
            m_Contact.Properties.SetTexture("_ContactFog", FogTexture != null ? FogTexture : m_White);
            m_Contact.Properties.SetVector("_ContactFogBounds", FogBounds);
            m_Contact.Properties.SetFloat("_ContactFogEnabled", FogEnabled ? 1 : 0);
            m_ContactView.SetPropertyBlock(m_Contact.Properties);m_ContactView.enabled=true;
            m_Contact.Count = 0;
        }
        void Flush(Batch batch)
        {
            if (batch.Count == 0) return;
            batch.Properties.SetVectorArray(UVId, batch.UV);
            batch.Properties.SetVectorArray(ColorId, batch.Colors);
            batch.Properties.SetTexture("_SwarmFog", FogTexture != null ? FogTexture : m_White);
            batch.Properties.SetVector("_SwarmFogBounds", FogBounds);
            batch.Properties.SetFloat("_SwarmFogEnabled", FogEnabled ? 1 : 0);
            batch.Properties.SetColor("_SwarmLight", EnvironmentColor);
            Graphics.DrawMeshInstanced(m_Quad, 0, batch.Material, batch.Matrices, batch.Count, batch.Properties,
                ShadowCastingMode.Off, false, gameObject.layer, m_Camera, LightProbeUsage.Off);
            DrawCalls++; batch.Count = 0;
        }
        void OnDestroy()
        {
            if (m_ContactMaterial != null) Destroy(m_ContactMaterial);
            if(m_ContactView!=null)Destroy(m_ContactView.gameObject);if(m_ContactMesh!=null)Destroy(m_ContactMesh);
            foreach (var batch in m_Batches.Values) if (batch.Material != null) Destroy(batch.Material);
            if (m_Quad != null) Destroy(m_Quad);
            if (m_White != null) Destroy(m_White);
        }
    }
}
