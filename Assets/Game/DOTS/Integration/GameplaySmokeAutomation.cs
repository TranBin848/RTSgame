using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using RTS.DOTS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

// Opt-in integration scenario. Shortened native timers affect this disposable run only.
public sealed class GameplaySmokeAutomation : MonoBehaviour
{
    [Serializable] class Check { public string name, details; public bool passed; }
    [Serializable] class Report
    {
        public string unity, scenario = "SampleScene / full gameplay ECS integration";
        public bool passed;
        public int actors, enemies;
        public long ticks;
        public Check[] checks;
        public string[] errors;
    }
    readonly List<Check> m_Checks = new();
    readonly List<string> m_Errors = new();
    SwarmGameBridge m_Bridge;
    TilemapManager m_Map;
    string m_Output;
    float m_Started;
    bool m_Finished;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "--gameplay-smoke") < 0) return;
        var host = new GameObject("Gameplay ECS integration validation"); DontDestroyOnLoad(host); host.AddComponent<GameplaySmokeAutomation>();
    }
    IEnumerator Start()
    {
        m_Started = Time.realtimeSinceStartup; Application.runInBackground = true;
        Application.logMessageReceived += OnLog;
        string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "--gameplay-output");
        m_Output = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.GetFullPath(".utmp/gameplay-smoke");
        Directory.CreateDirectory(m_Output);
        if (SceneManager.GetActiveScene().name != "SampleScene") SceneManager.LoadScene("SampleScene");
        yield return null; yield return null;
        m_Bridge = SwarmGameBridge.Instance; m_Map = FindFirstObjectByType<TilemapManager>();
        Record("bridge_initialized", m_Bridge != null && m_Bridge.Gameplay != null && m_Bridge.Gameplay.IsConfigured);
        if (m_Bridge == null) { Finish(); yield break; }
        var camera = FindFirstObjectByType<CameraController>(); if (camera != null) camera.LockCamera = true;
        var dayNight = FindFirstObjectByType<DayNightCycleManager>(); if (dayNight != null) dayNight.enabled = false;
        var waves = FindFirstObjectByType<EnemyWaveManager>(); if (waves != null) waves.enabled = false;
        bool allBound = true; int views = 0;
        foreach (var unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            if (unit is EnemyUnit) { allBound = false; continue; }
            views++; allBound &= unit.IsDotsControlled;
            if (unit.TryGetComponent<AIPawn>(out var pawn)) allBound &= !pawn.enabled;
        }
        Record("all_scene_units_bound_and_legacy_movement_disabled", allBound, $"views={views}, actors={m_Bridge.Gameplay.ActorCount}");
        var worker = FindFirstObjectByType<WorkerUnit>(); var hall = FindFirstObjectByType<TownHall>();
        if (worker == null || hall == null) { Record("worker_and_hall_exist", false); Finish(); yield break; }
        Vector3 before = worker.transform.position;
        m_Map.TryResolveWalkableDestination(before + Vector3.right * 3, out var goal);
        worker.MoveTo(goal, DestinationSource.PlayerClick); yield return new WaitForSecondsRealtime(1);
        Record("player_move_command_moves_ecs_and_view", Vector3.Distance(before, worker.transform.position) > 0.15f);

        var tree = FindFirstObjectByType<Tree>();
        if (tree != null)
        {
            var profiles = m_Bridge.Gameplay.EntityManager.GetBuffer<GatheringProfileData>(worker.DotsEntity);
            for (int i = 0; i < profiles.Length; i++) { var p = profiles[i]; p.TickTime = 0.03f; p.Amount = 2; p.Capacity = 4; profiles[i] = p; }
            int woodBefore = GameManager.Get().Wood;
            Record("worker_claims_scene_tree", worker.TryAssignResourceNode(tree));
            TeleportNear(worker, tree.DotsCenter, tree.InteractionRadius + 0.25f);
            yield return WaitUntil(() => m_Bridge.Gameplay.TryGetWorker(worker.DotsEntity, out var w) && w.Carried >= 4, 6);
            Record("worker_gathers_native_cargo", m_Bridge.Gameplay.TryGetWorker(worker.DotsEntity, out var cargo) && cargo.Carried >= 4);
            worker.SetWoodStorage(hall); TeleportNear(worker, hall.Collider.ClosestPoint(worker.transform.position), 0.2f);
            yield return WaitUntil(() => GameManager.Get().Wood > woodBefore, 8);
            Record("worker_deposits_into_native_wallet_and_hud", GameManager.Get().Wood >= woodBefore + 4);
            worker.CancelActiveWork();
        }
        else Record("scene_tree_exists", false);

        BuildActionSo buildAction = null;
        foreach (var action in worker.Actions) if (action is BuildActionSo build && build.StructureUnitPrefab is TowerUnit) { buildAction = build; break; }
        if (buildAction != null && TryPlacement(buildAction, out var placement))
        {
            var process = new BuildingProcess(buildAction, placement, worker);
            var tower = ClosestTower(placement);
            Record("construction_view_is_ecs", tower != null && tower.IsDotsControlled);
            if (tower != null)
            {
                var em = m_Bridge.Gameplay.EntityManager;
                var build = em.GetComponentData<GameplayConstruction>(tower.DotsEntity); build.Duration = 0.3f; em.SetComponentData(tower.DotsEntity, build);
                TeleportNear(worker, tower.Collider.ClosestPoint(worker.transform.position), 0.2f);
                yield return WaitUntil(() => process.m_isFinished, 8);
                Record("construction_completes_and_updates_sprite", process.m_isFinished && !tower.isUnderConstruction && tower.SpriteRenderer.sprite == buildAction.CompleteSprite);
            }
        }
        else Record("construction_fixture_available", false);

        int workerCount = FindObjectsByType<WorkerUnit>(FindObjectsSortMode.None).Length;
        foreach (var action in hall.Actions) if (action is SpawnVillagerActionSO train) { hall.EnqueueVillagerSpawn(train); break; }
        ShortenTraining(hall);
        yield return WaitUntil(() => FindObjectsByType<WorkerUnit>(FindObjectsSortMode.None).Length > workerCount, 5);
        bool trainedBound = false;
        foreach (var unit in FindObjectsByType<WorkerUnit>(FindObjectsSortMode.None)) if (unit != worker && unit.IsDotsControlled) trainedBound = true;
        Record("townhall_training_spawns_bound_worker", FindObjectsByType<WorkerUnit>(FindObjectsSortMode.None).Length > workerCount && trainedBound);

        var barrack = FindFirstObjectByType<Barrack>();
        if (barrack != null)
        {
            int count = CountKind(GameplayKind.Soldier);
            foreach (var action in barrack.Actions) if (action is SpawnWarriorActionSO train) { barrack.EnqueueWarriorSpawn(train); break; }
            ShortenTraining(barrack); yield return WaitUntil(() => CountKind(GameplayKind.Soldier) > count, 5);
            Record("barrack_training_spawns_bound_soldier", CountKind(GameplayKind.Soldier) > count);
        }
        else Record("barrack_exists", false);
        var archery = FindFirstObjectByType<ArcheryRange>();
        if (archery != null)
        {
            int count = CountKind(GameplayKind.Archer);
            foreach (var action in archery.Actions) if (action is SpawnArcherActionSO train) { archery.EnqueueArcherSpawn(train); break; }
            ShortenTraining(archery); yield return WaitUntil(() => CountKind(GameplayKind.Archer) > count, 5);
            Record("archery_training_spawns_bound_archer", CountKind(GameplayKind.Archer) > count);
        }
        else Record("archery_exists", false);

        // Use the public shelter commands, with a deterministic near-hall starting position.
        worker.ReturnToTownHall(hall); TeleportNear(worker, hall.Collider.ClosestPoint(worker.transform.position), 0.2f);
        yield return WaitUntil(() => worker.IsSheltered, 8);
        Record("worker_shelters_and_hides_view", worker.IsSheltered && !worker.SpriteRenderer.enabled && !worker.Collider.enabled);
        worker.LeaveTownHallAtDaybreak(); yield return new WaitForSecondsRealtime(0.4f);
        Record("daybreak_restores_worker", !worker.IsSheltered && worker.SpriteRenderer.enabled && worker.Collider.enabled);

        var sheep = FindFirstObjectByType<SheepUnit>();
        if (sheep != null)
        {
            Vector3 sheepBefore = sheep.transform.position; sheep.TakeDamage(1, worker);
            yield return new WaitForSecondsRealtime(1);
            Record("sheep_native_damage_and_flee", sheep.IsDotsControlled && sheep.CurrentHealth == sheep.MaxHealth - 1 && Vector3.Distance(sheepBefore, sheep.transform.position) > 0.05f);
        }
        else Record("scene_sheep_exists", false);

        // Spawn using an original wave prefab, then also exercise direct Instantiate lifecycle conversion.
        GameObject enemyPrefab = null;
        if (waves != null && waves.Definition != null && waves.Definition.EnemyPrefabs.Length > 0) enemyPrefab = waves.Definition.EnemyPrefabs[0];
        if (enemyPrefab != null)
        {
            m_Map.TryResolveWalkableDestination(hall.transform.position + Vector3.right * 8, out var enemyPosition);
            int nativeBefore = m_Bridge.Simulation.AgentCount;
            Record("enemy_wave_manager_spawns_ecs", waves != null && waves.SpawnEnemy(hall));
            Record("shared_wave_spawn_api_creates_entity", m_Bridge.SpawnEnemy(enemyPrefab, enemyPosition));
            m_Map.TryResolveWalkableDestination(enemyPosition + Vector3.up, out var secondPosition);
            var directView = Instantiate(enemyPrefab, secondPosition, Quaternion.identity);
            yield return null; yield return null;
            Record("direct_enemy_prefab_converts_to_entity", (directView == null || !directView.activeInHierarchy) && m_Bridge.Simulation.AgentCount >= nativeBefore + 3);
            Record("no_active_legacy_enemy_views", FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None).Length == 0);
        }
        else Record("wave_prefab_available", false);
        Finish();
    }
    void ShortenTraining(ProductionBuilding building)
    {
        var queue = m_Bridge.Gameplay.EntityManager.GetBuffer<TrainingRequest>(building.DotsEntity);
        if (queue.Length > 0) { var request = queue[0]; request.Duration = 0.15f; queue[0] = request; }
    }
    int CountKind(GameplayKind kind)
    {
        int count = 0;
        foreach (var unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
            if (unit.IsDotsControlled && m_Bridge.Gameplay.TryGetActor(unit.DotsEntity, out var actor, out _) && actor.Kind == kind) count++;
        return count;
    }
    void TeleportNear(Unit unit, Vector3 point, float offset)
    {
        Vector3 away = (unit.transform.position - point).normalized;
        if (away.sqrMagnitude < 0.1f) away = Vector3.right;
        m_Map.TryResolveWalkableDestination(point + away * offset, out var position);
        var em = m_Bridge.Gameplay.EntityManager; var body = em.GetComponentData<CombatBody>(unit.DotsEntity);
        body.Position = body.PreviousPosition = (Vector2)position; em.SetComponentData(unit.DotsEntity, body);
    }
    IEnumerator WaitUntil(Func<bool> condition, float timeout)
    { float end = Time.realtimeSinceStartup + timeout; while (!condition() && Time.realtimeSinceStartup < end) yield return null; }
    bool TryPlacement(BuildActionSo action, out Vector3 position)
    {
        var bounds = m_Map.WalkableTilemap.cellBounds;
        foreach (var cell in bounds.allPositionsWithin)
        {
            bool valid = true;
            for (int y = -3; y <= 3 && valid; y++)
            for (int x = -3; x <= 3; x++) if (!m_Map.CanPlaceTiles(cell + new Vector3Int(x, y, 0))) { valid = false; break; }
            if (valid) { position = m_Map.WalkableTilemap.GetCellCenterWorld(cell); return true; }
        }
        position = default; return false;
    }
    TowerUnit ClosestTower(Vector3 position)
    {
        TowerUnit found = null; float best = float.MaxValue;
        foreach (var tower in FindObjectsByType<TowerUnit>(FindObjectsSortMode.None))
        { float d = Vector3.SqrMagnitude(tower.transform.position - position); if (d < best) { best = d; found = tower; } }
        return found;
    }
    void Record(string name, bool passed, string details = "") { m_Checks.Add(new Check { name = name, passed = passed, details = details }); }
    void OnLog(string message, string stack, LogType type)
    { if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && m_Errors.Count < 32) m_Errors.Add(message + "\n" + stack); }
    void Update() { if (!m_Finished && m_Output != null && Time.realtimeSinceStartup - m_Started > 100) { Record("watchdog_timeout", false); Finish(); } }
    void Finish()
    {
        if (m_Finished) return; m_Finished = true;
        bool passed = m_Errors.Count == 0; foreach (var check in m_Checks) passed &= check.passed;
        var report = new Report { unity = Application.unityVersion, passed = passed, checks = m_Checks.ToArray(), errors = m_Errors.ToArray(),
            actors = m_Bridge != null ? m_Bridge.Gameplay.ActorCount : 0, enemies = m_Bridge != null ? m_Bridge.Simulation.AgentCount : 0,
            ticks = m_Bridge != null ? m_Bridge.Gameplay.TickCount : 0 };
        File.WriteAllText(Path.Combine(m_Output, "gameplay-result.json"), JsonUtility.ToJson(report, true));
        Debug.Log("Gameplay ECS smoke finished: " + (passed ? "PASS" : "FAIL")); Application.Quit(passed ? 0 : 1);
    }
    void OnDestroy() { Application.logMessageReceived -= OnLog; }
}
