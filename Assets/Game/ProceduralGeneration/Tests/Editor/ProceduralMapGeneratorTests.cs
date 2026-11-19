using NUnit.Framework;

namespace RTS.ProceduralGeneration.Tests
{
    public sealed class ProceduralMapGeneratorTests
    {
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
