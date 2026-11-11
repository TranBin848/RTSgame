using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RTS.DOTS
{
    public enum ResearchResult : byte { Started, Unavailable, InvalidTechnology, InvalidTownHall, Busy, AlreadyUnlocked, InsufficientResources, MissingPrerequisite }
    [Serializable]
    public struct ResearchDefinition
    {
        public string Id, Prerequisite;
        public int Wood, Gold, Meat;
        public float Duration, WoodMultiplier;
        public float CargoMultiplier, TrainingMultiplier, TowerMultiplier;
        public void Validate()
        {
            if (Wood < 0 || Gold < 0 || Meat < 0 || !math.isfinite(Duration) || Duration <= 0
                || !math.isfinite(WoodMultiplier) || WoodMultiplier < 1)
                throw new ArgumentException("Invalid research price, duration or multiplier.");
            if (!math.isfinite(CargoMultiplier) || CargoMultiplier < 0 || (CargoMultiplier > 0 && CargoMultiplier < 1)
                || !math.isfinite(TowerMultiplier) || TowerMultiplier < 0 || (TowerMultiplier > 0 && TowerMultiplier < 1)
                || !math.isfinite(TrainingMultiplier) || TrainingMultiplier < 0 || TrainingMultiplier > 1)
                throw new ArgumentException("Invalid cargo, tower or training modifier.");
        }
    }
    public struct PlayerResearch : IComponentData
    {
        public Entity TownHall;
        public float Elapsed;
        public byte Active, Unlocked;
        public uint UnlockMask;
        public int Technology;
    }

    public partial class GameplaySimulationSystem
    {
        public const string EconomyToolsId = "economy_tools";
        ResearchDefinition m_ResearchDefinition;
        ResearchDefinition[] m_ResearchCatalog;
        bool m_ResearchConfigured;
        public bool CanUseArchery => !m_ResearchConfigured || IsTechnologyUnlocked("military_archery");
        public bool CanUseTowers => !m_ResearchConfigured || IsTechnologyUnlocked("expansion_tower");
        public float CargoMultiplier => Modifier("economy_cargo", 0);
        public float TrainingMultiplier => Modifier("military_training", 1);
        public float TowerMultiplier => Modifier("expansion_fortify", 2);
        float Modifier(string id, int kind)
        {
            if (!IsTechnologyUnlocked(id)) return 1;
            var definition = GetResearchDefinition(id);
            float value = kind == 0 ? definition.CargoMultiplier : kind == 1 ? definition.TrainingMultiplier : definition.TowerMultiplier;
            return value == 0 ? 1 : value;
        }
        public PlayerResearch Research => EntityManager.GetComponentData<PlayerResearch>(WalletEntity);
        public float ResearchDuration => !m_ResearchConfigured ? 0 : Research.Active != 0 ? m_ResearchCatalog[Research.Technology].Duration : m_ResearchDefinition.Duration;
        public ResearchDefinition ResearchSettings => m_ResearchDefinition;
        public string ActiveResearchId => Research.Active != 0 ? m_ResearchCatalog[Research.Technology].Id : null;
        public void ConfigureResearch(ResearchDefinition definition)
        {
            definition.Id = EconomyToolsId;
            ConfigureResearchCatalog(new[] { definition });
        }
        public void ConfigureResearchCatalog(ResearchDefinition[] definitions)
        {
            ValidateResearchCatalog(definitions);
            Dependency.Complete();
            if (m_ResearchConfigured) throw new InvalidOperationException("Research is already configured for this match.");
            m_ResearchCatalog = (ResearchDefinition[])definitions.Clone();
            m_ResearchDefinition = m_ResearchCatalog[FindTechnology(EconomyToolsId)]; m_ResearchConfigured = true;
        }
        public static void ValidateResearchCatalog(ResearchDefinition[] definitions)
        {
            if (definitions == null || definitions.Length == 0 || definitions.Length > 6)
                throw new ArgumentException("Research catalog must contain 1 to 6 supported technologies.");
            var ids = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                definition.Validate();
                if (!IsSupportedTechnology(definition.Id) || ids.ContainsKey(definition.Id))
                    throw new ArgumentException("Unknown or duplicate technology ID.");
                ids.Add(definition.Id, ids.Count);
            }
            if (!ids.ContainsKey(EconomyToolsId)) throw new ArgumentException("Catalog requires economy_tools.");
            for (int i = 0; i < definitions.Length; i++)
            {
                var prerequisite = definitions[i].Prerequisite;
                int traversed = 0;
                while (!string.IsNullOrEmpty(prerequisite))
                {
                    if (!ids.TryGetValue(prerequisite, out var parent)) throw new ArgumentException("Missing prerequisite.");
                    if (++traversed > definitions.Length) throw new ArgumentException("Cyclic research prerequisites.");
                    prerequisite = definitions[parent].Prerequisite;
                }
            }
        }
        static bool IsSupportedTechnology(string id) => id == EconomyToolsId || id == "economy_cargo" || id == "military_archery"
            || id == "military_training" || id == "expansion_tower" || id == "expansion_fortify";
        int FindTechnology(string id)
        {
            if (m_ResearchCatalog != null) for (int i = 0; i < m_ResearchCatalog.Length; i++) if (m_ResearchCatalog[i].Id == id) return i;
            return -1;
        }
        public ResearchDefinition GetResearchDefinition(string id)
        {
            int index = FindTechnology(id);
            if (index < 0) throw new ArgumentException("Unknown technology ID.", nameof(id));
            return m_ResearchCatalog[index];
        }
        public bool IsTechnologyUnlocked(string id)
        {
            int index = FindTechnology(id);
            return index >= 0 && (Research.UnlockMask & (1u << index)) != 0;
        }
        public ResearchResult StartResearch(string id, Entity townHall)
        {
            Dependency.Complete();
            if (!IsConfigured || !m_ResearchConfigured || World.GetOrCreateSystemManaged<SwarmSimulationSystem>().Paused)
                return ResearchResult.Unavailable;
            int technology = FindTechnology(id);
            if (technology < 0) return ResearchResult.InvalidTechnology;
            if (!CanResearch(townHall)) return ResearchResult.InvalidTownHall;
            var state = Research;
            if (state.Active != 0) return ResearchResult.Busy;
            if (IsTechnologyUnlocked(id)) return ResearchResult.AlreadyUnlocked;
            var wallet = Wallet; var cost = m_ResearchCatalog[technology];
            if (!string.IsNullOrEmpty(cost.Prerequisite) && !IsTechnologyUnlocked(cost.Prerequisite)) return ResearchResult.MissingPrerequisite;
            if (wallet.Wood < cost.Wood || wallet.Gold < cost.Gold || wallet.Meat < cost.Meat)
                return ResearchResult.InsufficientResources;
            wallet.Wood -= cost.Wood; wallet.Gold -= cost.Gold; wallet.Meat -= cost.Meat;
            EntityManager.SetComponentData(WalletEntity, wallet);
            state.TownHall = townHall; state.Active = 1; state.Elapsed = 0; state.Technology = technology;
            EntityManager.SetComponentData(WalletEntity, state);
            return ResearchResult.Started;
        }
        bool CanResearch(Entity hall)
        {
            return EntityManager.Exists(hall) && EntityManager.HasComponent<GameplayDepot>(hall)
                && EntityManager.GetComponentData<GameplayDepot>(hall).TownHall != 0
                && EntityManager.HasComponent<CombatBody>(hall)
                && EntityManager.GetComponentData<CombatBody>(hall).Faction == 0
                && EntityManager.GetComponentData<CombatBody>(hall).Health > 0
                && EntityManager.HasComponent<GameplayConstruction>(hall)
                && EntityManager.GetComponentData<GameplayConstruction>(hall).Active == 0;
        }
        void TickResearch(float deltaTime)
        {
            var state = Research;
            if (state.Active == 0) return;
            if (!CanResearch(state.TownHall)) { state.Active = 0; state.Elapsed = 0; state.TownHall = Entity.Null; }
            else
            {
                float duration = m_ResearchCatalog[state.Technology].Duration;
                state.Elapsed = math.min(duration, state.Elapsed + deltaTime);
                if (state.Elapsed >= duration)
                {
                    state.Active = 0; state.UnlockMask |= 1u << state.Technology;
                    if (m_ResearchCatalog[state.Technology].Id == EconomyToolsId) state.Unlocked = 1;
                    state.TownHall = Entity.Null;
                }
            }
            EntityManager.SetComponentData(WalletEntity, state);
        }
        public static int EffectiveGatherAmount(int amount, float multiplier)
            => amount <= 0 ? 0 : math.max(1, (int)math.floor(amount * multiplier + 0.5f));
    }
}
