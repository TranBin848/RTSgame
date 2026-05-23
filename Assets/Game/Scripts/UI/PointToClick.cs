using System.Collections.Generic;
using UnityEngine;

public class PointToClick : MonoBehaviour
{
    [Header("LOL/AOE Move Indicator Settings")]
    [SerializeField] private Color m_Color = new Color(0f, 1f, 0.3f, 1f); // Classic Green
    [SerializeField] private float m_Lifetime = 0.4f;
    [SerializeField] private float m_StartDistance = 0.8f;
    [SerializeField] private float m_EndDistance = 0.1f;
    [SerializeField] private float m_StartScale = 0.6f;
    [SerializeField] private float m_EndScale = 0.15f;
    [SerializeField] private Sprite m_CustomArrowSprite;

    private Sprite m_ArrowSprite;
    private Texture2D m_GeneratedTex;
    private List<Transform> m_Arrows = new();
    private List<SpriteRenderer> m_Renderers = new();
    private float m_Timer;

    void Start()
    {
        // 1. Hide existing Root SpriteRenderer if any to prevent visual conflicts
        if (TryGetComponent<SpriteRenderer>(out var rootSR))
        {
            rootSR.enabled = false;
        }

        // 2. Load or procedurally generate the arrow sprite
        if (m_CustomArrowSprite != null)
        {
            m_ArrowSprite = m_CustomArrowSprite;
        }
        else
        {
            m_GeneratedTex = CreateArrowTexture(m_Color);
            m_ArrowSprite = Sprite.Create(m_GeneratedTex, new Rect(0, 0, m_GeneratedTex.width, m_GeneratedTex.height), new Vector2(0.5f, 0f)); // Pivot at tip
        }

        // 3. Create 4 arrows pointing towards the center
        CreateArrow(Vector3.up * m_StartDistance, Quaternion.Euler(0, 0, 0));       // Top arrow: points Down
        CreateArrow(Vector3.right * m_StartDistance, Quaternion.Euler(0, 0, 90));   // Right arrow: points Left
        CreateArrow(Vector3.down * m_StartDistance, Quaternion.Euler(0, 0, 180));   // Bottom arrow: points Up
        CreateArrow(Vector3.left * m_StartDistance, Quaternion.Euler(0, 0, 270));    // Left arrow: points Right
    }

    void CreateArrow(Vector3 localPos, Quaternion rotation)
    {
        GameObject arrowObj = new GameObject("LOLArrow");
        arrowObj.transform.SetParent(transform);
        arrowObj.transform.localPosition = localPos;
        arrowObj.transform.localRotation = rotation;
        arrowObj.transform.localScale = Vector3.one * m_StartScale;

        SpriteRenderer sr = arrowObj.AddComponent<SpriteRenderer>();
        sr.sprite = m_ArrowSprite;
        sr.color = m_Color;
        sr.sortingOrder = 1000; // Force render on top of units and ground

        m_Arrows.Add(arrowObj.transform);
        m_Renderers.Add(sr);
    }

    void Update()
    {
        m_Timer += Time.deltaTime;
        float t = m_Timer / m_Lifetime;

        if (t >= 1.0f)
        {
            Destroy(gameObject);
            return;
        }

        // Animate distance, scale, and transparency
        float distance = Mathf.Lerp(m_StartDistance, m_EndDistance, t);
        float scale = Mathf.Lerp(m_StartScale, m_EndScale, t);
        float alpha = Mathf.Clamp01(1.0f - t);

        // Update arrow positions
        m_Arrows[0].localPosition = Vector3.up * distance;
        m_Arrows[1].localPosition = Vector3.right * distance;
        m_Arrows[2].localPosition = Vector3.down * distance;
        m_Arrows[3].localPosition = Vector3.left * distance;

        // Apply scale and alpha to each arrow
        for (int i = 0; i < 4; i++)
        {
            if (m_Arrows[i] != null)
            {
                m_Arrows[i].localScale = Vector3.one * scale;
            }
            if (m_Renderers[i] != null)
            {
                Color c = m_Renderers[i].color;
                m_Renderers[i].color = new Color(c.r, c.g, c.b, alpha);
            }
        }
    }

    private void OnDestroy()
    {
        if (m_GeneratedTex != null)
        {
            Destroy(m_GeneratedTex);
        }
    }

    private Texture2D CreateArrowTexture(Color color)
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        // Fill with transparent color
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, Color.clear);
            }
        }

        // Draw a sharp, premium V-shape chevron pointing down
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - size / 2.0f);
                float thickness = 10f;
                float angleFactor = 1.0f; // 45 degrees
                float tipY = 8f;          // Arrow tip position from bottom

                float vLine = dx * angleFactor + tipY;
                if (y >= vLine && y <= vLine + thickness && y < size - 8)
                {
                    tex.SetPixel(x, y, color);
                }
            }
        }

        tex.Apply();
        return tex;
    }
}
