using System;
using System.Collections.Generic;
using RTS.DOTS;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-50), DisallowMultipleComponent]
public sealed class ResearchTreePanel : MonoBehaviour
{
    [SerializeField] Sprite m_RootIcon, m_WorkerIcon, m_CargoIcon, m_ArcherIcon, m_TrainingIcon, m_TowerIcon, m_FortifyIcon;
    static readonly Color Ink = new(.025f, .035f, .055f), Surface = new(.045f, .065f, .09f);
    static readonly Color White = new(.91f, .95f, .98f), Muted = new(.46f, .56f, .65f);
    static readonly Color Gold = new(1, .77f, .35f), Green = new(.36f, 1, .64f);
    static readonly Color Blue = new(.32f, .76f, 1), Purple = new(.76f, .54f, 1);
    IResearchService m_Research;
    GameManager m_Game;
    CameraController m_Camera;
    bool m_PreviousCameraLock;
    GameObject m_Overlay;
    ResearchTreeViewport m_Viewport;
    Text m_Wallet, m_Overview, m_DetailTitle, m_DetailEffect, m_DetailStatus, m_Cost, m_ActionLabel, m_ProgressLabel;
    Image m_Progress, m_DetailIcon;
    Button m_ResearchButton, m_Launcher;
    Font m_Font;
    Sprite m_ProgressSprite;
    Node m_Selected;
    string m_Feedback;
    float m_NextRefresh;
    int m_ClosedFrame = -1;
    readonly List<Node> m_Nodes = new();
    public bool IsOpen => m_Overlay != null && m_Overlay.activeSelf;
    public bool BlocksGameplayInput => IsOpen || Time.frameCount == m_ClosedFrame;
    public ResearchTreeViewport Viewport => m_Viewport;

    sealed class Node
    {
        public string Id, Title, Effect;
        public Color Tint;
        public ResearchDefinition Definition;
        public ResearchTreeGraphic Frame, Link;
        public Image Icon;
        public Text State;
        public Button Button;
    }

    public void Initialize(IResearchService research, GameManager game, CameraController camera)
    {
        if (research == null || game == null || camera == null) throw new ArgumentException("Research tree requires its scene research, game and camera.");
        if (m_Research != null)
        {
            if (m_Research != research || m_Game != game || m_Camera != camera) throw new InvalidOperationException("Research tree already initialized.");
            return;
        }
        m_Research = research; m_Game = game; m_Camera = camera;
        m_Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildView();
        Select(m_Nodes[0]);
        m_Overlay.SetActive(false);
    }

    public void Toggle() { if (IsOpen) Close(); else Open(); }
    public void Open()
    {
        if (m_Research == null || IsOpen || m_Game.IsMatchOver || ProceduralMapRuntime.IsCompositionBlocked || Time.timeScale <= 0) return;
        m_Game.CancelBuildProcess();
        m_Game.ResetSelectionGesture();
        m_PreviousCameraLock = m_Camera.LockCamera;
        m_Camera.LockCamera = true;
        m_Overlay.SetActive(true);
        m_Feedback = null;
        Refresh();
        // The first open fits the tree after CanvasScaler has resolved the viewport.
        if (m_Viewport.Content.localScale == Vector3.zero) m_Viewport.ResetView();
    }
    public void Close()
    {
        if (!IsOpen) return;
        m_Overlay.SetActive(false);
        if (m_Camera != null) m_Camera.LockCamera = m_PreviousCameraLock;
        m_ClosedFrame = Time.frameCount;
        if (m_Game != null) m_Game.ResetSelectionGesture();
    }
    void OnDisable() => Close();
    void Update()
    {
        if (m_Research == null) return;
        if (m_Game.IsMatchOver) { Close(); m_Launcher.interactable = false; return; }
        if (Input.GetKeyDown(KeyCode.T)) Toggle();
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        if (Input.GetKeyDown(KeyCode.Home)) m_Viewport.ResetView();
        if (Time.unscaledTime < m_NextRefresh) return;
        m_NextRefresh = Time.unscaledTime + .1f;
        Refresh();
    }

