using System;
using UnityEngine;

[CreateAssetMenu(fileName = "WorkerUnitDefinition", menuName = "Game/Definitions/Worker Unit Definition")]
public class WorkerUnitDefinition : ScriptableObject
{
    [SerializeField] private GatheringProfile[] m_GatheringProfiles;

    public bool TryGetProfile(ResourceType resourceType, out GatheringProfile profile)
    {
        foreach (var candidate in m_GatheringProfiles)
        {
            if (candidate.ResourceType == resourceType)
            {
                profile = candidate;
                return true;
            }
        }

        profile = default;
        return false;
    }

    [Serializable]
    public struct GatheringProfile
    {
        [SerializeField] private ResourceType m_ResourceType;
        [SerializeField] private UnitTask m_GatherTask;
        [SerializeField] private UnitState m_GatherState;
        [SerializeField] private float m_GatherTickTime;
        [SerializeField] private int m_ResourcePerTick;
        [SerializeField] private int m_CarryCapacity;
        [SerializeField] private float m_HitFrequency;
        [SerializeField] private float m_CarryAnimatorValue;
        [SerializeField] private string m_GatherAnimationBool;
        [SerializeField] private Color m_DeliveryPopupColor;

        public ResourceType ResourceType => m_ResourceType;
        public UnitTask GatherTask => m_GatherTask;
        public UnitState GatherState => m_GatherState;
        public float GatherTickTime => m_GatherTickTime;
        public int ResourcePerTick => m_ResourcePerTick;
        public int CarryCapacity => m_CarryCapacity;
        public float HitFrequency => m_HitFrequency;
        public float CarryAnimatorValue => m_CarryAnimatorValue;
        public string GatherAnimationBool => m_GatherAnimationBool;
        public Color DeliveryPopupColor => m_DeliveryPopupColor;
    }
}
