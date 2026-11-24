using Unity.Entities;
using Unity.Mathematics;

namespace RTS.DOTS
{
    public struct ResourceTotals
    {
        public long Wood, Gold, Meat;
        public void Add(int type, long amount)
        { if (type == 0) Wood += amount; else if (type == 1) Gold += amount; else Meat += amount; }
        public static ResourceTotals operator -(ResourceTotals a, ResourceTotals b)
            => new() { Wood = a.Wood - b.Wood, Gold = a.Gold - b.Gold, Meat = a.Meat - b.Meat };
    }

    public struct MatchStatistics : IComponentData
    {
        public ResourceTotals Deposited, Spent, Granted;
        public int UnitsLost, ResearchCompleted;
        public float TownHallDamage;
        public static MatchStatistics operator -(MatchStatistics a, MatchStatistics b) => new()
        {
            Deposited = a.Deposited - b.Deposited, Spent = a.Spent - b.Spent, Granted = a.Granted - b.Granted,
            UnitsLost = a.UnitsLost - b.UnitsLost, ResearchCompleted = a.ResearchCompleted - b.ResearchCompleted,
            TownHallDamage = a.TownHallDamage - b.TownHallDamage
        };
    }

    public partial class GameplaySimulationSystem
    {
        public MatchStatistics Statistics => EntityManager.GetComponentData<MatchStatistics>(WalletEntity);
        void RecordWalletAdjustment(int wood, int gold, int meat)
        {
            var stats = Statistics;
            void Add(int type, int amount)
            { if (amount < 0) stats.Spent.Add(type, -(long)amount); else stats.Granted.Add(type, amount); }
            Add(0, wood); Add(1, gold); Add(2, meat);
            EntityManager.SetComponentData(WalletEntity, stats);
        }
        // Called once per resolved damage batch, before writing health; healing cannot erase this counter.
        internal void RecordCombatStatistics(Entity target, CombatBody before, int damage)
        {
            if (damage <= 0 || before.Health <= 0 || before.Faction != 0 || !EntityManager.HasComponent<GameplayActor>(target)) return;
            var actor = EntityManager.GetComponentData<GameplayActor>(target);
            var stats = Statistics;
            if (EntityManager.HasComponent<GameplayDepot>(target) && EntityManager.GetComponentData<GameplayDepot>(target).TownHall != 0)
                stats.TownHallDamage += math.min(before.Health, damage);
            if (before.Health <= damage && (actor.Kind == GameplayKind.Worker || actor.Kind == GameplayKind.Soldier || actor.Kind == GameplayKind.Archer))
                stats.UnitsLost++;
            EntityManager.SetComponentData(WalletEntity, stats);
        }
    }
}
