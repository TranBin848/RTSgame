using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DawnReportPresentation : MonoBehaviour
{
    MatchReportService m_Reports;
    GameObject m_Root, m_Panel, m_Banner;
    TextMeshProUGUI m_Title, m_Body, m_Dawn;
    AudioSource m_Audio;
    AudioClip m_Chime;
    float m_BannerRemaining;
    public bool IsOpen => m_Panel != null && m_Panel.activeInHierarchy;
    public int DawnSignalCount { get; private set; }
    public string DisplayedText => m_Body != null ? m_Body.text : "";
    public bool IsInitialized => m_Reports != null;

    public void Initialize(MatchReportService reports, Canvas canvas)
    {
        if (reports == null || canvas == null) throw new ArgumentException("Dawn report requires match reports and HUD.");
        if (IsInitialized)
        { if (m_Reports != reports) throw new InvalidOperationException("Report view belongs to another match."); return; }
        m_Reports = reports;
        m_Root = Rect("Dawn and reports", canvas.transform, Vector2.zero, Vector2.zero).gameObject;
        var rootRect = (RectTransform)m_Root.transform; rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
        var overlay = m_Root.AddComponent<Canvas>(); overlay.overrideSorting = true; overlay.sortingOrder = 3200;
        m_Root.AddComponent<GraphicRaycaster>();
        var launcher = Rect("Reopen report", m_Root.transform, new Vector2(260, 46), new Vector2(156, -112));
        launcher.anchorMin = launcher.anchorMax = new Vector2(0, 1);
        Button(launcher, "TỔNG KẾT", OpenLatest);
        var panel = Rect("Report panel", m_Root.transform, new Vector2(760, 540), new Vector2(406, -416));
        panel.anchorMin = panel.anchorMax = new Vector2(0, 1);
        m_Panel = panel.gameObject; panel.gameObject.AddComponent<Image>().color = new Color(.025f, .045f, .065f, .97f);
        m_Title = Label(panel, "Report title", new Vector2(700, 48), new Vector2(0, 221), 26);
        m_Body = Label(panel, "Report values", new Vector2(688, 280), new Vector2(0, -12), 23);
        m_Body.alignment = TextAlignmentOptions.TopLeft; m_Body.textWrappingMode = TextWrappingModes.Normal;
        var note = Label(panel, "Report scope", new Vector2(700, 60), new Vector2(0, -196), 17);
        note.text = "Chu kỳ từ đầu ngày đến bình minh kế tiếp.\nTrận đấu tiếp tục khi mở bảng; số liệu chốt tại lúc mở.";
        Button(Rect("Latest cycle", panel, new Vector2(210, 40), new Vector2(-220, 162)), "ĐÊM VỪA QUA", OpenLatest);
        Button(Rect("Match total", panel, new Vector2(210, 40), new Vector2(0, 162)), "TỔNG TRẬN", OpenTotal);
        Button(Rect("Close report", panel, new Vector2(130, 40), new Vector2(260, 162)), "ĐÓNG", Close);
        m_Panel.SetActive(false);
        var banner = Rect("Dawn signal", m_Root.transform, new Vector2(650, 72), new Vector2(0, -145));
        banner.anchorMin = banner.anchorMax = new Vector2(.5f, 1);
        m_Banner = banner.gameObject;
        var backdrop = m_Banner.AddComponent<Image>(); backdrop.color = new Color(.12f, .10f, .035f, .94f); backdrop.raycastTarget = false;
        m_Dawn = Label(banner, "Dawn message", new Vector2(620, 64), Vector2.zero, 24); m_Dawn.color = new Color(1, .87f, .48f);
        m_Banner.SetActive(false);
        m_Chime = Chime(); m_Audio = m_Root.AddComponent<AudioSource>();
        m_Audio.playOnAwake = false; m_Audio.spatialBlend = 0; m_Audio.volume = .2f; m_Audio.clip = m_Chime;
        m_Reports.NightClosed += OnNightClosed;
    }
    void OnNightClosed()
    {
        DawnSignalCount++; m_BannerRemaining = 5;
        m_Dawn.text = $"BÌNH MINH · Đêm {m_Reports.CompletedNight} đã kết thúc\nMở TỔNG KẾT để xem kết quả";
        m_Banner.SetActive(true); if (isActiveAndEnabled && Time.timeScale > 0) m_Audio.Play();
        if (IsOpen) OpenLatest();
    }
    public void OpenLatest()
    {
        if (!IsInitialized) return;
        m_Title.text = m_Reports.CompletedNight > 0 ? $"TỔNG KẾT ĐÊM {m_Reports.CompletedNight}" : "CHƯA CÓ ĐÊM KẾT THÚC";
        m_Body.text = m_Reports.CompletedNight > 0 ? MatchReportText.Format(m_Reports.LatestNight) : "Báo cáo đầu tiên được chốt ở bình minh.\nChọn TỔNG TRẬN để xem số liệu hiện tại.";
        m_Panel.SetActive(true);
    }
    public void OpenTotal()
    {
        if (!IsInitialized) return;
        m_Title.text = "TỔNG TRẬN · TẠI LÚC MỞ BẢNG";
        m_Body.text = MatchReportText.Format(m_Reports.Total); m_Panel.SetActive(true);
    }
    public void Close() { if (m_Panel != null) m_Panel.SetActive(false); }
    void LateUpdate()
    {
        if (IsInitialized && m_Reports.IsFrozen) { m_Root.SetActive(false); if (m_Audio.isPlaying) m_Audio.Stop(); return; }
        if (!IsInitialized || m_BannerRemaining <= 0) return;
        m_BannerRemaining = Mathf.Max(0, m_BannerRemaining - Time.deltaTime);
        if (m_BannerRemaining == 0) m_Banner.SetActive(false);
    }
    static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
    }
    static TextMeshProUGUI Label(Transform parent, string name, Vector2 size, Vector2 position, float fontSize)
    {
        var text = Rect(name, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset; text.fontSize = fontSize; text.color = new Color(.9f, .94f, 1);
        text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; text.text = ""; return text;
    }
    static void Button(RectTransform rect, string title, UnityEngine.Events.UnityAction action)
    {
        rect.gameObject.AddComponent<Image>().color = new Color(.12f, .22f, .25f, 1);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.onClick.AddListener(action);
        Label(rect, title, rect.sizeDelta, Vector2.zero, 19).text = title;
    }
    static AudioClip Chime()
    {
        const int rate = 22050; var samples = new float[rate];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / rate;
            samples[i] = Mathf.Min(1, t / .012f) * Mathf.Exp(-5 * t)
                * (.5f * Mathf.Sin(2 * Mathf.PI * 660 * t) + .25f * Mathf.Sin(2 * Mathf.PI * 990 * t));
        }
        var clip = AudioClip.Create("Dawn chime (owned)", rate, 1, rate, false); clip.SetData(samples, 0); return clip;
    }
    void OnDestroy()
    {
        if (m_Reports != null) m_Reports.NightClosed -= OnNightClosed;
        if (m_Root != null) Destroy(m_Root);
        if (m_Chime != null) Destroy(m_Chime);
    }
}
