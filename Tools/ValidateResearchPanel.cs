using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using RTS.DOTS;

public static class ResearchPanelValidation
{
    sealed class FakeResearch : IResearchService
    {
        public int Calls;
        public PlayerResearch State => default;
        public float Duration => 20;
        public ResearchDefinition Definition => new ResearchDefinition { Wood = 40, Gold = 15, Duration = 20, WoodMultiplier = 1.25f };
        public ResearchDefinition GetDefinition(string id) => Definition;
        public bool IsUnlocked(string id) => false;
        public string ActiveTechnology => null;
        public ResearchResult Start(string id, TownHall hall) { Calls++; return ResearchResult.Started; }
    }
    public static string Run()
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var go = new GameObject("Research fake-service test", typeof(RectTransform), typeof(Button), typeof(ResearchPanel));
        var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(go.transform);
        var fake = new FakeResearch(); var panel = go.GetComponent<ResearchPanel>(); var button = go.GetComponent<Button>();
        try
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(ResearchPanel).GetField("m_Button", flags).SetValue(panel, button);
            typeof(ResearchPanel).GetField("m_Label", flags).SetValue(panel, label.GetComponent<Text>());
            panel.Initialize(fake, manager); panel.Initialize(fake, manager);
            button.onClick.Invoke();
            if (fake.Calls != 1) throw new Exception("Initialize subscribed twice.");
            go.SetActive(false); button.onClick.Invoke();
            if (fake.Calls != 1) throw new Exception("Disabled panel retained subscription.");
            go.SetActive(true); button.onClick.Invoke();
            if (fake.Calls != 2) throw new Exception("Reopened panel did not subscribe exactly once.");
            bool rejected = false;
            try { panel.Initialize(new FakeResearch(), manager); } catch (InvalidOperationException) { rejected = true; }
            if (!rejected) throw new Exception("Replacement service was accepted.");
            return "PASS fake-service injection, repeat Init, close/reopen, replacement rejected";
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
