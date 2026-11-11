using System;
using RTS.DOTS;
using UnityEngine;
using UnityEngine.UI;

public sealed class ResearchPanel : MonoBehaviour
{
    [SerializeField] Button m_Button;
    [SerializeField] Text m_Label;
    [SerializeField] string m_TechnologyId = GameplaySimulationSystem.EconomyToolsId;
    IResearchService m_Research;
    GameManager m_Game;
    int m_LastDisplay = int.MinValue;
    ResearchResult? m_LastResult;
    static string Title(string id) => id == "economy_tools" ? "Tools" : id == "economy_cargo" ? "Cargo"
        : id == "military_archery" ? "Archery" : id == "military_training" ? "Training"
        : id == "expansion_tower" ? "Outposts" : "Fortify";
    public void Initialize(IResearchService research, GameManager game)
    {
        if (research == null || game == null || m_Button == null || m_Label == null)
            throw new ArgumentException("Research panel requires service, manager, button and label.");
        if (m_Research != null)
        {
            if (m_Research != research || m_Game != game) throw new InvalidOperationException("Panel already initialized.");
            return;
        }
        m_Research = research; m_Game = game;
        if (isActiveAndEnabled) m_Button.onClick.AddListener(StartResearch);
    }
    void OnEnable() { m_LastDisplay = int.MinValue; if (m_Research != null) m_Button.onClick.AddListener(StartResearch); }
    void OnDisable() { if (m_Button != null) m_Button.onClick.RemoveListener(StartResearch); }
    void StartResearch()
    {
        m_LastResult = m_Research.Start(m_TechnologyId, m_Game.ActiveUnit as TownHall);
        m_LastDisplay = int.MinValue;
    }
    void Update()
    {
        if (m_Research == null) return;
        var state = m_Research.State;
        bool selected = m_Game.ActiveUnit is TownHall;
        bool ready = !ProceduralMapRuntime.IsCompositionBlocked;
        var definition = m_Research.GetDefinition(m_TechnologyId);
        bool unlocked = m_Research.IsUnlocked(m_TechnologyId);
        bool prerequisite = string.IsNullOrEmpty(definition.Prerequisite) || m_Research.IsUnlocked(definition.Prerequisite);
        m_Button.interactable = ready && selected && prerequisite && state.Active == 0 && !unlocked;
        int display = unlocked ? -1 : state.Active != 0 ? (int)(state.Elapsed * 10) : !prerequisite ? -4 : selected ? -2 : -3;
        if (display == m_LastDisplay) return;
        m_LastDisplay = display;
        string effect = m_TechnologyId == "economy_tools" ? $"Wood x{definition.WoodMultiplier}" : m_TechnologyId == "economy_cargo" ? $"Cargo x{definition.CargoMultiplier}"
            : m_TechnologyId == "military_training" ? $"Training time x{definition.TrainingMultiplier}" : m_TechnologyId == "expansion_fortify" ? $"Tower damage x{definition.TowerMultiplier}"
            : m_TechnologyId == "military_archery" ? "Unlock archery" : "Unlock towers";
        m_Label.text = unlocked ? $"{Title(m_TechnologyId)}: researched\n{effect}"
            : state.Active != 0 && m_Research.ActiveTechnology == m_TechnologyId ? $"{Title(m_TechnologyId)}: {state.Elapsed:F1}/{m_Research.Duration:F1}s"
            : $"{Title(m_TechnologyId)}: {effect}\n{definition.Wood}W {definition.Gold}G {definition.Meat}M / {definition.Duration}s\n"
                + (!prerequisite ? $"Requires {Title(definition.Prerequisite)}" : state.Active != 0 ? "Research slot busy" : "Select completed TownHall. Hall lost: no refund.")
                + (m_LastResult == ResearchResult.InsufficientResources ? "\nNot enough resources" : m_LastResult == ResearchResult.InvalidTownHall ? "\nA living, completed TownHall is required" : "");
    }
}
