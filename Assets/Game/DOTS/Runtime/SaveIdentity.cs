using System;
using Unity.Entities;

namespace RTS.DOTS
{
    public struct SaveIdentity : IComponentData { public long Value; }

    // Shared by both simulations. ResetSimulation must not reuse IDs while gameplay bodies survive.
    public sealed class SaveIdAllocator
    {
        public long Next { get; private set; }
        public SaveIdAllocator(long next = 1)
        { if (next <= 0 || next == long.MaxValue) throw new ArgumentOutOfRangeException(nameof(next)); Next = next; }
        public long Allocate()
        { if (Next >= long.MaxValue - 1) throw new InvalidOperationException("SaveId exhausted."); return Next++; }
    }
    public partial class SwarmSimulationSystem
    {
        SaveIdAllocator m_SaveIds = new();
        public long NextSaveId => m_SaveIds.Next;
        public long EnsureSaveIdentity(Entity entity)
        { RequireSnapshotMutable();
            if (!EntityManager.Exists(entity)) throw new ArgumentException("Save identity requires a live entity.");
            bool hasIdentity = EntityManager.HasComponent<SaveIdentity>(entity);
            if (hasIdentity && EntityManager.GetComponentData<SaveIdentity>(entity).Value > 0)
                return EntityManager.GetComponentData<SaveIdentity>(entity).Value;
            long id = m_SaveIds.Allocate();
            if (hasIdentity) EntityManager.SetComponentData(entity, new SaveIdentity { Value = id });
            else EntityManager.AddComponentData(entity, new SaveIdentity { Value = id });
            return id;
        }
    }
}
