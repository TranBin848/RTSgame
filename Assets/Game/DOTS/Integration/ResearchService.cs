using System;
using RTS.DOTS;

public interface IResearchService
{
    PlayerResearch State { get; }
    float Duration { get; }
    ResearchDefinition Definition { get; }
    ResearchDefinition GetDefinition(string id);
    bool IsUnlocked(string id);
    string ActiveTechnology { get; }
    ResearchResult Start(string technologyId, TownHall townHall);
}

public sealed class ResearchService : IResearchService
{
    readonly SwarmGameBridge m_Bridge;
    public ResearchService(SwarmGameBridge bridge)
    {
        if (bridge == null || !bridge.IsInitialized) throw new ArgumentException("Research requires an initialized gameplay bridge.", nameof(bridge));
        m_Bridge = bridge;
    }
    public PlayerResearch State => m_Bridge.Gameplay.Research;
    public float Duration => m_Bridge.Gameplay.ResearchDuration;
    public ResearchDefinition Definition => m_Bridge.Gameplay.ResearchSettings;
    public ResearchDefinition GetDefinition(string id) => m_Bridge.Gameplay.GetResearchDefinition(id);
    public bool IsUnlocked(string id) => m_Bridge.Gameplay.IsTechnologyUnlocked(id);
    public string ActiveTechnology => m_Bridge.Gameplay.ActiveResearchId;
    public ResearchResult Start(string technologyId, TownHall townHall)
    {
        if (ProceduralMapRuntime.IsCompositionBlocked || !m_Bridge.IsSceneBound) return ResearchResult.Unavailable;
        if (townHall == null || !townHall.IsDotsControlled) return ResearchResult.InvalidTownHall;
        return m_Bridge.Gameplay.StartResearch(technologyId, townHall.DotsEntity);
    }
}
