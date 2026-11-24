using System;
using RTS.DOTS;

public struct MatchReportSnapshot
{
    public MatchStatistics Statistics;
    public int ExploredCells;
    public static MatchReportSnapshot operator -(MatchReportSnapshot a, MatchReportSnapshot b)
        => new() { Statistics = a.Statistics - b.Statistics, ExploredCells = a.ExploredCells - b.ExploredCells };
}

// Match-local snapshots: closing/reopening UI never writes counters or grants rewards.
public sealed class MatchReportService : IDisposable
{
    readonly GameplaySimulationSystem m_Gameplay;
    readonly DayNightCycleManager m_Clock;
    readonly FogOfWarManager m_Fog;
    DayNightPhase m_PreviousPhase;
    MatchReportSnapshot m_PreviousDawn, m_Final;
    bool m_Frozen, m_Disposed;
    public int CompletedNight { get; private set; }
    public bool IsFrozen => m_Frozen;
    public MatchReportSnapshot LatestNight { get; private set; }
    public MatchReportSnapshot Total => m_Frozen ? m_Final : new MatchReportSnapshot { Statistics = m_Gameplay.Statistics, ExploredCells = m_Fog.ExploredCellCount };
    public event Action NightClosed;

    public MatchReportService(GameplaySimulationSystem gameplay, DayNightCycleManager clock, FogOfWarManager fog)
    {
        if (gameplay == null || clock == null || fog == null || !fog.IsInitialized) throw new ArgumentException("Reports require gameplay, clock and initialized fog.");
        m_Gameplay = gameplay; m_Clock = clock; m_Fog = fog; m_PreviousPhase = clock.CurrentPhase;
        clock.OnPhaseChanged += OnPhaseChanged;
    }
    void OnPhaseChanged(DayNightPhase phase)
    {
        var previous = m_PreviousPhase; m_PreviousPhase = phase;
        if (m_Disposed || m_Frozen || previous != DayNightPhase.Night || phase != DayNightPhase.Day || m_Clock.CurrentDay - 1 <= CompletedNight) return;
        var total = Total;
        LatestNight = total - m_PreviousDawn; m_PreviousDawn = total; CompletedNight = m_Clock.CurrentDay - 1;
        NightClosed?.Invoke();
    }
    public void Freeze() { if (!m_Frozen) { m_Final = Total; m_Frozen = true; } }
    public void Dispose()
    {
        if (m_Disposed) return;
        m_Disposed = true; m_Clock.OnPhaseChanged -= OnPhaseChanged; NightClosed = null;
    }
}

public static class MatchReportText
{
    static string Resources(ResourceTotals value) => $"Gỗ {value.Wood}   ·   Vàng {value.Gold}   ·   Thịt {value.Meat}";
    public static string Format(MatchReportSnapshot report)
    {
        var s = report.Statistics;
        return $"Nộp kho:  {Resources(s.Deposited)}\nChi phí:  {Resources(s.Spent)}\n"
            + $"Dân / quân mất:  {s.UnitsLost}\nSát thương TownHall đã nhận:  {s.TownHallDamage:0.##}\n"
            + $"Nghiên cứu hoàn tất:  {s.ResearchCompleted}\nÔ mới khám phá:  {report.ExploredCells}\n"
            + $"Đầu trận / cấp thêm (riêng):  {Resources(s.Granted)}";
    }
}