    TownHall ResearchHall()
    {
        if (m_Game.ActiveUnit is TownHall selected && CanUse(selected)) return selected;
        foreach (var structure in m_Game.GetPlayerStructures()) if (structure is TownHall hall && CanUse(hall)) return hall;
        return null;
    }
    static bool CanUse(TownHall hall) => hall != null && hall.isActiveAndEnabled && hall.IsDotsControlled
        && hall.IsPlayer && hall.CurrentHealth > 0 && hall.CurrentState != UnitState.Dead && !hall.isUnderConstruction;
    bool Affordable(ResearchDefinition d) => m_Game.Wood >= d.Wood && m_Game.Gold >= d.Gold && m_Game.Meat >= d.Meat;
    bool Prerequisite(Node node) => string.IsNullOrEmpty(node.Definition.Prerequisite) || m_Research.IsUnlocked(node.Definition.Prerequisite);

    public void SelectTechnology(string id)
    {
        var node = m_Nodes.Find(n => n.Id == id);
        if (node == null) throw new ArgumentException("Unknown research node.", nameof(id));
        Select(node);
    }
    void Select(Node node)
    {
        m_Selected = node; m_Feedback = null;
        m_DetailTitle.text = node.Title;
        m_DetailEffect.text = node.Effect;
        m_DetailIcon.sprite = node.Icon.sprite;
        m_DetailIcon.color = node.Icon.sprite != null ? Color.white : Color.clear;
        m_Cost.text = $"GỖ  {node.Definition.Wood}     VÀNG  {node.Definition.Gold}\nTHỊT  {node.Definition.Meat}       {node.Definition.Duration:0} GIÂY";
        Refresh();
    }
    public ResearchResult ResearchSelected()
    {
        if (!IsOpen || m_Selected == null) return ResearchResult.Unavailable;
        var result = m_Research.Start(m_Selected.Id, ResearchHall());
        m_Feedback = result switch
        {
            ResearchResult.Started => "Đã bắt đầu nghiên cứu.",
            ResearchResult.InsufficientResources => "Chưa đủ tài nguyên.",
            ResearchResult.MissingPrerequisite => "Cần nghiên cứu công nghệ trước đó.",
            ResearchResult.Busy => "Một công nghệ khác đang được nghiên cứu.",
            ResearchResult.AlreadyUnlocked => "Công nghệ đã hoàn thành.",
            ResearchResult.InvalidTownHall => "Cần TownHall còn sống và đã xây xong.",
            _ => "Hiện chưa thể nghiên cứu."
        };
        Refresh();
        return result;
    }
    void Refresh()
    {
        if (m_Selected == null) return;
        var state = m_Research.State;
        bool busy = state.Active != 0;
        string active = busy ? m_Research.ActiveTechnology : null;
        int count = 0;
        foreach (var node in m_Nodes)
        {
            bool done = m_Research.IsUnlocked(node.Id), unlockedPath = Prerequisite(node), researching = active == node.Id;
            if (done) count++;
            Color tint = done ? node.Tint : researching ? Gold : unlockedPath ? node.Tint * .8f : new Color(.23f, .28f, .34f);
            tint.a = 1;
            if (node == m_Selected) tint = Color.Lerp(tint, Color.white, .3f);
            node.Frame.color = tint;
            node.Link.color = done ? node.Tint : unlockedPath ? new Color(node.Tint.r, node.Tint.g, node.Tint.b, .45f) : new Color(.22f, .27f, .34f, .65f);
            node.Icon.color = done || unlockedPath ? Color.white : new Color(.4f, .45f, .5f);
            string status = done ? "ĐÃ MỞ" : researching ? "ĐANG NGHIÊN CỨU" : !unlockedPath ? "CHƯA MỞ KHÓA" : "CÓ THỂ NGHIÊN CỨU";
            if (node.State.text != status) node.State.text = status;
            node.State.color = done ? node.Tint : researching ? Gold : Muted;
        }
        string wallet = $"GỖ  {m_Game.Wood}       VÀNG  {m_Game.Gold}       THỊT  {m_Game.Meat}";
        if (m_Wallet.text != wallet) m_Wallet.text = wallet;
        m_Overview.text = $"{count} / 6 CÔNG NGHỆ ĐÃ MỞ";
        bool selectedDone = m_Research.IsUnlocked(m_Selected.Id), path = Prerequisite(m_Selected), enough = Affordable(m_Selected.Definition);
        bool selectedActive = active == m_Selected.Id;
        float progress = selectedActive ? Mathf.Clamp01(state.Elapsed / m_Research.Duration) : selectedDone ? 1 : 0;
        m_Progress.fillAmount = progress;
        string progressText = selectedActive ? $"{state.Elapsed:0.0} / {m_Research.Duration:0} giây" : selectedDone ? "NGHIÊN CỨU HOÀN TẤT" : "CHƯA NGHIÊN CỨU";
        if (m_ProgressLabel.text != progressText) m_ProgressLabel.text = progressText;
        string required = path ? "" : $"Yêu cầu: {Title(m_Selected.Definition.Prerequisite)}.\n";
        string statusText = m_Feedback ?? (selectedDone ? "Hiệu ứng đã áp dụng trong trận này." : selectedActive ? "TownHall đang nghiên cứu công nghệ này."
            : required + (busy ? $"Đang nghiên cứu {Title(active)}." : !enough ? "Chưa đủ tài nguyên." : "Sẵn sàng nghiên cứu tại TownHall."));
        if (m_DetailStatus.text != statusText) m_DetailStatus.text = statusText;
        bool ready = !ProceduralMapRuntime.IsCompositionBlocked && !m_Game.IsMatchOver && Time.timeScale > 0;
        m_ResearchButton.interactable = ready && !selectedDone && path && enough && !busy && ResearchHall() != null;
        m_ActionLabel.text = selectedDone ? "ĐÃ NGHIÊN CỨU" : selectedActive ? "ĐANG NGHIÊN CỨU..." : !path ? "CẦN CÔNG NGHỆ TRƯỚC" : busy ? "ĐANG BẬN" : !enough ? "THIẾU TÀI NGUYÊN" : "NGHIÊN CỨU";
    }
    static string Title(string id) => id switch { "economy_tools" => "Công cụ", "economy_cargo" => "Vận chuyển", "military_archery" => "Thuật bắn cung",
        "military_training" => "Huấn luyện", "expansion_tower" => "Tiền đồn", "expansion_fortify" => "Gia cố", _ => "" };

