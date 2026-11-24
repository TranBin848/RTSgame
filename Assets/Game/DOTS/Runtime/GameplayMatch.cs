using System;
using Unity.Collections;
using Unity.Entities;

namespace RTS.DOTS
{
    public enum MatchOutcome : byte { Running, Victory, Defeat }

    public partial class GameplaySimulationSystem
    {
        public MatchOutcome Outcome { get; private set; }
        public bool IsMatchOver => Outcome != MatchOutcome.Running;
        public int SurvivalNights { get; private set; }

        public void ConfigureMatch(int survivalNights)
        { RequireSnapshotMutable();
            if (survivalNights < 1) throw new ArgumentOutOfRangeException(nameof(survivalNights));
            if (SurvivalNights != 0 && SurvivalNights != survivalNights)
                throw new InvalidOperationException("Reset the match before changing its objective.");
            SurvivalNights = survivalNights;
        }

        public MatchOutcome EvaluateMatch(int currentDay)
        { RequireSnapshotMutable();
            if (currentDay < 1) throw new ArgumentOutOfRangeException(nameof(currentDay));
            if (SurvivalNights == 0 || IsMatchOver) return Outcome;
            Dependency.Complete();
            bool hasTownHall = false;
            using var depots = m_Depots.ToEntityArray(Allocator.Temp);
            foreach (var entity in depots)
            {
                var body = EntityManager.GetComponentData<CombatBody>(entity);
                if (body.Faction == 0 && body.Health > 0 && EntityManager.GetComponentData<GameplayDepot>(entity).TownHall != 0)
                { hasTownHall = true; break; }
            }
            // Loss takes priority if the last hall dies at the dawn boundary.
            Outcome = !hasTownHall ? MatchOutcome.Defeat : currentDay > SurvivalNights ? MatchOutcome.Victory : MatchOutcome.Running;
            if (IsMatchOver)
            {
                World.GetOrCreateSystemManaged<SwarmSimulationSystem>().Paused = true;
                var research = Research;
                research.Active = 0; research.Elapsed = 0; research.TownHall = Entity.Null;
                EntityManager.SetComponentData(WalletEntity, research);
            }
            return Outcome;
        }
    }
}
