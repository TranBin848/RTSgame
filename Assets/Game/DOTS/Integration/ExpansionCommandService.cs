using System;
using System.Collections.Generic;
using RTS.DOTS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct GroupCommandSummary
{
    public int Accepted, AlreadyReturning, Rejected;
    public override string ToString() => $"Nhận {Accepted} · Đã về/đang về {AlreadyReturning} · Từ chối {Rejected}";
}

public sealed class ExpansionCommandService : IDisposable
{
    readonly SwarmGameBridge m_Bridge;
    readonly GameManager m_Game;
    readonly FogOfWarManager m_Fog;
    readonly ProceduralMapAuthoring m_Map;
    readonly EntityQuery m_Resources;
    readonly List<Unit> m_Group = new();
    bool m_Disposed;
    public ExpansionCommandService(SwarmGameBridge bridge, GameManager game, FogOfWarManager fog, ProceduralMapAuthoring map)
    {
        m_Bridge = bridge; m_Game = game; m_Fog = fog; m_Map = map;
        m_Resources = bridge.Gameplay.EntityManager.CreateEntityQuery(typeof(GameplayResource));
    }
    bool Known(Vector2 p) => m_Fog.IsInitialized && m_Fog.TryGetCellState(m_Fog.FogTilemap.WorldToCell(p), out var state, out _) && state != FogCellState.Unexplored;
    bool Allowed => Time.timeScale > 0 && m_Bridge.IsSceneBound && !m_Bridge.Simulation.Paused && !m_Bridge.Gameplay.IsMatchOver && !ProceduralMapRuntime.IsCompositionBlocked;
    public GroupCommandSummary RecallWorkers()
    {
        var result = new GroupCommandSummary(); m_Group.Clear();
        foreach (var u in m_Game.GetFriendlyUnits(true)) if (u is WorkerUnit) m_Group.Add(u);
        foreach (var unit in m_Group)
        {
            if (!Allowed || unit == null || !unit.isActiveAndEnabled || !unit.IsDotsControlled) { result.Rejected++; continue; }
            var worker = (WorkerUnit)unit;
            TownHall best = null; float nearest = float.MaxValue; bool already = false;
            foreach (var structure in m_Game.GetPlayerStructures())
            {
                if (structure is not TownHall hall || !hall.IsDotsControlled) continue;
                var status = m_Bridge.Gameplay.CanRecallWorker(worker.DotsEntity, hall.DotsEntity);
                if (status == GroupCommandResult.AlreadyReturning) { already = true; break; }
                if (status != GroupCommandResult.Accepted) continue;
                m_Bridge.Gameplay.TryGetActor(worker.DotsEntity, out _, out var body);
                m_Bridge.Gameplay.TryGetActor(hall.DotsEntity, out _, out var target);
                if (m_Bridge.Gameplay.TryLogisticsRoute(body.Position, body.Radius, target, .55f, out float distance, out _) && distance < nearest)
                { nearest = distance; best = hall; }
            }
            if (already) result.AlreadyReturning++;
            else if (best != null && worker.TryRecallToTownHall(best) == GroupCommandResult.Accepted) result.Accepted++;
            else result.Rejected++;
        }
        return result;
    }
    public GroupCommandSummary RallyArmy(Vector2 destination)
    {
        var result = new GroupCommandSummary(); m_Group.Clear();
        foreach (var unit in m_Game.GetFriendlyUnits(true)) if (unit is WarriorUnit || unit is RangerUnit) m_Group.Add(unit);
        var candidates = new List<Vector2>();
        if (Known(destination))
        {
            candidates.Add(destination);
            var cell = m_Fog.FogTilemap.WorldToCell(destination);
            for (int y = -2; y <= 2; y++) for (int x = -2; x <= 2; x++)
            {
                Vector2 point = m_Fog.FogTilemap.GetCellCenterWorld(cell + new Vector3Int(x, y, 0));
                if (Known(point)) candidates.Add(point);
            }
            candidates.Sort((a, b) => ((a - destination).sqrMagnitude).CompareTo((b - destination).sqrMagnitude));
        }
        foreach (var unit in m_Group)
        {
            if (!Allowed || !Known(destination) || unit == null || !unit.isActiveAndEnabled || !unit.IsDotsControlled)
            { result.Rejected++; continue; }
            bool accepted = false;
            foreach (var point in candidates)
            {
                var status = m_Bridge.Gameplay.TryRallyActor(unit.DotsEntity, point);
                if (status == GroupCommandResult.Accepted) { accepted = true; break; }
                if (status == GroupCommandResult.InvalidActor || status == GroupCommandResult.Unavailable) break;
            }
            if (accepted) result.Accepted++; else result.Rejected++;
        }
        return result;
    }
    public string Preview(PlacementProcess placement)
    {
        if (placement == null) return "Chọn xây kho để xem đường lưới và chi phí.\nTập kết quân: ô hợp lệ quanh tâm màn hình (±2 ô), đã khám phá.";
        var action = placement.BuildAction; var p = placement.PreviewPosition;
        string cost = $"Xây: Gỗ {action.WoodCost} · Vàng {action.GoldCost}";
        if (m_Game.Wood < action.WoodCost || m_Game.Gold < action.GoldCost) cost += " (chưa đủ)";
        var min = (Vector2)p + (Vector2)(Vector3)action.OriginalOffset;
        for (int y = 0; y < action.BuildingSize.y; y++) for (int x = 0; x < action.BuildingSize.x; x++)
            if (!Known(min + new Vector2(x + .5f, y + .5f))) return cost + "\nChưa khám phá toàn bộ vị trí · không gợi ý.";
        if (!placement.PreviewAreaValid) return cost + "\nKhông đặt được tại vị trí này.";
        var gp = m_Bridge.Gameplay; var em = gp.EntityManager;
        if (!m_Game.CanBuild(action)) return cost + "\nChưa mở công nghệ cần thiết.";
        if (m_Game.ActiveUnit is not WorkerUnit worker || !worker.IsDotsControlled || !gp.TryGetActor(worker.DotsEntity, out _, out var workerBody) || workerBody.Health <= 0)
            return cost + "\nChọn dân còn sống để kiểm đường.";
        var firstCell = new Vector3Int(Mathf.RoundToInt(min.x), Mathf.RoundToInt(min.y), 0);
        float2 blockedMin = (Vector2)m_Fog.FogTilemap.CellToWorld(firstCell);
        float2 blockedMax = (Vector2)m_Fog.FogTilemap.CellToWorld(firstCell + new Vector3Int(action.BuildingSize.x, action.BuildingSize.y, 0));
        var target = new CombatBody { Position = ((float2)blockedMin + blockedMax) * .5f };
        bool approach = gp.TryLogisticsRoute(workerBody.Position, workerBody.Radius, target, .6f, out float buildDistance, out var entrance, blockedMin, blockedMax, true);
        if (!approach) return cost + "\nKhông có đường lưới tới mép công trình dự kiến.";
        GameplayResource closest = default; float best = float.MaxValue; bool found = false;
        using var resources = m_Resources.ToComponentDataArray<GameplayResource>(Unity.Collections.Allocator.Temp);
        int mask = (action.StructureUnitPrefab.CanStoreWood ? 1 : 0) | (action.StructureUnitPrefab.CanStoreGold ? 2 : 0) | (action.StructureUnitPrefab.CanStoreMeat ? 4 : 0);
        foreach (var node in resources)
        {
            if ((mask & (1 << node.Type)) == 0 || !Known(node.Position)) continue;
            float d = math.distancesq(node.Position, entrance);
            if (d < best) { closest = node; best = d; found = true; }
        }
        string message = cost + $"\nDân → mép xây: {buildDistance:0.#} đơn vị lưới.";
        if (!found) message += "\nChưa có tài nguyên phù hợp đã khám phá.";
        else
        {
            var nodeBody = new CombatBody { Position = closest.Position, Radius = closest.Radius };
            bool route = gp.TryLogisticsRoute(entrance, workerBody.Radius, nodeBody, .08f, out float nodeDistance, out var nodeEntrance, blockedMin, blockedMax);
            message += route ? $"\nMép kho → tài nguyên gần nhất: {nodeDistance:0.#} đơn vị lưới." : "\nTài nguyên gần nhất: không có đường hợp lệ.";
            float depotDistance = float.MaxValue;
            if (route) foreach (var structure in m_Game.GetPlayerStructures())
            {
                if (structure == null || !structure.IsDotsControlled || !Known(structure.transform.position) || !gp.TryGetActor(structure.DotsEntity, out _, out var depotBody)
                    || depotBody.Health <= 0 || !em.HasComponent<GameplayDepot>(structure.DotsEntity)
                    || (em.GetComponentData<GameplayDepot>(structure.DotsEntity).ResourceMask & (1 << closest.Type)) == 0
                    || em.GetComponentData<GameplayConstruction>(structure.DotsEntity).Active != 0) continue;
                if (gp.TryLogisticsRoute(nodeEntrance, workerBody.Radius, depotBody, .55f, out float distance, out _, blockedMin, blockedMax)) depotDistance = Mathf.Min(depotDistance, distance);
            }
            message += depotDistance < float.MaxValue ? $"\nTài nguyên → kho hiện có: {depotDistance:0.#} đơn vị lưới." : "\nKho hiện có: chưa tìm thấy đường nộp phù hợp.";
            if (closest.RequiresKill != 0) message += "\nNguồn thịt cần hạ mục tiêu trước.";
        }
        bool exposed = false;
        foreach (var gate in m_Map.GeneratedMap.Layout.Gateways)
        {
            var world = m_Fog.FogTilemap.GetCellCenterWorld(m_Map.OriginCell + new Vector3Int(gate.X, gate.Y, 0));
            if (Known(world) && Vector2.Distance(world, p) <= 12) exposed = true;
        }
        message += exposed ? "\nGần gateway đã khám phá (≤12 world units): cần phòng thủ." : "\nChưa đánh giá được độ an toàn từ thông tin đã khám phá.";
        return message;
    }
    public void Dispose()
    {
        if (m_Disposed) return; m_Disposed = true;
        if (m_Bridge.Gameplay.World != null && m_Bridge.Gameplay.World.IsCreated) m_Resources.Dispose();
    }
}
