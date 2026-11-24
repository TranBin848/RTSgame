using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ExpansionHud : MonoBehaviour
{
    ExpansionCommandService m_Service;
    GameManager m_Game;
    Camera m_Camera;
    GameObject m_Root;
    TextMeshProUGUI m_Preview, m_Result;
    float m_NextRefresh;
    public string PreviewText => m_Preview != null ? m_Preview.text : "";
    public string ResultText => m_Result != null ? m_Result.text : "";
    public void Initialize(ExpansionCommandService service, GameManager game, Camera camera, Canvas canvas)
    {
        if (service == null || game == null || camera == null || canvas == null) throw new ArgumentException("Expansion HUD requires its scene context.");
        if (m_Service != null) { if (m_Service != service) throw new InvalidOperationException("Expansion HUD already bound."); return; }
        m_Service = service; m_Game = game; m_Camera = camera;
        var root = Rect("Expansion HUD", canvas.transform, new Vector2(490, 48), new Vector2(270, -176));
        root.anchorMin = root.anchorMax = new Vector2(0, 1); m_Root = root.gameObject;
        AddButton("GỌI DÂN VỀ", new Vector2(-120, 0), Recall);
        AddButton("TẬP KẾT QUÂN", new Vector2(120, 0), Rally);
        m_Result = Text("Group result", new Vector2(490, 50), new Vector2(0, -50), 18);
        m_Result.text = "Quân: quanh tâm màn hình · Dân: trú đến bình minh";
        m_Preview = Text("Logistics preview", new Vector2(490, 260), new Vector2(0, -205), 19);
        var panel = Rect("Preview background", root, new Vector2(510, 270), new Vector2(0, -205)); panel.SetAsFirstSibling();
        panel.gameObject.AddComponent<Image>().color = new Color(.02f, .04f, .06f, .94f);
        m_Preview.rectTransform.SetParent(panel, false); m_Preview.rectTransform.anchoredPosition = Vector2.zero;
        panel.gameObject.SetActive(false);
        Refresh();
    }
    RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 pos)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false); r.sizeDelta = size; r.anchoredPosition = pos; return r;
    }
    TextMeshProUGUI Text(string name, Vector2 size, Vector2 pos, float font)
    {
        var t = Rect(name, m_Root.transform, size, pos).gameObject.AddComponent<TextMeshProUGUI>(); t.font = TMP_Settings.defaultFontAsset; t.fontSize = font;
        t.color = new Color(.9f, .95f, 1); t.alignment = TextAlignmentOptions.TopLeft; t.raycastTarget = false; t.text = ""; return t;
    }
    void AddButton(string title, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        var r = Rect(title, m_Root.transform, new Vector2(228, 40), pos); r.gameObject.AddComponent<Image>().color = new Color(.1f, .2f, .23f, .95f);
        r.gameObject.AddComponent<Button>().onClick.AddListener(action);
        var label = Text(title, r.sizeDelta, Vector2.zero, 19); label.rectTransform.SetParent(r, false); label.alignment = TextAlignmentOptions.Center; label.text = title;
    }
    public void Recall() => m_Result.text = "Trú đến bình minh · " + m_Service.RecallWorkers();
    public void Rally() => m_Result.text = "Quanh tâm ±2 ô · " + m_Service.RallyArmy(m_Camera.transform.position);
    void Update() { if (m_Service != null && Time.unscaledTime >= m_NextRefresh) { m_NextRefresh = Time.unscaledTime + .35f; Refresh(); } }
    public void Refresh()
    {
        if (m_Service == null) return;
        m_Preview.transform.parent.gameObject.SetActive(m_Game.CurrentPlacement != null);
        if (m_Game.CurrentPlacement != null) m_Preview.text = m_Service.Preview(m_Game.CurrentPlacement);
    }
    void OnDestroy() { if (m_Root != null) Destroy(m_Root); }
}
