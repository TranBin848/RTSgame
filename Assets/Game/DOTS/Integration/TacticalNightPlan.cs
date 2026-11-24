using System;
using UnityEngine;

public enum NightApproach { Single, AlternatingWaves, MixedSupport, ExpansionSide, Combined }

[CreateAssetMenu(menuName = "Game/Definitions/Tactical Night Plan")]
public sealed class TacticalNightPlan : ScriptableObject
{
    [Serializable] public sealed class Night
    {
        public string Title, Briefing;
        public int[] Counts;
        public float SpawnInterval, Delay;
        public int RangedEvery;
        public NightApproach Approach;
    }
    public int WaveSeed = 8001;
    public GameObject MeleePrefab, RangedPrefab;
    public Night[] Nights;
    public Night ForDay(int day) => Nights[Mathf.Clamp(day - 1, 0, Nights.Length - 1)];
    public void Validate()
    {
        if (MeleePrefab == null || RangedPrefab == null || MeleePrefab.GetComponent<EnemyUnit>() == null
            || RangedPrefab.GetComponent<EnemyRangerUnit>() == null || Nights == null || Nights.Length != 5)
            throw new InvalidOperationException("Tactical plan requires existing melee/ranged prefabs and exactly five nights.");
        foreach (var night in Nights)
        {
            if (night == null || string.IsNullOrWhiteSpace(night.Title) || string.IsNullOrWhiteSpace(night.Briefing)
                || night.Counts == null || night.Counts.Length < 1 || night.Counts.Length > 8 || !float.IsFinite(night.SpawnInterval)
                || night.SpawnInterval < .05f || !float.IsFinite(night.Delay) || night.Delay < 0 || night.RangedEvery < 0
                || !Enum.IsDefined(typeof(NightApproach), night.Approach)) throw new InvalidOperationException("Invalid tactical night definition.");
            foreach (int count in night.Counts) if (count < 1 || count > 128) throw new InvalidOperationException("Tactical wave count outside1..128.");
        }
    }
    public int PrefabSlot(int day, int wave, int ordinal)
    {
        int every = ForDay(day).RangedEvery;
        return every > 0 && (ordinal + wave) % every == every - 1 ? 1 : 0;
    }
    // Integer hash is independent of UnityEngine.Random, VFX, camera and UI callback order.
    public int PrimaryGateway(int day, int mapSeed, int gatewayCount)
    {
        if (gatewayCount <= 0) return -1;
        unchecked
        {
            uint value = (uint)WaveSeed ^ ((uint)mapSeed * 0x9e3779b9u) ^ ((uint)day * 0x85ebca6bu);
            value ^= value >> 16; value *= 0x7feb352du; value ^= value >> 15;
            return (int)(value % (uint)gatewayCount);
        }
    }
    public int Gateway(int day, int wave, int ordinal, int mapSeed, int count, int expansionGateway)
    {
        int primary = PrimaryGateway(day, mapSeed, count); if (primary < 0) return -1;
        return ForDay(day).Approach switch
        {
            NightApproach.Single => primary,
            NightApproach.AlternatingWaves or NightApproach.MixedSupport => (primary + wave) % count,
            NightApproach.ExpansionSide => Mathf.Clamp(expansionGateway, 0, count - 1),
            _ => (primary + wave + ordinal % 2) % count
        };
    }
}
