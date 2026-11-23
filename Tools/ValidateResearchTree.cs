using System;
using System.Linq;
using System.Reflection;
using RTS.DOTS;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class ResearchTreeValidation
{
    static void Check(bool condition, string message)
    { if (!condition) throw new Exception("Research tree: " + message); }
    public static string Run()
    {
        Check(Application.isPlaying, "run in a fresh ProceduralGame Play Mode match.");
        var root = UnityEngine.Object.FindFirstObjectByType<GameSceneCompositionRoot>();
        var game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var tree = game.ResearchTree;
        var camera = UnityEngine.Object.FindFirstObjectByType<CameraController>();
        var view = tree.Viewport;
        var events = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        Check(root.IsReady && tree != null && events != null, "dependencies not ready.");
        Check(root.Research.State.UnlockMask == 0 && root.Research.State.Active == 0, "expected fresh research state.");
        Check(!tree.IsOpen, "overlay must start closed.");
        Check(!UnityEngine.Object.FindObjectsByType<ResearchPanel>(FindObjectsSortMode.None).Any(), "legacy panel still active.");
        var cameraPosition = camera.ViewCamera.transform.position;
        float cameraSize = camera.ViewCamera.orthographicSize, timeScale = Time.timeScale;
        bool previousLock = camera.LockCamera;
        tree.Initialize(root.Research, game, camera);
        tree.Initialize(root.Research, game, camera);
        tree.Open(); tree.Open();
        try
        {
            Check(tree.IsOpen && tree.BlocksGameplayInput && camera.LockCamera, "modal input or camera lock failed.");
            Check(Time.timeScale == timeScale, "overlay changed simulation speed.");
            Check(tree.GetComponents<Canvas>().Length == 1, "repeat initialization duplicated the canvas.");
            var ids = new[] { "economy_tools", "economy_cargo", "military_archery", "military_training", "expansion_tower", "expansion_fortify" };
            Check(view.Content.GetComponentsInChildren<Button>().Length == 6, "expected exactly six research nodes.");
            foreach (var id in ids) Check(view.Content.Find(id) != null, "missing node " + id);
            int wood = game.Wood, gold = game.Gold, meat = game.Meat;
            tree.SelectTechnology("economy_cargo");
            Check(tree.ResearchSelected() == ResearchResult.MissingPrerequisite, "prerequisite bypassed.");
            Check(game.Wood == wood && game.Gold == gold && game.Meat == meat, "rejected research debited wallet.");

            view.ResetView();
            var panField = typeof(ResearchTreeViewport).GetField("m_Position", BindingFlags.Instance | BindingFlags.NonPublic);
            var before = (Vector2)panField.GetValue(view);
            var pivot = new Vector2(130, -70);
            var contentPoint = (pivot - before) / view.Zoom;
            view.SetZoom(1.1f, pivot);
            var after = (Vector2)panField.GetValue(view);
            Check(Vector2.Distance((pivot - after) / view.Zoom, contentPoint) < .001f, "zoom lost its cursor anchor.");
            view.SetZoom(999, Vector2.zero); Check(view.Zoom == ResearchTreeViewport.MaxZoom, "maximum zoom not clamped.");
            view.SetZoom(0, Vector2.zero); Check(view.Zoom == ResearchTreeViewport.MinZoom, "minimum zoom not clamped.");
            view.SetZoom(float.NaN, Vector2.zero); Check(!float.IsNaN(view.Zoom), "NaN zoom accepted.");
            view.ResetView();
            Canvas.ForceUpdateCanvases();
            var center = RectTransformUtility.WorldToScreenPoint(null, view.transform.position);
            var pointer = new PointerEventData(events) { button = PointerEventData.InputButton.Left, position = center, eligibleForClick = true };
            before = view.Content.anchoredPosition;
            view.OnBeginDrag(pointer); pointer.position += new Vector2(100, 45); view.OnDrag(pointer); view.OnEndDrag(pointer);
            Check(Vector2.Distance(view.Content.anchoredPosition, before) > 10, "drag did not move tree.");
            Check(!pointer.eligibleForClick && !view.IsDragging, "drag could buy a node or stayed captured.");
            camera.SendMessage("LateUpdate");
            Check(camera.ViewCamera.transform.position == cameraPosition && camera.ViewCamera.orthographicSize == cameraSize, "tree moved the world camera.");
            view.ResetView();
            tree.SelectTechnology("economy_tools");
            game.ActiveUnit = null;
            var definition = root.Research.GetDefinition("economy_tools");
            Check(tree.ResearchSelected() == ResearchResult.Started, "research could not choose a valid Hall without changing selection.");
            Check(game.Wood == wood - definition.Wood && game.Gold == gold - definition.Gold && game.Meat == meat - definition.Meat, "incorrect research debit.");
            wood = game.Wood; gold = game.Gold;
            Check(tree.ResearchSelected() == ResearchResult.Busy && game.Wood == wood && game.Gold == gold, "duplicate research debited twice.");
        }
        finally { tree.Close(); }
        Check(!tree.IsOpen && camera.LockCamera == previousLock && Time.timeScale == timeScale, "close did not restore camera/time.");
        Check(tree.BlocksGameplayInput, "closing frame could leak Escape/click into gameplay.");
        for (int i = 0; i < 3; i++) { tree.Open(); tree.Close(); }
        return "PASS: six nodes, legacy hidden, repeat Init/Open, modal input, camera/time, prerequisite/no debit, cursor zoom/clamps/NaN, drag/no click, automatic Hall, one debit/busy, repeated close/reopen. Exit Play Mode after this fixture.";
    }
}
