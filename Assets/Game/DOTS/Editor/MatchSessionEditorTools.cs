#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MatchSessionEditorTools
{
    public static void Configure(GameSceneCompositionRoot root)
    {
        var session = root.GetComponent<MatchSession>();
        if (session == null) session = root.gameObject.AddComponent<MatchSession>();
        var menu = Object.FindFirstObjectByType<GameFlowMenu>(FindObjectsInactive.Include);
        if (menu == null) throw new System.InvalidOperationException("Gameplay scene requires GameFlowMenu.");
        var existing = GameObject.Find("Match UI");
        var canvasObject = existing != null ? existing : new GameObject("Match UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 2000;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        var objective = Label(canvasObject.transform, "Objective", new Vector2(800, 44), new Vector2(0, 28));
        objective.rectTransform.anchorMin = objective.rectTransform.anchorMax = new Vector2(0.5f, 0);
        objective.text = "OBJECTIVE: Protect your TownHall through 5 nights"; objective.fontSize = 21;
        var panel = Child(canvasObject.transform, "Result", typeof(Image));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.07f, 0.94f);
        var title = Label(panel.transform, "Outcome", new Vector2(1100, 180), new Vector2(0, 125));
        title.fontSize = 40;
        var replay = Button(panel.transform, "Play again", new Vector2(0, -20));
        var mainMenu = Button(panel.transform, "Main menu", new Vector2(0, -110));
        var data = new SerializedObject(session);
        data.FindProperty("m_Menu").objectReferenceValue = menu;
        data.FindProperty("m_ResultPanel").objectReferenceValue = panel;
        data.FindProperty("m_ResultLabel").objectReferenceValue = title;
        data.FindProperty("m_ObjectiveLabel").objectReferenceValue = objective;
        data.FindProperty("m_ReplayButton").objectReferenceValue = replay;
        data.FindProperty("m_MenuButton").objectReferenceValue = mainMenu;
        data.ApplyModifiedPropertiesWithoutUndo();
        var refs = new SerializedObject(root);
        refs.FindProperty("m_Match").objectReferenceValue = session; refs.ApplyModifiedPropertiesWithoutUndo();
        panel.SetActive(false);
        EditorUtility.SetDirty(session); EditorUtility.SetDirty(root);
    }

    static GameObject Child(Transform parent, string name, params System.Type[] components)
    {
        var child = parent.Find(name);
        if (child != null) return child.gameObject;
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        foreach (var type in components) go.AddComponent(type);
        return go;
    }
    static Text Label(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var go = Child(parent, name, typeof(Text)); var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.sizeDelta = size; rect.anchoredPosition = position;
        var text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        return text;
    }
    static Button Button(Transform parent, string name, Vector2 position)
    {
        var go = Child(parent, name, typeof(Image), typeof(Button)); var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(340, 68); rect.anchoredPosition = position;
        go.GetComponent<Image>().color = new Color(0.15f, 0.3f, 0.38f);
        var label = Label(go.transform, "Label", new Vector2(320, 60), Vector2.zero);
        label.text = name; label.fontSize = 26;
        return go.GetComponent<Button>();
    }
}
#endif
