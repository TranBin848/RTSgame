using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteHitFlash : MonoBehaviour
{
    const float Duration = 0.15f;
    SpriteRenderer m_Renderer;
    Material m_NormalMaterial;
    Material m_FlashMaterial;
    Coroutine m_Routine;
    bool m_Flashing;
    float m_LastPlayedTime = float.NegativeInfinity;

    public bool PlayedRecently => Time.time - m_LastPlayedTime < 0.5f;

    void Awake() { m_Renderer = GetComponent<SpriteRenderer>(); }

    public void Play()
    {
        if (m_FlashMaterial == null)
        {
            var shader = Shader.Find("Custom/SpriteFlash");
            if (shader == null) return;
            m_FlashMaterial = new Material(shader);
            m_FlashMaterial.SetColor("_FlashColor", Color.white);
        }
        if (m_Routine != null) { StopCoroutine(m_Routine); Restore(); }
        m_NormalMaterial = m_Renderer.sharedMaterial;
        m_Flashing = true;
        m_Renderer.sharedMaterial = m_FlashMaterial;
        m_FlashMaterial.SetFloat("_FlashAmount", 1f);
        m_LastPlayedTime = Time.time;
        m_Routine = StartCoroutine(Fade());
    }

    IEnumerator Fade()
    {
        float elapsed = 0f;
        while (elapsed < Duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            m_FlashMaterial.SetFloat("_FlashAmount", Mathf.Clamp01(1f - elapsed / Duration));
        }
        Restore();
    }

    void OnDisable()
    {
        if (m_Routine != null) StopCoroutine(m_Routine);
        Restore();
    }

    void OnDestroy()
    {
        if (m_FlashMaterial != null) Destroy(m_FlashMaterial);
    }

    void Restore()
    {
        if (m_Flashing && m_Renderer != null) m_Renderer.sharedMaterial = m_NormalMaterial;
        m_Flashing = false;
        m_NormalMaterial = null;
        m_Routine = null;
    }
}
