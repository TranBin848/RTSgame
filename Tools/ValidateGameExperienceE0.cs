// Run via script-execute in a fresh ProceduralGame match. Runtime probes are discarded on exit.
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public static class GameExperienceE0Validation
{
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static void Check(bool value, string message) { if (!value) throw new Exception("E0: " + message); }

    public static string Layout()
    {
        var tree = Find<GameManager>().ResearchTree;
        tree.Open(); tree.Viewport.ResetView(); Canvas.ForceUpdateCanvases();
        Check(tree.IsOpen, "tree must be open");
        var bounds = new Rect(0, 0, Screen.width, Screen.height);
        var corners = new Vector3[4];
        foreach (var button in tree.GetComponentsInChildren<Button>())
        {
            if (button.name == "Open research tree") continue;
            ((RectTransform)button.transform).GetWorldCorners(corners);
            Check(corners.All(p => p.x >= bounds.xMin - 1 && p.x <= bounds.xMax + 1
                && p.y >= bounds.yMin - 1 && p.y <= bounds.yMax + 1), "button outside screen: " + button.name);
        }
        foreach (var graphic in tree.GetComponentsInChildren<ResearchTreeGraphic>())
            Check(graphic.GetComponent<CanvasRenderer>() != null, "missing CanvasRenderer: " + graphic.name);
        foreach (var icon in tree.GetComponentsInChildren<Image>().Where(i => i.name == "Icon"))
            Check(icon.sprite != null, "missing icon: " + icon.transform.parent.name);
        foreach (var label in tree.GetComponentsInChildren<Text>())
        {
            if (label.transform.IsChildOf(tree.Viewport.Content)) continue;
            Check(label.preferredHeight <= label.rectTransform.rect.height + 1, "text height clipped: " + label.text);
        }
        return $"PASS layout {Screen.width}x{Screen.height}: visible buttons in screen, panel text heights, all graph CanvasRenderers and icons present (API bounds; inspect PNG separately).";
    }

    public static string PauseAndDisable()
    {
        var tree = Find<GameManager>().ResearchTree;
        var camera = Find<CameraController>();
        var menu = Find<GameFlowMenu>();
        tree.Close();
        bool originalLock = camera.LockCamera;
        menu.Pause(); tree.Open();
        Check(!tree.IsOpen && Time.timeScale == 0, "tree opened over pause");
        menu.Resume(); tree.Open();
        Check(tree.IsOpen && camera.LockCamera, "failed to reopen after pause");
        tree.enabled = false;
        Check(!tree.IsOpen && camera.LockCamera == originalLock, "disable did not restore camera");
        tree.enabled = true; tree.Open(); tree.Close();
        Check(camera.LockCamera == originalLock && Time.timeScale == 1, "re-enable leaked camera/time");
        return "PASS pause/open guard, resume/reopen, component disable/enable restores camera/time.";
    }

    public static string Terminal()
    {
        var game = Find<GameManager>(); var tree = game.ResearchTree;
        var root = Find<GameSceneCompositionRoot>(); var bridge = Find<SwarmGameBridge>();
        tree.Open();
        Check(tree.IsOpen, "expected open tree before terminal outcome");
        Check(bridge.Gameplay.EvaluateMatch(6) == RTS.DOTS.MatchOutcome.Victory, "fixture did not reach victory");
        Find<MatchSession>().SendMessage("Update"); tree.SendMessage("Update");
        Check(game.IsMatchOver && !tree.IsOpen, "terminal match left tree open");
        tree.Open();
        Check(!tree.IsOpen && tree.ResearchSelected() == ResearchResult.Unavailable, "terminal input accepted");
        return "PASS authoritative victory closes tree, blocks reopen/research; fixture forces day6, not a five-night playtest. Reset match after this fixture.";
    }
}
