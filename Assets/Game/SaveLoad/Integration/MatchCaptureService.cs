using System;
using System.Linq;
using RTS.DOTS;
using RTS.SaveLoad;
using Unity.Entities;
using UnityEngine;

// End-of-frame owner: all normal gameplay LateUpdate callbacks run before the requested boundary.
[DefaultExecutionOrder(32000)]
public sealed class MatchCaptureService : MonoBehaviour
{
    GameSceneCompositionRoot m_Root;
    SwarmGameBridge m_Bridge;
    ProceduralMapAuthoring m_Generator;
    TilemapManager m_Map;
    GameManager m_Game;
    DayNightCycleManager m_Clock;
    EnemyWaveManager m_Waves;
    FogOfWarManager m_Fog;
    CameraController m_Camera;
    MatchReportService m_Reports;
    SaveContentCatalog m_Content;
    long m_RequestedGeneration;
    public SaveCatalog Catalog { get; private set; }
    public MatchSaveData LastSnapshot { get; private set; }
    public bool IsPending { get; private set; }
    public bool IsCapturing { get; private set; }
    public string LastError { get; private set; }
    public double CaptureMilliseconds { get; private set; }
    public void Initialize(GameSceneCompositionRoot root, SwarmGameBridge bridge, ProceduralMapAuthoring generator, TilemapManager map,
        GameManager game, DayNightCycleManager clock, EnemyWaveManager waves, FogOfWarManager fog, CameraController camera,
        MatchReportService reports, SaveContentCatalog content)
    {
        if (m_Root != null) throw new InvalidOperationException("Capture service is already initialized.");
        if (root == null || bridge == null || generator == null || map == null || game == null || clock == null || waves == null
            || fog == null || camera == null || reports == null || content == null || root.gameObject != gameObject || bridge.gameObject != gameObject)
            throw new ArgumentException("Capture requires the owning procedural scene dependencies.");
        m_Root = root; m_Bridge = bridge; m_Generator = generator; m_Map = map; m_Game = game; m_Clock = clock;
        m_Waves = waves; m_Fog = fog; m_Camera = camera; m_Reports = reports; m_Content = content;
        Catalog = content.BuildRuntimeCatalog();
    }
    public bool RequestCapture(long generation)
    {
        if (!isActiveAndEnabled || m_Root == null || !m_Root.IsReady || m_Bridge.Gameplay.IsMatchOver || IsPending || IsCapturing
            || gameObject.scene.name != "ProceduralGame"
            || m_Bridge.Simulation.SnapshotFenced || generation <= 0 || generation == long.MaxValue) return false;
        m_RequestedGeneration = generation; IsPending = true; LastError = null;
        return true;
    }
    void LateUpdate()
    {
        if (!IsPending) return;
        IsPending = false; IsCapturing = true; LastSnapshot = null;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        try { LastSnapshot = CaptureBoundary(m_RequestedGeneration); }
        catch (Exception error) { LastError = error.Message; }
        finally { CaptureMilliseconds = timer.Elapsed.TotalMilliseconds; IsCapturing = false; }
    }
    MatchSaveData CaptureBoundary(long generation)
    {
        if (!m_Root.IsReady || m_Bridge.Gameplay.IsMatchOver || m_Waves.TacticalPlan == null || m_Generator.GatewayCount == 0)
            throw new InvalidOperationException("Only a ready, running ProceduralGame tactical match supports capture.");
        bool oldGame = m_Game.SnapshotFenced, oldClock = m_Clock.SnapshotFenced, oldWave = m_Waves.SnapshotFenced, oldCamera = m_Camera.LockCamera;
        m_Game.SnapshotFenced = m_Clock.SnapshotFenced = m_Waves.SnapshotFenced = true; m_Camera.LockCamera = true;
        try
        {
            m_Bridge.PrepareSaveCapture();
            using var lease = m_Bridge.Simulation.AcquireSnapshotFence();
            var native = m_Bridge.Simulation.CaptureSnapshot();
            var ids = native.Objects.ToDictionary(x => x.Entity, x => x.Id);
            long Id(Entity entity) => ids.TryGetValue(entity, out long value) ? value : 0;
            var state = native.Research;
            var snapshot = new MatchSaveData
            {
                SchemaVersion = SaveValidator.SchemaVersion, SaveId = Guid.NewGuid().ToString("N"), CapturedUtc = DateTimeOffset.UtcNow.ToString("O"),
                ContentFingerprint = Catalog.Fingerprint, Generation = generation, NextSaveId = native.NextSaveId, NextRequestId = m_Bridge.SaveNextRequestId,
                GameplayTicks = native.GameplayTicks, SwarmTicks = native.SwarmTicks, TotalSpawned = native.TotalSpawned, TotalHits = native.TotalHits,
                Map = CaptureMap(), Objects = m_Bridge.ConvertSaveObjects(native, m_Content),
                Player = new PlayerSaveData { Wallet = new ResourceAmounts { Wood = native.Wallet.Wood, Gold = native.Wallet.Gold, Meat = native.Wallet.Meat },
                    UnlockedTechnologyIds = m_Content.Research.ToCatalog().Where((_, index) => (state.UnlockMask & (1u << index)) != 0).Select(x => x.Id).ToArray(),
                    ActiveTechnologyId = state.Active != 0 ? m_Bridge.Gameplay.GetTechnologyId(state.Technology) : null,
                    ResearchTownHall = state.Active != 0 ? Id(state.TownHall) : 0, ResearchElapsed = state.Elapsed,
                    SurvivalNights = native.SurvivalNights, Statistics = Stats(native.Statistics) },
                Clock = new ClockSaveData { Day = m_Clock.CurrentDay, Phase = (int)m_Clock.CurrentPhase, Elapsed = m_Clock.SavePhaseElapsed },
                Wave = m_Waves.CaptureSaveState(),
                Reports = new ReportSaveData { PreviousPhase = m_Reports.SavePreviousPhase, CompletedNight = m_Reports.CompletedNight,
                    PreviousDawn = Report(m_Reports.SavePreviousDawn), LatestNight = Report(m_Reports.LatestNight),
                    WarningConsumedDay = m_Clock.CurrentDay - (m_Clock.CurrentPhase == DayNightPhase.Day ? 1 : 0), LastAutosavedNight = 0 },
                View = new ViewSaveData { Camera = new SaveVector { X = m_Camera.transform.position.x, Y = m_Camera.transform.position.y },
                    Zoom = m_Camera.ViewCamera.orthographicSize,
                    Selection = m_Game.SelectedUnits.Where(x => x != null && x.IsDotsControlled && ids.ContainsKey(x.DotsEntity)).Select(x => Id(x.DotsEntity)).ToArray() }
            };
            ValidateClaims(snapshot);
            SaveValidator.Validate(snapshot, Catalog);
            return snapshot;
        }
        finally
        {
            m_Game.SnapshotFenced = oldGame; m_Clock.SnapshotFenced = oldClock; m_Waves.SnapshotFenced = oldWave; m_Camera.LockCamera = oldCamera;
        }
    }
    MapSaveData CaptureMap()
    {
        var map = m_Generator.GeneratedMap;
        var layout = map.Layout;
        var origin = m_Generator.OriginCell;
        var bounds = new BoundsInt(origin, new Vector3Int(map.Width, map.Height, 1));
        var world = m_Map.WalkableTilemap.CellToWorld(origin);
        return new MapSaveData
        {
            Width = map.Width, Height = map.Height, Seed = map.Seed, RequestedSeed = map.RequestedSeed, UsedFallback = map.UsedFallback, Attempt = map.Attempt, GeneratorVersion = map.GeneratorVersion,
            StartX = map.StartX, StartY = map.StartY, OriginCellX = origin.x, OriginCellY = origin.y,
            WorldOrigin = new SaveVector { X = world.x, Y = world.y }, CellSize = m_Map.WalkableTilemap.cellSize.x * m_Map.WalkableTilemap.transform.lossyScale.x,
            SettingsJson = JsonUtility.ToJson(m_Generator.SaveSettings), Terrain = (byte[])map.Terrain.Clone(), Reserved = (byte[])map.Reserved.Clone(),
            Explored = m_Fog.CaptureExplored(bounds), RegionIds = layout != null ? (byte[])layout.RegionIds.Clone() : Array.Empty<byte>(),
            Routes = layout != null ? (byte[])layout.Routes.Clone() : Array.Empty<byte>(),
            Regions = layout != null ? layout.Regions.Select(x => new RegionSaveData { X = x.X, Y = x.Y, Radius = x.Radius, Role = (int)x.Role }).ToArray() : Array.Empty<RegionSaveData>(),
            Connections = layout != null ? layout.Connections.Select(x => new ConnectionSaveData { From = x.From, To = x.To }).ToArray() : Array.Empty<ConnectionSaveData>(),
            Gateways = layout != null ? layout.Gateways.Select(x => new GatewaySaveData { X = x.X, Y = x.Y, Region = x.Region }).ToArray() : Array.Empty<GatewaySaveData>(),
            Landmark = layout != null ? new LandmarkSaveData { X = layout.Landmark.X, Y = layout.Landmark.Y, Radius = layout.Landmark.Radius, Region = layout.Landmark.Region } : default,
            Placements = map.Resources.Select((p, index) => new PlacementSaveData { Kind = (int)p.Kind, VariantId = p.VariantId, X = p.X, Y = p.Y,
                ClusterId = p.ClusterId, IsStartingResource = p.IsStartingResource, ObjectId = m_Bridge.SaveIdForPlacement(m_Generator.SavePlacementView(index)) }).ToArray()
        };
    }
    static void ValidateClaims(MatchSaveData snapshot)
    {
        var claims = snapshot.Objects.Where(x => x.Worker != null && x.Worker.Node != 0).GroupBy(x => x.Worker.Node).ToDictionary(x => x.Key, x => x.Count());
        foreach (var obj in snapshot.Objects)
            if (obj.Resource != null && obj.Resource.Claims != (claims.TryGetValue(obj.Id, out int count) ? count : 0))
                throw new InvalidOperationException("Resource claims do not match worker assignments: " + obj.Id);
    }
    static ResourceAmounts Amounts(ResourceTotals x) => new() { Wood = x.Wood, Gold = x.Gold, Meat = x.Meat };
    static StatisticsSaveData Stats(MatchStatistics x) => new() { Deposited = Amounts(x.Deposited), Spent = Amounts(x.Spent), Granted = Amounts(x.Granted),
        UnitsLost = x.UnitsLost, ResearchCompleted = x.ResearchCompleted, TownHallDamage = x.TownHallDamage };
    static ReportSnapshotData Report(MatchReportSnapshot x) => new() { Statistics = Stats(x.Statistics), ExploredCells = x.ExploredCells };
    void OnDisable() { if (IsPending) LastError = "Capture cancelled with its owning scene."; IsPending = false; }
    void OnDestroy() { LastSnapshot = null; }
}
