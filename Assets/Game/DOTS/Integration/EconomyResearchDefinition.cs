using System;
using RTS.DOTS;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Progression/Economy Tools")]
public sealed class EconomyResearchDefinition : ScriptableObject
{
    public string Id = GameplaySimulationSystem.EconomyToolsId;
    public int Wood = 40, Gold = 15, Meat;
    public float Duration = 20, WoodMultiplier = 1.25f;
    public ResearchDefinition[] AdditionalTechnologies = Defaults();
    public static ResearchDefinition[] Defaults() => new[] {
        new ResearchDefinition { Id="economy_cargo", Prerequisite="economy_tools",Wood=80,Gold=30,Duration=30,WoodMultiplier=1,CargoMultiplier=1.5f },
        new ResearchDefinition { Id="military_archery",Wood=50,Gold=30,Meat=10,Duration=25,WoodMultiplier=1 },
        new ResearchDefinition { Id="military_training",Prerequisite="military_archery",Wood=80,Gold=60,Meat=20,Duration=35,WoodMultiplier=1,TrainingMultiplier=0.8f },
        new ResearchDefinition { Id="expansion_tower",Wood=60,Gold=25,Duration=25,WoodMultiplier=1 },
        new ResearchDefinition { Id="expansion_fortify",Prerequisite="expansion_tower",Wood=100,Gold=60,Duration=40,WoodMultiplier=1,TowerMultiplier=1.25f }
    };
    public ResearchDefinition[] ToCatalog()
    {
        var first = ToData(); first.Id = Id;
        var extra = AdditionalTechnologies ?? Array.Empty<ResearchDefinition>();
        var catalog = new ResearchDefinition[extra.Length + 1]; catalog[0] = first;
        Array.Copy(extra, 0, catalog, 1, extra.Length);
        GameplaySimulationSystem.ValidateResearchCatalog(catalog); return catalog;
    }
    public ResearchDefinition ToData()
    {
        if (Id != GameplaySimulationSystem.EconomyToolsId)
            throw new ArgumentException("This first research catalog supports only economy_tools.");
        var data = new ResearchDefinition { Wood = Wood, Gold = Gold, Meat = Meat, Duration = Duration, WoodMultiplier = WoodMultiplier };
        data.Validate(); return data;
    }
}
