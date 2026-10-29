using System.Diagnostics;
using RTS.ProceduralGeneration;

int count = args.Length > 0 && int.TryParse(args[0], out int parsed) ? Math.Max(1, parsed) : 1000;
var generator = new ProceduralMapGenerator();
var settings = new MapGenerationSettingsData();
var failures = new List<string>();
long totalMilliseconds = 0;
long worstMilliseconds = 0;
int worstSeed = 0;

for (int seed = 0; seed < count; seed++)
{
    settings.Seed = seed;
    var stopwatch = Stopwatch.StartNew();
    bool success = generator.TryGenerate(settings, out MapData map, out string error);
    stopwatch.Stop();
    totalMilliseconds += stopwatch.ElapsedMilliseconds;
    if (stopwatch.ElapsedMilliseconds > worstMilliseconds)
    {
        worstMilliseconds = stopwatch.ElapsedMilliseconds;
        worstSeed = seed;
    }

    if (!success)
    {
        failures.Add($"seed={seed}: {error}");
        continue;
    }

    string validationError = ValidateExpectedMap(map, settings);
    if (validationError != null)
    {
        failures.Add($"seed={seed}: {validationError}");
        continue;
    }

    if (!generator.TryGenerate(settings, out MapData repeated, out string repeatedError))
    {
        failures.Add($"seed={seed}: repeat failed: {repeatedError}");
        continue;
    }

    if (!Equivalent(map, repeated)) failures.Add($"seed={seed}: generation is not deterministic");
}

Console.WriteLine($"Seeds checked: {count}");
Console.WriteLine($"Failures: {failures.Count}");
Console.WriteLine($"Mean generation time: {(double)totalMilliseconds / count:F2} ms");
Console.WriteLine($"Worst generation time: {worstMilliseconds} ms (seed {worstSeed})");
foreach (string failure in failures.Take(20)) Console.WriteLine(failure);
return failures.Count == 0 ? 0 : 1;

static bool Equivalent(MapData left, MapData right)
{
    if (left.Width != right.Width || left.Height != right.Height || left.Attempt != right.Attempt) return false;
    if (!left.Terrain.AsSpan().SequenceEqual(right.Terrain)) return false;
    if (!left.Reserved.AsSpan().SequenceEqual(right.Reserved)) return false;
    if (left.Resources.Count != right.Resources.Count) return false;
    for (int index = 0; index < left.Resources.Count; index++)
    {
        ResourcePlacement a = left.Resources[index];
        ResourcePlacement b = right.Resources[index];
        if (a.Kind != b.Kind || a.VariantId != b.VariantId || a.X != b.X || a.Y != b.Y
            || a.ClusterId != b.ClusterId || a.IsStartingResource != b.IsStartingResource) return false;
    }
    return true;
}

static string ValidateExpectedMap(MapData map, MapGenerationSettingsData settings)
{
    if (!map.Validation.IsValid) return "validation report is invalid";
    if (map.Validation.TreeCount != settings.TotalTreeCount) return "tree quota mismatch";
    if (map.Validation.GoldCount != settings.TotalGoldCount) return "gold quota mismatch";
    if (map.Validation.SheepCount != settings.TotalSheepCount) return "sheep quota mismatch";
    if (map.Validation.ReachableGrassCells <= 0) return "no reachable grass";

    var goldVariants = new int[6];
    foreach (ResourcePlacement resource in map.Resources)
    {
        if (resource.Kind != ProceduralResourceKind.Gold) continue;
        if (resource.VariantId < 0 || resource.VariantId >= goldVariants.Length) return "gold variant is out of range";
        goldVariants[resource.VariantId]++;
    }

    int minimum = settings.TotalGoldCount / goldVariants.Length;
    int maximum = (settings.TotalGoldCount + goldVariants.Length - 1) / goldVariants.Length;
    if (goldVariants.Any(count => count < minimum || count > maximum)) return "gold variants are not balanced";
    return null;
}
