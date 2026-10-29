using NUnit.Framework;

namespace RTS.ProceduralGeneration.Tests
{
    public sealed class ProceduralMapGeneratorTests
    {
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
