using System.Diagnostics;
using RTS.ProceduralGeneration;

int count = args.Length > 0 && int.TryParse(args[0], out int parsed) ? Math.Max(1, parsed) : 1000;
var generator = new ProceduralMapGenerator();
var settings = new MapGenerationSettingsData();
if (args.Length > 1)
{
    if (args[1] != "strategic" && args[1] != "legacy" && args[1] != "four") throw new ArgumentException("Preset must be strategic, legacy or four.");
    settings.Preset = args[1] == "four" ? MapLayoutPreset.FourFrontiers : args[1] == "strategic" ? MapLayoutPreset.StrategicRegions : MapLayoutPreset.LegacyMainland;
}
if (args.Length > 2 && args[2] == "baseline")
{
    foreach (int seed in new[] { 0, 1, 1543 })
    {
        settings.Seed = seed;
        if (!generator.TryGenerate(settings, out var sample, out var error)) throw new Exception(error);
        Console.WriteLine($"{settings.Preset} seed={seed} attempt={sample.Attempt} sha256={Hash(sample)}");
    }
    return 0;
}
var failures = new List<string>();
var measurements = new List<double>();
var regionalMetrics = new List<object>();
var rows = new List<string> { "seed,success,attempt,milliseconds,sha256" };
long totalMilliseconds = 0;
long worstMilliseconds = 0;
int worstSeed = 0;
var attemptHistogram = new int[settings.MaxAttempts];

