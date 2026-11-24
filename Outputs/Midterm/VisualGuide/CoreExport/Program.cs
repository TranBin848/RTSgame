using System.Text.Json;
using RTS.ProceduralGeneration;
using Unity.Mathematics;

var directory = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
Directory.CreateDirectory(directory);
foreach (int seed in new[] { 0, 1, 1543 })
{
    var s = new MapGenerationSettingsData { Seed = seed, Preset = MapLayoutPreset.StrategicRegions };
    if (!new ProceduralMapGenerator().TryGenerate(s, out var map, out var error)) throw new Exception(error);
    var rng = new DeterministicRandom(DeterministicRandom.DeriveSeed(seed, 0x54455252u, map.Attempt), 0x54455252u);
    var offsets = new float2[s.Octaves];
    for (int k = 0; k < offsets.Length; k++) offsets[k] = new float2(rng.NextFloat() * 8192f, rng.NextFloat() * 8192f);
    var layers = new float[s.Octaves][];
    for (int k = 0; k < layers.Length; k++) layers[k] = new float[s.Width * s.Height];
    var values = new float[s.Width * s.Height];
    var raw = new byte[values.Length];
    for (int y = 0; y < s.Height; y++) for (int x = 0; x < s.Width; x++)
    {
        float amplitude = 1, frequency = 1, total = 0, sum = 0;
        for (int k = 0; k < s.Octaves; k++)
        {
            float value = noise.snoise(new float2(x / s.NoiseScale, y / s.NoiseScale) * frequency + offsets[k]);
            layers[k][y * s.Width + x] = value;
            total += value * amplitude; sum += amplitude; amplitude *= s.Persistence; frequency *= s.Lacunarity;
        }
        values[y * s.Width + x] = total / sum;
        raw[y * s.Width + x] = total / sum >= s.WaterThreshold ? (byte)1 : (byte)0;
    }
    // Rebuild only the pre-BFS stage with the actual Core layout function.
    var carved = (byte[])raw.Clone();
    var reserved = new byte[raw.Length];
    StrategicLayout.Create(s, map.Attempt, carved, reserved, new MapValidationReport());
    var payload = new { seed, attempt = map.Attempt, width = s.Width, height = s.Height, version = map.GeneratorVersion,
        settings = s, layers, values, raw = raw.Select(x => (int)x), carved = carved.Select(x => (int)x),
        terrain = map.Terrain.Select(x => (int)x), reserved = map.Reserved.Select(x => (int)x),
        regions = map.Layout.Regions, regionIds = map.Layout.RegionIds.Select(x => (int)x),
        routes = map.Layout.Routes.Select(x => (int)x), gateways = map.Layout.Gateways,
        landmark = map.Layout.Landmark, resources = map.Resources, validation = map.Validation };
    File.WriteAllText(Path.Combine(directory, $"seed-{seed}.json"), JsonSerializer.Serialize(payload, new JsonSerializerOptions { IncludeFields = true }));
    Console.WriteLine($"seed={seed} attempt={map.Attempt} resources={map.Resources.Count} valid={map.Validation.IsValid}");
}