    void BuildView()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.overrideSorting = true; canvas.sortingOrder = 3000;
        var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        gameObject.AddComponent<GraphicRaycaster>();
        m_Launcher = MakeButton(transform, "Open research tree", "CÔNG NGHỆ   [T]", Vector2.zero, new Vector2(260, 58), Gold, Toggle);
        var launchRect = (RectTransform)m_Launcher.transform;
        launchRect.anchorMin = launchRect.anchorMax = new Vector2(0, 1); launchRect.pivot = new Vector2(0, 1);
        launchRect.anchoredPosition = new Vector2(26, -26);

        m_Overlay = Rect("Technology overlay", transform, Vector2.zero, Vector2.zero).gameObject;
        Stretch((RectTransform)m_Overlay.transform, Vector2.zero, Vector2.zero);
        var background = m_Overlay.AddComponent<Image>(); background.color = Ink;
        var atmosphere = Rect("Atmosphere", m_Overlay.transform, Vector2.zero, Vector2.zero);
        Stretch(atmosphere, Vector2.zero, Vector2.zero);
        var backdrop = atmosphere.gameObject.AddComponent<ResearchTreeGraphic>();
        backdrop.Form = ResearchTreeGraphic.Shape.Backdrop; backdrop.raycastTarget = false;

        var viewport = Rect("Viewport", m_Overlay.transform, Vector2.zero, Vector2.zero);
        Stretch(viewport, new Vector2(20, 110), new Vector2(-410, -125));
        viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .01f);
        viewport.gameObject.AddComponent<RectMask2D>();
        m_Viewport = viewport.gameObject.AddComponent<ResearchTreeViewport>();
        var content = Rect("Tree content", viewport, Vector2.zero, new Vector2(2500, 1700));
        content.localScale = Vector3.zero;
        m_Viewport.Content = content;
        BuildGraph(content);
        BuildHeader();
        BuildDetail();

        var controls = Rect("Navigation controls", m_Overlay.transform, Vector2.zero, new Vector2(840, 60));
        controls.anchorMin = controls.anchorMax = new Vector2(0, 0); controls.pivot = Vector2.zero; controls.anchoredPosition = new Vector2(36, 26);
        MakeText(controls, "Kéo để di chuyển    /    Cuộn để zoom    /    Esc để đóng", new Vector2(400, 18), new Vector2(800, 35), 20, Muted, TextAnchor.MiddleLeft);
        var center = MakeButton(m_Overlay.transform, "Center tree", "VỀ TRUNG TÂM", Vector2.zero, new Vector2(200, 46), Muted, m_Viewport.ResetView);
        var centerRect = (RectTransform)center.transform; centerRect.anchorMin = centerRect.anchorMax = Vector2.zero;
        centerRect.pivot = Vector2.zero; centerRect.anchoredPosition = new Vector2(36, 78);
        var zoomOut = MakeButton(m_Overlay.transform, "Zoom out", "−", Vector2.zero, new Vector2(50, 46), Muted, () => m_Viewport.SetZoom(m_Viewport.Zoom / 1.2f, Vector2.zero));
        AnchorControl(zoomOut, 252);
        var zoomIn = MakeButton(m_Overlay.transform, "Zoom in", "+", Vector2.zero, new Vector2(50, 46), Muted, () => m_Viewport.SetZoom(m_Viewport.Zoom * 1.2f, Vector2.zero));
        AnchorControl(zoomIn, 314);
    }
    static void AnchorControl(Button button, float x)
    {
        var r = (RectTransform)button.transform; r.anchorMin = r.anchorMax = Vector2.zero; r.pivot = Vector2.zero; r.anchoredPosition = new Vector2(x, 78);
    }

    void BuildGraph(RectTransform content)
    {
        Vector2 root = new(0, 70), economy = new(-335, 165), military = new(335, 165), expansion = new(0, -210);
        Link(content, root, economy, Green * .6f); Link(content, root, military, Blue * .6f); Link(content, root, expansion, Purple * .6f);
        Core(content, root, "THỊ TRẤN", "KHỞI NGUỒN PHÁT TRIỂN", Gold, m_RootIcon, 162);
        Core(content, economy, "KINH TẾ", "Khai thác & hậu cần", Green, m_WorkerIcon);
        Core(content, military, "QUÂN SỰ", "Đơn vị & huấn luyện", Blue, m_TrainingIcon);
        Core(content, expansion, "MỞ RỘNG", "Công trình & phòng thủ", Purple, m_TowerIcon);
        var tools = new Vector2(-600, 285); var cargo = new Vector2(-715, -20);
        var archery = new Vector2(600, 285); var training = new Vector2(715, -20);
        var tower = new Vector2(-235, -410); var fortify = new Vector2(235, -410);
        AddNode(content, "economy_tools", "Gỗ mỗi nhịp ×1,25\nTăng lượng gỗ dân khai thác trong mỗi chu kỳ.", tools, economy, Green, m_WorkerIcon);
        AddNode(content, "economy_cargo", "Sức chứa dân ×1,5\nÁp dụng cho dân hiện có và dân được huấn luyện sau này.", cargo, tools, Green, m_CargoIcon);
        AddNode(content, "military_archery", "Mở xây ArcheryRange và huấn luyện Archer.", archery, military, Blue, m_ArcherIcon);
        AddNode(content, "military_training", "Thời gian huấn luyện ×0,8\nÁp dụng cho lệnh mới. Hàng đợi cũ giữ thời gian ban đầu.", training, archery, Blue, m_TrainingIcon);
        AddNode(content, "expansion_tower", "Mở xây Tower để bảo vệ căn cứ và các tuyến mở rộng.", tower, expansion, Purple, m_TowerIcon);
        AddNode(content, "expansion_fortify", "Damage đạn tháp ×1,25\nÁp dụng cho đạn bắn sau khi nghiên cứu hoàn thành.", fortify, tower, Purple, m_FortifyIcon);
    }
    void Core(RectTransform parent, Vector2 position, string title, string subtitle, Color tint, Sprite icon, float size = 100)
    {
        var rect = Rect(title, parent, position, new Vector2(size, size));
        var shape = rect.gameObject.AddComponent<ResearchTreeGraphic>(); shape.Form = ResearchTreeGraphic.Shape.Core; shape.color = tint; shape.raycastTarget = false;
        Icon(rect, icon, Vector2.zero, new Vector2(size * .5f, size * .5f));
        MakeText(rect, title, new Vector2(0, -size * .62f), new Vector2(310, 34), size > 100 ? 27 : 24, tint);
        MakeText(rect, subtitle, new Vector2(0, -size * .62f - 30), new Vector2(310, 30), 17, Muted);
    }
    void AddNode(RectTransform parent, string id, string effect, Vector2 position, Vector2 from, Color tint, Sprite icon)
    {
        var node = new Node { Id = id, Title = Title(id), Tint = tint, Definition = m_Research.GetDefinition(id), Effect = effect };
        node.Link = Link(parent, from, position, tint);
        // Connections stay behind icons even when a later node adds another edge.
        node.Link.transform.SetAsFirstSibling();
        var rect = Rect(id, parent, position, new Vector2(105, 105));
        var hit = rect.gameObject.AddComponent<Image>(); hit.color = new Color(.03f, .05f, .075f, .96f);
        node.Button = rect.gameObject.AddComponent<Button>(); node.Button.targetGraphic = hit;
        var palette = node.Button.colors; palette.normalColor = Color.white; palette.highlightedColor = new Color(1.6f, 1.6f, 1.6f);
        palette.pressedColor = new Color(.6f, .7f, .8f); node.Button.colors = palette;
        var frame = Rect("Glow frame", rect, Vector2.zero, new Vector2(130, 130));
        node.Frame = frame.gameObject.AddComponent<ResearchTreeGraphic>(); node.Frame.color = tint; node.Frame.raycastTarget = false;
        node.Icon = Icon(rect, icon, Vector2.zero, new Vector2(60, 60));
        MakeText(rect, node.Title, new Vector2(0, -82), new Vector2(275, 38), 25, White);
        node.State = MakeText(rect, "", new Vector2(0, -113), new Vector2(285, 30), 15, Muted);
        node.Button.onClick.AddListener(() => Select(node));
        m_Nodes.Add(node);
    }
    ResearchTreeGraphic Link(RectTransform parent, Vector2 from, Vector2 to, Color tint)
    {
        var rect = Rect("Connection", parent, Vector2.zero, parent.sizeDelta);
        var line = rect.gameObject.AddComponent<ResearchTreeGraphic>();
        line.Form = ResearchTreeGraphic.Shape.Link; line.From = from; line.To = to; line.color = tint; line.raycastTarget = false;
        return line;
    }
    void BuildHeader()
    {
        var bar = Rect("Header", m_Overlay.transform, Vector2.zero, Vector2.zero);
        bar.anchorMin = new Vector2(0, 1); bar.anchorMax = Vector2.one; bar.pivot = new Vector2(.5f, 1);
        bar.sizeDelta = new Vector2(0, 118);
        bar.gameObject.AddComponent<Image>().color = new Color(.035f, .05f, .075f, .98f);
        var title = MakeText(bar, "CÂY CÔNG NGHỆ", Vector2.zero, new Vector2(500, 50), 34, White, TextAnchor.MiddleLeft);
        var r = (RectTransform)title.transform; r.anchorMin = r.anchorMax = new Vector2(0, .5f); r.anchoredPosition = new Vector2(290, 13);
        m_Overview = MakeText(bar, "", Vector2.zero, new Vector2(490, 28), 18, Gold, TextAnchor.MiddleLeft);
        r = (RectTransform)m_Overview.transform; r.anchorMin = r.anchorMax = new Vector2(0, .5f); r.anchoredPosition = new Vector2(285, -25);
        m_Wallet = MakeText(bar, "", Vector2.zero, new Vector2(740, 40), 24, Gold, TextAnchor.MiddleRight);
        r = (RectTransform)m_Wallet.transform; r.anchorMin = r.anchorMax = new Vector2(1, .5f); r.anchoredPosition = new Vector2(-650, 0);
        var close = MakeButton(bar, "Close tree", "ĐÓNG   [Esc]", Vector2.zero, new Vector2(185, 50), Muted, Close);
        r = (RectTransform)close.transform; r.anchorMin = r.anchorMax = new Vector2(1, .5f); r.anchoredPosition = new Vector2(-130, 0);
    }
    void BuildDetail()
    {
        var pane = Rect("Technology details", m_Overlay.transform, Vector2.zero, Vector2.zero);
        pane.anchorMin = new Vector2(1, 0); pane.anchorMax = Vector2.one;
        pane.pivot = new Vector2(1, .5f); pane.sizeDelta = new Vector2(360, -190); pane.anchoredPosition = new Vector2(-28, -25);
        pane.gameObject.AddComponent<Image>().color = Surface;
        var top = Rect("Details content", pane, Vector2.zero, new Vector2(360, 0));
        top.anchorMin = top.anchorMax = new Vector2(.5f, 1); top.pivot = new Vector2(.5f, 1); top.anchoredPosition = new Vector2(0, -22);
        MakeText(top, "CÔNG NGHỆ ĐƯỢC CHỌN", new Vector2(0, -25), new Vector2(320, 35), 17, Muted);
        var haloRect = Rect("Detail halo", top, new Vector2(0, -112), new Vector2(150, 150));
        var halo = haloRect.gameObject.AddComponent<ResearchTreeGraphic>(); halo.Form = ResearchTreeGraphic.Shape.Core; halo.color = Gold; halo.raycastTarget = false;
        m_DetailIcon = Icon(top, null, new Vector2(0, -112), new Vector2(70, 70));
        m_DetailTitle = MakeText(top, "", new Vector2(0, -212), new Vector2(315, 65), 30, White);
        m_DetailEffect = MakeText(top, "", new Vector2(0, -315), new Vector2(306, 135), 22, White);
        m_Cost = MakeText(top, "", new Vector2(0, -435), new Vector2(306, 76), 20, Gold);
        var track = Rect("Research progress track", top, new Vector2(0, -500), new Vector2(300, 6));
        track.gameObject.AddComponent<Image>().color = new Color(.15f, .2f, .25f);
        var fill = Rect("Research progress", track, Vector2.zero, Vector2.zero); Stretch(fill, Vector2.zero, Vector2.zero);
        m_Progress = fill.gameObject.AddComponent<Image>(); m_Progress.color = Green;
        m_ProgressSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f);
        m_Progress.sprite = m_ProgressSprite;
        m_Progress.type = Image.Type.Filled; m_Progress.fillMethod = Image.FillMethod.Horizontal;
        m_ProgressLabel = MakeText(top, "", new Vector2(0, -530), new Vector2(310, 35), 17, Muted);
        m_DetailStatus = MakeText(top, "", new Vector2(0, -595), new Vector2(306, 95), 20, Muted);
        m_ResearchButton = MakeButton(top, "Research selected technology", "NGHIÊN CỨU", new Vector2(0, -685), new Vector2(306, 58), Green, () => ResearchSelected());
        m_ActionLabel = m_ResearchButton.GetComponentInChildren<Text>();
        var note = MakeText(pane, "Trận đấu vẫn diễn ra khi mở cây.\nTownHall mất: hủy nghiên cứu, không hoàn tiền.", Vector2.zero, new Vector2(316, 65), 17, Muted);
        var noteRect = (RectTransform)note.transform; noteRect.anchorMin = noteRect.anchorMax = new Vector2(.5f, 0); noteRect.anchoredPosition = new Vector2(0, 47);
    }

    static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
    }
    static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = min; rect.offsetMax = max; }
    Text MakeText(Transform parent, string value, Vector2 position, Vector2 size, int fontSize, Color tint, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        var label = Rect("Label", parent, position, size).gameObject.AddComponent<Text>();
        label.font = m_Font; label.fontSize = fontSize; label.color = tint; label.alignment = alignment;
        label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
        label.raycastTarget = false; label.text = value; return label;
    }
    static Image Icon(Transform parent, Sprite sprite, Vector2 position, Vector2 size)
    {
        var icon = Rect("Icon", parent, position, size).gameObject.AddComponent<Image>();
        icon.sprite = sprite; icon.preserveAspect = true; icon.raycastTarget = false;
        icon.color = sprite != null ? Color.white : Color.clear; return icon;
    }
    Button MakeButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color tint, UnityEngine.Events.UnityAction action)
    {
        var rect = Rect(name, parent, position, size);
        var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.08f, .12f, .16f, 1);
        var outline = rect.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(tint.r, tint.g, tint.b, .5f); outline.effectDistance = new Vector2(1, -1);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
        MakeText(rect, label, Vector2.zero, size - new Vector2(12, 4), 20, tint);
        return button;
    }
    void OnDestroy()
    {
        Close();
        if (m_ProgressSprite != null) Destroy(m_ProgressSprite);
    }
}