for (int seed = 0; seed < count; seed++)
{
    settings.Seed = seed;
    var stopwatch = Stopwatch.StartNew();
    bool success = generator.TryGenerate(settings, out MapData map, out string error);
    stopwatch.Stop();
    measurements.Add(stopwatch.Elapsed.TotalMilliseconds);
    rows.Add($"{seed},{success},{(success ? map.Attempt : -1)},{stopwatch.Elapsed.TotalMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture)},{(success ? Hash(map) : error)}");
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
    attemptHistogram[map.Attempt]++;
    if(map.Layout!=null)regionalMetrics.Add(RegionalMetrics(map));

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

if(args.Length>2&&args[2]=="record")File.WriteAllLines("Docs/Modules/ProceduralGeneration/Evidence/FourFrontiers/dotnet-"+settings.Preset+".csv",rows);
if(args.Length>2&&args[2]=="record")File.WriteAllText("Docs/Modules/ProceduralGeneration/Evidence/FourFrontiers/region-metrics-"+settings.Preset+".json",System.Text.Json.JsonSerializer.Serialize(regionalMetrics,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
measurements.Sort();
Console.WriteLine($"Median/p95 Core: {measurements[(count-1)/2]:F3}/{measurements[(int)Math.Ceiling(count*.95)-1]:F3} ms");
Console.WriteLine($"Seeds checked: {count}");
Console.WriteLine($"Preset: {settings.Preset}");
Console.WriteLine($"Failures: {failures.Count}");
Console.WriteLine($"Accepted attempt histogram (0-based): {string.Join(",", attemptHistogram)}");
Console.WriteLine($"Mean generation time: {(double)totalMilliseconds / count:F2} ms");
Console.WriteLine($"Worst generation time: {worstMilliseconds} ms (seed {worstSeed})");
foreach (string failure in failures) Console.WriteLine(failure);
return failures.Count == 0 ? 0 : 1;

static bool Equivalent(MapData left, MapData right)
{
    if (left.Width != right.Width || left.Height != right.Height || left.Attempt != right.Attempt) return false;
    if (!left.Terrain.AsSpan().SequenceEqual(right.Terrain)) return false;
    if (!left.Reserved.AsSpan().SequenceEqual(right.Reserved)) return false;
    if (left.GeneratorVersion != right.GeneratorVersion || (left.Layout == null) != (right.Layout == null)) return false;
    if (left.Layout != null && (!left.Layout.RegionIds.AsSpan().SequenceEqual(right.Layout.RegionIds)
        || !left.Layout.Routes.AsSpan().SequenceEqual(right.Layout.Routes)
        || !left.Layout.Regions.SequenceEqual(right.Layout.Regions) || !left.Layout.Connections.SequenceEqual(right.Layout.Connections)
        || !left.Layout.Gateways.SequenceEqual(right.Layout.Gateways) || !left.Layout.Landmark.Equals(right.Layout.Landmark))) return false;
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
    if (settings.Preset != MapLayoutPreset.LegacyMainland)
    {
        if (map.Layout == null || map.Layout.Regions.Length != (settings.Preset == MapLayoutPreset.FourFrontiers ? 5 : 3) || map.Layout.Gateways.Length != 2) return "strategic layout missing";
        foreach (var gateway in map.Layout.Gateways)
            if (!map.IsGrass(gateway.X, gateway.Y) || map.Reserved[map.Index(gateway.X, gateway.Y)] == 0) return "gateway not protected";
        if (!ProceduralMapGenerator.ValidateSnapshot(settings,map).IsValid) return "final strategic validator failed";
    }

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

static string Hash(MapData map)
{
    using var stream = new MemoryStream(); using var w = new BinaryWriter(stream);
    w.Write(map.Width); w.Write(map.Height); w.Write(map.Seed); w.Write(map.Attempt); w.Write(map.GeneratorVersion);
    w.Write(map.Terrain); w.Write(map.Reserved);
    if(map.Layout != null) {
        w.Write(map.Layout.RegionIds); w.Write(map.Layout.Routes);
        foreach(var r in map.Layout.Regions) {w.Write(r.X);w.Write(r.Y);w.Write(r.Radius);w.Write((byte)r.Role);}
        foreach(var c in map.Layout.Connections) {w.Write(c.From);w.Write(c.To);}
        foreach(var g in map.Layout.Gateways) {w.Write(g.X);w.Write(g.Y);w.Write(g.Region);}
        var l=map.Layout.Landmark;w.Write(l.X);w.Write(l.Y);w.Write(l.Radius);w.Write(l.Region);
    }
    foreach(var r in map.Resources) {w.Write((byte)r.Kind);w.Write(r.X);w.Write(r.Y);w.Write(r.VariantId);w.Write(r.ClusterId);w.Write(r.IsStartingResource);}
    w.Flush();return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream.ToArray()));
}

static object RegionalMetrics(MapData map)
{
    var area=new int[map.Layout.Regions.Length];var grass=new int[area.Length];var outer=new int[area.Length];
    for(int i=0;i<map.Terrain.Length;i++){int id=map.Layout.RegionIds[i];area[id]++;if(map.Terrain[i]!=0)grass[id]++;}
    var blocked=new bool[map.Terrain.Length];
    foreach(var resource in map.Resources)
    {
        if(!resource.IsStartingResource)outer[map.Layout.RegionIds[map.Index(resource.X,resource.Y)]]++;
        if(resource.Kind==ProceduralResourceKind.Sheep)continue;
        int radius=resource.Kind==ProceduralResourceKind.Gold?1:0;
        for(int y=resource.Y-radius;y<=resource.Y+radius;y++)for(int x=resource.X-radius;x<=resource.X+radius;x++)
            if(map.InBounds(x,y))blocked[map.Index(x,y)]=true;
    }
    var distance=Enumerable.Repeat(-1,map.Terrain.Length).ToArray();var queue=new Queue<int>();int start=map.Index(map.StartX,map.StartY);
    distance[start]=0;queue.Enqueue(start);
    while(queue.Count>0)
    {
        int index=queue.Dequeue(),x=index%map.Width,y=index/map.Width;
        foreach(var (dx,dy) in new[]{(1,0),(-1,0),(0,1),(0,-1)})
        {
            int nx=x+dx,ny=y+dy;if(!map.InBounds(nx,ny))continue;int next=map.Index(nx,ny);
            if(distance[next]>=0||blocked[next]||map.Terrain[next]==0)continue;
            distance[next]=distance[index]+1;queue.Enqueue(next);
        }
    }
    return new {seed=map.Seed,attempt=map.Attempt,regions=Enumerable.Range(0,area.Length).Select(id=>new{
        id,role=map.Layout.Regions[id].Role.ToString(),labeledCells=area[id],grassCells=grass[id],outerResources=outer[id],
        resourcesPer100Grass=grass[id]>0?100.0*outer[id]/grass[id]:0,
        centerBfs=distance[map.Index(map.Layout.Regions[id].X,map.Layout.Regions[id].Y)]}).ToArray()};
}
