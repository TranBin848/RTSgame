using UnityEngine;
using UnityEngine.UI;

// UI mesh glow keeps the overlay independent of camera bloom and the world render pipeline.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class ResearchTreeGraphic : MaskableGraphic
{
    public enum Shape { Node, Core, Link, Backdrop }
    public Shape Form;
    public Vector2 From, To;
    public float Emphasis = 1;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect = rectTransform.rect;
        if (Form == Shape.Link)
        {
            Stroke(mesh, From, To, 18, Alpha(color, .035f));
            Stroke(mesh, From, To, 9, Alpha(color, .09f));
            Stroke(mesh, From, To, 4, Alpha(color, .24f));
            Stroke(mesh, From, To, 1.4f, color);
            return;
        }
        if (Form == Shape.Backdrop)
        {
            Halo(mesh, Vector2.zero, 450, new Color(.14f, .42f, .44f, .11f));
            for (int i = 0; i < 110; i++)
            {
                float x = Mathf.Repeat(i * 367.7f + 91, rect.width);
                float y = Mathf.Repeat(i * 231.3f + 53, rect.height);
                Vector2 p = new(rect.xMin + x, rect.yMin + y);
                float a = .08f + (i % 4) * .025f;
                Stroke(mesh, p - Vector2.right * 1.2f, p + Vector2.right * 1.2f, 1, new Color(.6f, .8f, .85f, a));
            }
            return;
        }
        float radius = Mathf.Min(rect.width, rect.height) * .38f;
        Halo(mesh, rect.center, radius * 1.9f, Alpha(color, .16f * Emphasis));
        if (Form == Shape.Core)
        {
            Ring(mesh, rect.center, radius + 13, 1, Alpha(color, .35f));
            Ring(mesh, rect.center, radius, 3, color);
            Ring(mesh, rect.center, radius - 7, 1, Alpha(color, .45f));
        }
        else
        {
            Outline(mesh, rect.center, radius + 6, 10, Alpha(color, .05f * Emphasis));
            Outline(mesh, rect.center, radius + 2, 5, Alpha(color, .12f * Emphasis));
            Outline(mesh, rect.center, radius, 2, color);
            Outline(mesh, rect.center, radius - 6, .8f, Alpha(color, .3f));
        }
    }

    static Color Alpha(Color c, float factor) => new(c.r, c.g, c.b, c.a * factor);
    static void Outline(VertexHelper m, Vector2 c, float r, float width, Color tint)
    {
        float cut = r * .23f;
        Vector2[] p = { new(-r + cut, r), new(r - cut, r), new(r, r - cut), new(r, -r + cut),
            new(r - cut, -r), new(-r + cut, -r), new(-r, -r + cut), new(-r, r - cut) };
        for (int i = 0; i < p.Length; i++) Stroke(m, p[i] + c, p[(i + 1) % p.Length] + c, width, tint);
    }
    static void Ring(VertexHelper m, Vector2 c, float radius, float width, Color tint)
    {
        for (int i = 0; i < 64; i++)
        {
            float a = i * Mathf.PI / 32, b = (i + 1) * Mathf.PI / 32;
            Stroke(m, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                c + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, width, tint);
        }
    }
    static void Halo(VertexHelper m, Vector2 c, float radius, Color tint)
    {
        int start = m.currentVertCount;
        m.AddVert(c, tint, Vector2.zero);
        for (int i = 0; i <= 48; i++)
        {
            float angle = i * Mathf.PI / 24;
            m.AddVert(c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, Alpha(tint, 0), Vector2.zero);
            if (i > 0) m.AddTriangle(start, start + i, start + i + 1);
        }
    }
    static void Stroke(VertexHelper m, Vector2 a, Vector2 b, float width, Color tint)
    {
        Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * (width * .5f);
        int i = m.currentVertCount;
        m.AddVert(a - n, tint, Vector2.zero); m.AddVert(a + n, tint, Vector2.zero);
        m.AddVert(b + n, tint, Vector2.zero); m.AddVert(b - n, tint, Vector2.zero);
        m.AddTriangle(i, i + 1, i + 2); m.AddTriangle(i + 2, i + 3, i);
    }
}
