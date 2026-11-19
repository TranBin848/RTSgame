// Run after the ResearchPanel cache fix through Unity MCP. Does not change the live match.
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RTS.DOTS;

public static class S9ResearchAllocations
{
    sealed class FakeResearch : IResearchService
    {
        public PlayerResearch Snapshot;
        public int DefinitionReads;
        public PlayerResearch State => Snapshot;
        public float Duration => 20;
        public ResearchDefinition Definition => new ResearchDefinition { Id="economy_tools",Duration=20,Wood=40,Gold=15,WoodMultiplier=1.25f };
        public ResearchDefinition GetDefinition(string id) { DefinitionReads++; return Definition; }
        public bool IsUnlocked(string id) => false;
        public string ActiveTechnology => "military_archery";
        public ResearchResult Start(string id,TownHall hall) => ResearchResult.Unavailable;
    }
    public static string Run()
    {
        var fixture=new GameObject("S9 isolated research allocation fixture");fixture.SetActive(false);
        var panelObject=new GameObject("Panel",typeof(RectTransform),typeof(Button),typeof(Text),typeof(ResearchPanel));
        panelObject.SetActive(false);panelObject.transform.SetParent(fixture.transform,false);
        try
        {
            var game=fixture.AddComponent<GameManager>();var panel=panelObject.GetComponent<ResearchPanel>();
            var data=new SerializedObject(panel);
            data.FindProperty("m_Button").objectReferenceValue=panelObject.GetComponent<Button>();
            data.FindProperty("m_Label").objectReferenceValue=panelObject.GetComponent<Text>();data.ApplyModifiedPropertiesWithoutUndo();
            var service=new FakeResearch();panel.Initialize(service,game);
            var update=(Action)Delegate.CreateDelegate(typeof(Action),panel,typeof(ResearchPanel).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance));
            long Measure(bool busy)
            {
                service.Snapshot=new PlayerResearch { Active=(byte)(busy?1:0),Elapsed=0 };
                for(int i=0;i<100;i++)update();
                long before=GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<1000;i++){var state=service.Snapshot;state.Elapsed=i*.05f;service.Snapshot=state;update();}
                return GC.GetAllocatedBytesForCurrentThread()-before;
            }
            long idle=Measure(false),busy=Measure(true);
            if(idle!=0||busy!=0||service.DefinitionReads!=1)
                throw new Exception($"Polling regression: idle={idle}B,busy-other-panel={busy}B,definitionReads={service.DefinitionReads}.");
            return $"PASS1000idle+1000busy-other-panel Updates: {idle}/{busy}B managed thread allocation; definition read once. Excludes changing progress text, Unity layout/render and whole-frame GC.";
        }
        finally{UnityEngine.Object.DestroyImmediate(fixture);}
    }
}
