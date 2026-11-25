using NUnit.Framework;

namespace RTS.ProceduralGeneration.Tests
{
    public sealed class ProceduralMapGeneratorTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(1543)] [TestCase(-1)] [TestCase(int.MinValue)]
        public void FourFrontiersHasDeterministicBudgetsAndClearCenters(int seed)
        {
            var settings=new MapGenerationSettingsData {Preset=MapLayoutPreset.FourFrontiers,Seed=seed};
            var generator=new ProceduralMapGenerator();
            Assert.That(generator.TryGenerate(settings,out var a,out var error),Is.True,error);
            Assert.That(generator.TryGenerate(settings,out var b,out error),Is.True,error);
            Assert.That(a.GeneratorVersion,Is.EqualTo(4)); Assert.That(a.Layout.Regions.Length,Is.EqualTo(5));
            Assert.That(a.Layout.Connections.Length,Is.EqualTo(4));Assert.That(a.Layout.Gateways.Length,Is.EqualTo(2));
            Assert.That(b.Terrain,Is.EqualTo(a.Terrain));Assert.That(b.Reserved,Is.EqualTo(a.Reserved));Assert.That(b.Resources,Is.EqualTo(a.Resources));
            Assert.That(b.Layout.RegionIds,Is.EqualTo(a.Layout.RegionIds));Assert.That(b.Layout.Routes,Is.EqualTo(a.Layout.Routes));
            Assert.That(b.Layout.Regions,Is.EqualTo(a.Layout.Regions));Assert.That(b.Layout.Gateways,Is.EqualTo(a.Layout.Gateways));
            Assert.That(a.Validation.ReachableGateways,Is.EqualTo(2));
            Assert.That(ProceduralMapGenerator.ValidateStrategicMap(settings,a).IsValid,Is.True);
            for(int id=1;id<5;id++)foreach(ProceduralResourceKind kind in System.Enum.GetValues(typeof(ProceduralResourceKind)))
            {
                int count=0;foreach(var r in a.Resources)if(!r.IsStartingResource&&r.Kind==kind&&a.Layout.RegionIds[a.Index(r.X,r.Y)]==id)count++;
                Assert.That(count,Is.EqualTo(ProceduralMapGenerator.FourFrontiersBudget(settings,id,kind)),id+" "+kind);
            }
        }
        [Test] public void FourFrontiersExhaustionKeepsEightAttemptLimit()
        {
            var settings=new MapGenerationSettingsData {Preset=MapLayoutPreset.FourFrontiers,ResourcePlacementAttempts=1};
            Assert.That(new ProceduralMapGenerator().TryGenerate(settings,out var map,out var error),Is.False);
            Assert.That(map,Is.Null);Assert.That(error,Does.Contain("after 8 attempts"));
        }
        [Test] public void FourFrontiersMinimumGoldSplitsThreeAndOne()
        {
            var settings=new MapGenerationSettingsData {Preset=MapLayoutPreset.FourFrontiers,TotalGoldCount=7};
            Assert.That(ProceduralMapGenerator.FourFrontiersBudget(settings,2,ProceduralResourceKind.Gold),Is.EqualTo(3));
            Assert.That(ProceduralMapGenerator.FourFrontiersBudget(settings,4,ProceduralResourceKind.Gold),Is.EqualTo(1));
            Assert.That(new ProceduralMapGenerator().TryGenerate(settings,out var map,out var error),Is.True,error);
            settings.TotalGoldCount=6;
            Assert.That(MapGenerationSettingsValidator.Validate(settings),Is.Not.Null);
        }
        [TestCase("connection")] [TestCase("gateway")] [TestCase("role")] [TestCase("fourth-region")]
        public void FourFrontiersRejectsBrokenTopology(string fixture)
        {
            var settings=new MapGenerationSettingsData {Preset=MapLayoutPreset.FourFrontiers};
            Assert.That(new ProceduralMapGenerator().TryGenerate(settings,out var map,out _),Is.True);
            if(fixture=="connection")map.Layout.Connections[3]=map.Layout.Connections[0];
            if(fixture=="gateway")map.Layout.Gateways[1]=map.Layout.Gateways[0];
            if(fixture=="role")map.Layout.Regions[4].Role=RegionRole.Economy;
            if(fixture=="fourth-region")
            {var r=map.Layout.Regions[4];map.Terrain[map.Index(r.X,r.Y)]=0;}
            Assert.That(ProceduralMapGenerator.ValidateStrategicMap(settings,map).IsValid,Is.False);
        }

        [Test] public void SnapshotLayoutClonesTopologyRatherThanRegeneratingIt()
        {
            var regions = new[] { new MapRegion { X = 1 }, new MapRegion { X = 2 }, new MapRegion { X = 3 } };
            var connections = new[] { new RegionConnection { From = 0, To = 1 }, new RegionConnection { From = 0, To = 2 } };
            var gateways = new[] { new MapGateway { X = 4 }, new MapGateway { X = 5 } };
            var cells = new byte[] { 0, 1, 2 };
            var saved = StrategicLayout.FromSnapshot(regions, connections, gateways, new MapLandmark { X = 9 }, cells, new byte[3]);
            cells[0] = 2; regions[0] = default;
            Assert.That(saved.RegionIds[0], Is.EqualTo(0)); Assert.That(saved.Regions[0].X, Is.EqualTo(1)); Assert.That(saved.Landmark.X, Is.EqualTo(9));
            Assert.Throws<System.ArgumentException>(() => StrategicLayout.FromSnapshot(null, connections, gateways, default, cells, new byte[3]));
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(int.MinValue)] [TestCase(1543)]
        public void StrategicLayoutIsDeterministicAndProtectsRequiredPlaces(int seed)
        {
            var data=new MapGenerationSettingsData {Seed=seed,Preset=MapLayoutPreset.StrategicRegions};
            var generator=new ProceduralMapGenerator();
            Assert.That(generator.TryGenerate(data,out var a,out var error),Is.True,error);
            Assert.That(generator.TryGenerate(data,out var b,out error),Is.True,error);
            Assert.That(a.GeneratorVersion,Is.EqualTo(3));Assert.That(a.Layout.Regions.Length,Is.EqualTo(3));Assert.That(a.Layout.Gateways.Length,Is.EqualTo(2));
            Assert.That(b.Terrain,Is.EqualTo(a.Terrain));Assert.That(b.Reserved,Is.EqualTo(a.Reserved));
            Assert.That(b.Layout.RegionIds,Is.EqualTo(a.Layout.RegionIds));Assert.That(b.Layout.Routes,Is.EqualTo(a.Layout.Routes));
            Assert.That(b.Layout.Regions,Is.EqualTo(a.Layout.Regions));Assert.That(b.Layout.Connections,Is.EqualTo(a.Layout.Connections));
            Assert.That(b.Layout.Gateways,Is.EqualTo(a.Layout.Gateways));Assert.That(b.Layout.Landmark,Is.EqualTo(a.Layout.Landmark));
            Assert.That(b.Resources,Is.EqualTo(a.Resources));
            foreach(var gate in a.Layout.Gateways){Assert.That(a.IsGrass(gate.X,gate.Y),Is.True);Assert.That(a.Reserved[a.Index(gate.X,gate.Y)],Is.EqualTo(1));}
            var landmark=a.Layout.Landmark;
            Assert.That(System.Math.Abs(landmark.X-a.StartX)+System.Math.Abs(landmark.Y-a.StartY),Is.GreaterThan(data.StartClearRadius+landmark.Radius));
            foreach(var resource in a.Resources)Assert.That(a.Reserved[a.Index(resource.X,resource.Y)],Is.Zero);
        }
        [Test] public void LegacyPresetKeepsVersionOneWithoutStrategicData()
        {
            Assert.That(new ProceduralMapGenerator().TryGenerate(new MapGenerationSettingsData(),out var map,out _),Is.True);
            Assert.That(map.GeneratorVersion,Is.EqualTo(1));Assert.That(map.Layout,Is.Null);
        }
        [Test] public void ImpossibleStrategicSizeIsRejectedBeforeAttempting()
        {
            var settings=new MapGenerationSettingsData {Width=32,Height=32,Preset=MapLayoutPreset.StrategicRegions};
            Assert.That(new ProceduralMapGenerator().TryGenerate(settings,out var map,out var error),Is.False);
            Assert.That(map,Is.Null);Assert.That(error,Does.Contain("Strategic regions"));
        }
        [Test] public void StrategicBudgetsAndLandmarkPadSurviveFinalPlacement()
        {
            var settings=new MapGenerationSettingsData {Preset=MapLayoutPreset.StrategicRegions};
            Assert.That(new ProceduralMapGenerator().TryGenerate(settings,out var map,out var error),Is.True,error);
            int trees=0,sheep=0,landmarkGold=0;
            foreach(var r in map.Resources)if(!r.IsStartingResource)
            {
                int region=map.Layout.RegionIds[map.Index(r.X,r.Y)];
                if(r.Kind==ProceduralResourceKind.Tree&&region==1)trees++;
                if(r.Kind==ProceduralResourceKind.Sheep&&region==1)sheep++;
                if(r.Kind==ProceduralResourceKind.Gold){Assert.That(region,Is.EqualTo(2));if(r.ClusterId==400)landmarkGold++;}
            }
            Assert.That(trees,Is.EqualTo(68));Assert.That(sheep,Is.EqualTo(9));Assert.That(landmarkGold,Is.EqualTo(3));
            Assert.That(map.Validation.ReachableGateways,Is.EqualTo(2));
            Assert.That(ProceduralMapGenerator.ValidateStrategicMap(settings,map).IsValid,Is.True);
        }
        [TestCase("tree-wall")] [TestCase("gold-water")] [TestCase("diagonal")] [TestCase("landmark-island")] [TestCase("extra-entrance")]
        public void FinalTopologyValidatorRejectsBrokenFixtures(string fixture)
        {
            var settings=new MapGenerationSettingsData {Preset=MapLayoutPreset.StrategicRegions,Seed=0};
            Assert.That(new ProceduralMapGenerator().TryGenerate(settings,out var map,out _),Is.True);
            var landmark=map.Layout.Landmark;
            if(fixture=="tree-wall")for(int y=0;y<map.Height;y++)map.Resources.Add(new ResourcePlacement {Kind=ProceduralResourceKind.Tree,X=map.StartX-10,Y=y});
            if(fixture=="gold-water")foreach(var r in map.Resources)if(r.Kind==ProceduralResourceKind.Gold){map.Terrain[map.Index(r.X+1,r.Y)]=0;break;}
            if(fixture=="diagonal")for(int y=0;y<map.Height;y++)for(int x=0;x<map.Width;x++)
                if(System.Math.Abs(x-map.StartX)>2||System.Math.Abs(y-map.StartY)>2)map.Terrain[map.Index(x,y)]=(byte)(x==y?1:0);
            if(fixture=="landmark-island")for(int offset=-3;offset<=3;offset++)
            {map.Terrain[map.Index(landmark.X+offset,landmark.Y-3)]=0;map.Terrain[map.Index(landmark.X+offset,landmark.Y+3)]=0;map.Terrain[map.Index(landmark.X-3,landmark.Y+offset)]=0;map.Terrain[map.Index(landmark.X+3,landmark.Y+offset)]=0;}
            if(fixture=="extra-entrance")for(int x=0;x<=map.StartX;x++)map.Terrain[map.Index(x,map.StartY+10)]=1;
            Assert.That(ProceduralMapGenerator.ValidateStrategicMap(settings,map).IsValid,Is.False,fixture);
        }
        [Test] public void StrategicLandmarkRequiresThreeOuterGoldNodes()
        {
            var settings=new MapGenerationSettingsData {Preset=MapLayoutPreset.StrategicRegions,TotalGoldCount=5,StartingGoldCount=3};
            Assert.That(new ProceduralMapGenerator().TryGenerate(settings,out _,out var error),Is.False);
            Assert.That(error,Does.Contain("three outer gold"));
        }
        [Test] public void StrategicFailureStillUsesFiniteAttemptBudget()
        {
            var settings=new MapGenerationSettingsData {Preset=MapLayoutPreset.StrategicRegions,MaxAttempts=1,TotalTreeCount=100000,ResourcePlacementAttempts=100};
            Assert.That(new ProceduralMapGenerator().TryGenerate(settings,out _,out var error),Is.False);
            Assert.That(error,Does.Contain("after 1 attempts"));
        }
        [Test]
        public void SameSeedProducesSameMapAndPlacements()
        {
            var settings = new MapGenerationSettingsData { Seed = 1543 };
            var generator = new ProceduralMapGenerator();
            Assert.That(generator.TryGenerate(settings, out MapData first, out string firstError), Is.True, firstError);
            Assert.That(generator.TryGenerate(settings, out MapData second, out string secondError), Is.True, secondError);
            Assert.That(second.Attempt, Is.EqualTo(first.Attempt));
            Assert.That(second.Terrain, Is.EqualTo(first.Terrain));
            Assert.That(second.Reserved, Is.EqualTo(first.Reserved));
            Assert.That(second.Resources.Count, Is.EqualTo(first.Resources.Count));
            for (int index = 0; index < first.Resources.Count; index++)
            {
                ResourcePlacement a = first.Resources[index];
                ResourcePlacement b = second.Resources[index];
                Assert.That((b.Kind, b.VariantId, b.X, b.Y, b.ClusterId, b.IsStartingResource),
                    Is.EqualTo((a.Kind, a.VariantId, a.X, a.Y, a.ClusterId, a.IsStartingResource)));
            }
        }

        [Test]
        public void DefaultPresetMeetsQuotasAndRequiredConnectivity()
        {
            var settings = new MapGenerationSettingsData();
            var generator = new ProceduralMapGenerator();
            for (int seed = 0; seed < 32; seed++)
            {
                settings.Seed = seed;
                Assert.That(generator.TryGenerate(settings, out MapData map, out string error), Is.True, $"seed={seed}: {error}");
                Assert.That(map.Validation.IsValid, Is.True, $"seed={seed}");
                Assert.That(map.Validation.TreeCount, Is.EqualTo(settings.TotalTreeCount));
                Assert.That(map.Validation.GoldCount, Is.EqualTo(settings.TotalGoldCount));
                Assert.That(map.Validation.SheepCount, Is.EqualTo(settings.TotalSheepCount));
                Assert.That(map.Validation.ReachableGrassCells, Is.GreaterThan(0));

                var goldVariants = new int[6];
                foreach (ResourcePlacement resource in map.Resources)
                    if (resource.Kind == ProceduralResourceKind.Gold) goldVariants[resource.VariantId]++;
                foreach (int variantCount in goldVariants)
                    Assert.That(variantCount, Is.InRange(settings.TotalGoldCount / 6,
                        (settings.TotalGoldCount + 5) / 6));
            }
        }

        [Test]
        public void InvalidQuotaIsRejectedBeforeGeneration()
        {
            var settings = new MapGenerationSettingsData { StartingGoldCount = 4, TotalGoldCount = 3 };
            var generator = new ProceduralMapGenerator();
            Assert.That(generator.TryGenerate(settings, out _, out string error), Is.False);
            Assert.That(error, Does.Contain("Gold quotas"));
        }
    }
}
