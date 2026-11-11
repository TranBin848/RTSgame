using System;
using System.Linq;
using Unity.Core;
using Unity.Entities;
using UnityEngine;
using RTS.DOTS;

public static class S3Validation
{
    public static string Run()
    {
        var root = UnityEngine.Object.FindFirstObjectByType<GameSceneCompositionRoot>();
        var bridge = SwarmGameBridge.Instance;
        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var hall = UnityEngine.Object.FindFirstObjectByType<TownHall>();
        var world = World.DefaultGameObjectInjectionWorld;
        var original = world.Time;
        var ids = new[] { "economy_tools", "economy_cargo", "military_archery", "military_training", "expansion_tower", "expansion_fortify" };
        if (!root.IsReady || UnityEngine.Object.FindObjectsByType<ResearchPanel>(FindObjectsSortMode.None).Length != 6)
            throw new Exception("Expected ready root and six research panels.");
        if (bridge.Gameplay.CanUseArchery || bridge.Gameplay.CanUseTowers) throw new Exception("Capabilities must start locked in a new match.");
        if (root.Research.Start("economy_cargo", hall) != ResearchResult.MissingPrerequisite) throw new Exception("Prerequisite bypass.");
        manager.AddResources(2000, 2000, 2000);
        manager.ActiveUnit = hall;
        var originalHall = bridge.Gameplay.EntityManager.GetComponentData<CombatBody>(hall.DotsEntity);
        var protectedHall = originalHall; protectedHall.Health = 1000000;
        bridge.Gameplay.EntityManager.SetComponentData(hall.DotsEntity, protectedHall);
        int ticks = 0;
        try
        {
            foreach (var id in ids)
            {
                var cost = root.Research.GetDefinition(id);
                int wood = manager.Wood, gold = manager.Gold, meat = manager.Meat;
                if (root.Research.Start(id, hall) != ResearchResult.Started) throw new Exception("Start failed: " + id);
                if (root.Research.Start(id, hall) != ResearchResult.Busy) throw new Exception("Slot bypass.");
                if (manager.Wood != wood - cost.Wood || manager.Gold != gold - cost.Gold || manager.Meat != meat - cost.Meat)
                    throw new Exception("Incorrect debit: " + id);
                int count = (int)Math.Ceiling(cost.Duration / 0.05) + 2;
                for (int i = 0; i < count; i++)
                {
                    ticks++; world.SetTime(new TimeData(original.ElapsedTime + ticks * 0.05, 0.05f));
                    bridge.Gameplay.Update(); bridge.Simulation.Update();
                }
                if (!root.Research.IsUnlocked(id)) throw new Exception("Completion failed: " + id);
            }
            foreach (var panel in UnityEngine.Object.FindObjectsByType<ResearchPanel>(FindObjectsSortMode.None)) panel.SendMessage("Update");
            if (!bridge.Gameplay.CanUseArchery || !bridge.Gameplay.CanUseTowers || bridge.Gameplay.CargoMultiplier != 1.5f
                || bridge.Gameplay.TrainingMultiplier != 0.8f || bridge.Gameplay.TowerMultiplier != 1.25f)
                throw new Exception("Effective modifiers do not match catalog.");
            return $"PASS six research transactions, prerequisites, busy guard, unlocks, modifiers, six panel refreshes; mask={bridge.Gameplay.Research.UnlockMask}";
        }
        finally
        {
            world.SetTime(original);
            if (bridge.Gameplay.EntityManager.Exists(hall.DotsEntity)) bridge.Gameplay.EntityManager.SetComponentData(hall.DotsEntity, originalHall);
        }
    }
}
