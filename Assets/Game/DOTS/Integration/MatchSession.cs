using System;
using RTS.DOTS;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MatchSession : MonoBehaviour
{
    [SerializeField, Min(1)] int m_SurvivalNights = 5;
    [SerializeField] GameFlowMenu m_Menu;
    [SerializeField] GameObject m_ResultPanel;
    [SerializeField] Text m_ResultLabel;
    [SerializeField] Text m_ObjectiveLabel;
    [SerializeField] Button m_ReplayButton;
    [SerializeField] Button m_MenuButton;
    GameplaySimulationSystem m_Gameplay;
    DayNightCycleManager m_Day;
    GameManager m_Game;
    bool m_ResultShown;
    public void ShowLoadedPause(CameraController camera) { m_Menu.PauseLoaded(camera); }
    MatchReportService m_Reports;
    Text m_ResultStatistics;
    public string ResultStatisticsText => m_ResultStatistics != null ? m_ResultStatistics.text : "";

    public void InitializeReports(MatchReportService reports)
    {
        if (reports == null) throw new ArgumentNullException(nameof(reports));
        if (m_Reports != null) { if (m_Reports != reports) throw new InvalidOperationException("Result reports belong to another match."); return; }
        m_Reports = reports;
        m_ResultPanel.GetComponentInParent<Canvas>().sortingOrder = 4000;
        m_ResultPanel.GetComponent<Image>().color = new Color(.02f, .04f, .07f, 1);
        var go = new GameObject("Result statistics", typeof(RectTransform)); go.transform.SetParent(m_ResultPanel.transform, false);
        m_ResultStatistics = go.AddComponent<Text>(); m_ResultStatistics.font = m_ResultLabel.font; m_ResultStatistics.fontSize = 25;
        m_ResultStatistics.color = Color.white; m_ResultStatistics.raycastTarget = false; m_ResultStatistics.alignment = TextAnchor.MiddleCenter;
        var rect = m_ResultStatistics.rectTransform; rect.sizeDelta = new Vector2(1050, 330); rect.anchoredPosition = new Vector2(0, 25);
        m_ResultLabel.rectTransform.anchoredPosition = new Vector2(0, 280);
        m_ReplayButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -220);
        m_MenuButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -310);
    }

    public void Initialize(GameplaySimulationSystem gameplay, DayNightCycleManager day, GameManager game)
    {
        if (gameplay == null || day == null || game == null || m_Menu == null || m_ResultPanel == null
            || m_ResultLabel == null || m_ObjectiveLabel == null || m_ReplayButton == null || m_MenuButton == null)
            throw new InvalidOperationException("Match session requires its gameplay, clock, menu and result UI references.");
        if (m_Gameplay != null)
        {
            if (m_Gameplay != gameplay || m_Day != day || m_Game != game)
                throw new InvalidOperationException("Match session is already initialized with different dependencies.");
            return;
        }
        gameplay.ConfigureMatch(m_SurvivalNights);
        m_Gameplay = gameplay; m_Day = day; m_Game = game;
        m_Menu.InitializeMatch(gameplay);
        day.InitializeMatch(gameplay);
        m_ResultPanel.SetActive(false);
        m_ReplayButton.onClick.AddListener(m_Menu.Play);
        m_MenuButton.onClick.AddListener(m_Menu.ReturnToMenu);
        m_ObjectiveLabel.text = $"OBJECTIVE: Protect your TownHall through {m_SurvivalNights} nights";
    }

    void LateUpdate()
    {
        if (m_Gameplay == null || !ProceduralMapRuntime.IsReady || ProceduralMapRuntime.IsCompositionBlocked || m_ResultShown) return;
        var outcome = m_Gameplay.EvaluateMatch(m_Day.CurrentDay);
        if (outcome == MatchOutcome.Running) return;
        m_ResultShown = true;
        m_Game.CancelBuildProcess();
        m_Menu.ClosePausePanels();
        m_ResultLabel.text = outcome == MatchOutcome.Victory
            ? $"VICTORY\nSurvived {m_SurvivalNights} nights!"
            : $"DEFEAT\nYour last TownHall fell on day {m_Day.CurrentDay}.";
        m_ResultPanel.SetActive(true);
        if (m_Reports != null) { m_Reports.Freeze(); m_ResultStatistics.text = "TỔNG TRẬN\n" + MatchReportText.Format(m_Reports.Total); }
        Time.timeScale = 0;
    }
}
