using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace RTS.ProceduralGeneration
{
    public sealed class ProceduralMapGenerator
    {
        const uint TerrainPhase = 0x54455252u;
        const uint ResourcePhase = 0x5245534Fu;
        static readonly int[] DirectionX = { 1, -1, 0, 0 };
        static readonly int[] DirectionY = { 0, 0, 1, -1 };

        public bool TryGenerate(MapGenerationSettingsData settings, out MapData map, out string error)
        {
            map = null;
            error = MapGenerationSettingsValidator.Validate(settings);
            if (error != null) return false;

            MapValidationReport lastReport = null;
            for (int attempt = 0; attempt < settings.MaxAttempts; attempt++)
            {
                MapData candidate = GenerateAttempt(settings, attempt);
                lastReport = candidate.Validation;
                if (lastReport.IsValid)
                {
                    map = candidate;
                    return true;
                }
            }

            error = lastReport == null
                ? "Map generation did not run."
                : $"Seed {settings.Seed} failed after {settings.MaxAttempts} attempts: {string.Join(" | ", lastReport.Errors)}";
            return false;
        }

        MapData GenerateAttempt(MapGenerationSettingsData settings, int attempt)
        {
            int width = settings.Width;
            int height = settings.Height;
            int startX = width / 2;
            int startY = height / 2;
            int terrainSeed = DeterministicRandom.DeriveSeed(settings.Seed, TerrainPhase, attempt);
            var terrainRandom = new DeterministicRandom(terrainSeed, TerrainPhase);
            var terrain = new byte[width * height];
            var reserved = new byte[terrain.Length];
            var report = new MapValidationReport { Seed = settings.Seed, Attempt = attempt };

            float2[] octaveOffsets = new float2[settings.Octaves];
            for (int octave = 0; octave < octaveOffsets.Length; octave++)
            {
                octaveOffsets[octave] = new float2(terrainRandom.NextFloat() * 8192f, terrainRandom.NextFloat() * 8192f);
            }

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float total = 0f;
                float amplitudeSum = 0f;
                float2 normalized = new float2(x / settings.NoiseScale, y / settings.NoiseScale);
                for (int octave = 0; octave < settings.Octaves; octave++)
                {
                    total += noise.snoise(normalized * frequency + octaveOffsets[octave]) * amplitude;
                    amplitudeSum += amplitude;
                    amplitude *= settings.Persistence;
                    frequency *= settings.Lacunarity;
                }

                bool border = x < settings.BorderLandWidth || y < settings.BorderLandWidth
                    || x >= width - settings.BorderLandWidth || y >= height - settings.BorderLandWidth;
                terrain[y * width + x] = (byte)((border || total / amplitudeSum >= settings.WaterThreshold)
                    ? TerrainCell.Grass : TerrainCell.Water);
            }

            ReserveStartAndCorridor(settings, terrain, reserved, startX, startY, report);
            RemoveUnreachableLand(terrain, width, height, startX, startY, report);

            int[] distances = BuildDistances(terrain, width, height, startX, startY, null);
            var occupied = new byte[terrain.Length];
            var resources = new List<ResourcePlacement>(settings.TotalTreeCount + settings.TotalGoldCount + settings.TotalSheepCount);
            int resourceSeed = DeterministicRandom.DeriveSeed(settings.Seed, ResourcePhase, attempt);
            var resourceRandom = new DeterministicRandom(resourceSeed, ResourcePhase);

            PlaceQuota(settings, ProceduralResourceKind.Tree, settings.StartingTreeCount, 0,
                settings.StartingTreeMinDistance, settings.StartingTreeMaxDistance, true,
                terrain, reserved, occupied, distances, resources, ref resourceRandom);
            PlaceQuota(settings, ProceduralResourceKind.Gold, settings.StartingGoldCount, 1,
                settings.StartingGoldMinDistance, settings.StartingGoldMaxDistance, true,
                terrain, reserved, occupied, distances, resources, ref resourceRandom);
            PlaceQuota(settings, ProceduralResourceKind.Sheep, settings.StartingSheepCount, 0,
                settings.StartingSheepMinDistance, settings.StartingSheepMaxDistance, true,
                terrain, reserved, occupied, distances, resources, ref resourceRandom);

            PlaceQuota(settings, ProceduralResourceKind.Tree, settings.TotalTreeCount - settings.StartingTreeCount, 0,
                settings.StartingTreeMaxDistance + 1, int.MaxValue, false,
                terrain, reserved, occupied, distances, resources, ref resourceRandom);
            PlaceQuota(settings, ProceduralResourceKind.Gold, settings.TotalGoldCount - settings.StartingGoldCount, 1,
                settings.StartingGoldMaxDistance + 1, int.MaxValue, false,
                terrain, reserved, occupied, distances, resources, ref resourceRandom);
            PlaceQuota(settings, ProceduralResourceKind.Sheep, settings.TotalSheepCount - settings.StartingSheepCount, 0,
                settings.StartingSheepMaxDistance + 1, int.MaxValue, false,
                terrain, reserved, occupied, distances, resources, ref resourceRandom);

            Validate(settings, terrain, reserved, resources, width, height, startX, startY, report);
            CountTerrain(terrain, report);
            return new MapData(width, height, settings.Seed, attempt, startX, startY, terrain, reserved, resources, report);
        }

        static void ReserveStartAndCorridor(MapGenerationSettingsData settings, byte[] terrain, byte[] reserved,
            int startX, int startY, MapValidationReport report)
        {
            int width = settings.Width;
            int height = settings.Height;
            int radius = settings.StartClearRadius;
            for (int y = startY - radius; y <= startY + radius; y++)
            for (int x = startX - radius; x <= startX + radius; x++)
            {
                if (!InBounds(x, y, width, height)) continue;
                int index = y * width + x;
                if (terrain[index] == (byte)TerrainCell.Water) report.Repairs++;
                terrain[index] = (byte)TerrainCell.Grass;
                reserved[index] = 1;
            }

            int corridor = math.max(1, settings.RequiredCorridorHalfWidth);
            for (int x = startX + radius; x < width; x++)
            for (int y = startY - corridor; y <= startY + corridor; y++)
            {
                if (!InBounds(x, y, width, height)) continue;
                int index = y * width + x;
                if (terrain[index] == (byte)TerrainCell.Water) report.Repairs++;
                terrain[index] = (byte)TerrainCell.Grass;
                reserved[index] = 1;
            }
        }

        static void RemoveUnreachableLand(byte[] terrain, int width, int height, int startX, int startY, MapValidationReport report)
        {
            int[] distances = BuildDistances(terrain, width, height, startX, startY, null);
            for (int i = 0; i < terrain.Length; i++)
            {
                if (terrain[i] == (byte)TerrainCell.Grass && distances[i] < 0)
                {
                    terrain[i] = (byte)TerrainCell.Water;
                    report.Repairs++;
                }
            }
        }

        static void PlaceQuota(MapGenerationSettingsData settings, ProceduralResourceKind kind, int requested, int footprintRadius,
            int minDistance, int maxDistance, bool starting, byte[] terrain, byte[] reserved, byte[] occupied,
            int[] distances, List<ResourcePlacement> placements, ref DeterministicRandom random)
        {
            if (requested <= 0) return;
            int width = settings.Width;
            int height = settings.Height;
            int placed = 0;
            int attempts = 0;
            int clusterId = kind == ProceduralResourceKind.Tree ? 100 : kind == ProceduralResourceKind.Gold ? 200 : 300;
            int clusterX = random.NextInt(0, width);
            int clusterY = random.NextInt(0, height);

            while (placed < requested && attempts++ < settings.ResourcePlacementAttempts)
            {
                if (!starting && attempts % 24 == 1)
                {
                    clusterX = random.NextInt(0, width);
                    clusterY = random.NextInt(0, height);
                    clusterId++;
                }

                int x;
                int y;
                if (!starting && kind != ProceduralResourceKind.Sheep)
                {
                    int spread = kind == ProceduralResourceKind.Tree ? 7 : 4;
                    x = clusterX + random.NextInt(-spread, spread + 1);
                    y = clusterY + random.NextInt(-spread, spread + 1);
                }
                else
                {
                    x = random.NextInt(0, width);
                    y = random.NextInt(0, height);
                }

                if (!InBounds(x, y, width, height)) continue;
                int distance = distances[y * width + x];
                if (distance < minDistance || distance > maxDistance) continue;
                if (!CanPlace(x, y, footprintRadius, terrain, reserved, occupied, width, height)) continue;
                if (kind == ProceduralResourceKind.Gold && HasNearby(placements, kind, x, y, 3)) continue;
                if (kind == ProceduralResourceKind.Sheep && HasNearby(placements, kind, x, y, 1)) continue;

                int variant = kind == ProceduralResourceKind.Gold ? CountPlacements(placements, kind) % 6 : 0;
                placements.Add(new ResourcePlacement
                {
                    Kind = kind,
                    VariantId = variant,
                    X = x,
                    Y = y,
                    ClusterId = clusterId,
                    IsStartingResource = starting
                });
                MarkOccupied(x, y, footprintRadius, occupied, width, height);
                placed++;
            }
        }

        static bool CanPlace(int x, int y, int radius, byte[] terrain, byte[] reserved, byte[] occupied, int width, int height)
        {
            for (int cy = y - radius; cy <= y + radius; cy++)
            for (int cx = x - radius; cx <= x + radius; cx++)
            {
                if (!InBounds(cx, cy, width, height)) return false;
                int index = cy * width + cx;
                if (terrain[index] != (byte)TerrainCell.Grass || reserved[index] != 0 || occupied[index] != 0) return false;
            }
            return true;
        }

        static void MarkOccupied(int x, int y, int radius, byte[] occupied, int width, int height)
        {
            for (int cy = y - radius; cy <= y + radius; cy++)
            for (int cx = x - radius; cx <= x + radius; cx++)
                if (InBounds(cx, cy, width, height)) occupied[cy * width + cx] = 1;
        }

        static bool HasNearby(List<ResourcePlacement> placements, ProceduralResourceKind kind, int x, int y, int radius)
        {
            int radiusSquared = radius * radius;
            for (int i = 0; i < placements.Count; i++)
            {
                ResourcePlacement placement = placements[i];
                if (placement.Kind != kind) continue;
                int dx = placement.X - x;
                int dy = placement.Y - y;
                if (dx * dx + dy * dy <= radiusSquared) return true;
            }
            return false;
        }

        static int CountPlacements(List<ResourcePlacement> placements, ProceduralResourceKind kind)
        {
            int count = 0;
            for (int i = 0; i < placements.Count; i++)
                if (placements[i].Kind == kind) count++;
            return count;
        }

        static void Validate(MapGenerationSettingsData settings, byte[] terrain, byte[] reserved,
            List<ResourcePlacement> resources, int width, int height, int startX, int startY, MapValidationReport report)
        {
            var blocked = new byte[terrain.Length];
            for (int i = 0; i < resources.Count; i++)
            {
                ResourcePlacement resource = resources[i];
                if (resource.Kind == ProceduralResourceKind.Sheep) continue;
                int radius = resource.Kind == ProceduralResourceKind.Gold ? 1 : 0;
                MarkOccupied(resource.X, resource.Y, radius, blocked, width, height);
            }

            int[] distances = BuildDistances(terrain, width, height, startX, startY, blocked);
            for (int i = 0; i < distances.Length; i++)
                if (distances[i] >= 0) report.ReachableGrassCells++;

            for (int i = 0; i < resources.Count; i++)
            {
                ResourcePlacement resource = resources[i];
                if (resource.Kind == ProceduralResourceKind.Tree) report.TreeCount++;
                else if (resource.Kind == ProceduralResourceKind.Gold) report.GoldCount++;
                else report.SheepCount++;

                if (!HasReachableInteractionCell(resource, distances, terrain, width, height))
                    report.AddError($"{resource.Kind} at ({resource.X},{resource.Y}) has no reachable interaction cell.");
            }

            if (report.TreeCount != settings.TotalTreeCount) report.AddError($"Tree quota {report.TreeCount}/{settings.TotalTreeCount}.");
            if (report.GoldCount != settings.TotalGoldCount) report.AddError($"Gold quota {report.GoldCount}/{settings.TotalGoldCount}.");
            if (report.SheepCount != settings.TotalSheepCount) report.AddError($"Sheep quota {report.SheepCount}/{settings.TotalSheepCount}.");

            for (int i = 0; i < reserved.Length; i++)
                if (reserved[i] != 0 && distances[i] < 0 && blocked[i] == 0)
                {
                    report.AddError("A required start/corridor cell became unreachable.");
                    break;
                }
        }

        static bool HasReachableInteractionCell(ResourcePlacement resource, int[] distances, byte[] terrain, int width, int height)
        {
            if (resource.Kind == ProceduralResourceKind.Sheep)
                return distances[resource.Y * width + resource.X] >= 0;

            int radius = resource.Kind == ProceduralResourceKind.Gold ? 2 : 1;
            for (int y = resource.Y - radius; y <= resource.Y + radius; y++)
            for (int x = resource.X - radius; x <= resource.X + radius; x++)
            {
                if (!InBounds(x, y, width, height)) continue;
                if (math.max(math.abs(x - resource.X), math.abs(y - resource.Y)) != radius) continue;
                int index = y * width + x;
                if (terrain[index] == (byte)TerrainCell.Grass && distances[index] >= 0) return true;
            }
            return false;
        }

        static int[] BuildDistances(byte[] terrain, int width, int height, int startX, int startY, byte[] blocked)
        {
            var distances = new int[terrain.Length];
            Array.Fill(distances, -1);
            int start = startY * width + startX;
            if (terrain[start] != (byte)TerrainCell.Grass || (blocked != null && blocked[start] != 0)) return distances;

            var queue = new int[terrain.Length];
            int head = 0;
            int tail = 0;
            queue[tail++] = start;
            distances[start] = 0;
            while (head < tail)
            {
                int current = queue[head++];
                int x = current % width;
                int y = current / width;
                for (int direction = 0; direction < 4; direction++)
                {
                    int nextX = x + DirectionX[direction];
                    int nextY = y + DirectionY[direction];
                    if (!InBounds(nextX, nextY, width, height)) continue;
                    int next = nextY * width + nextX;
                    if (distances[next] >= 0 || terrain[next] != (byte)TerrainCell.Grass
                        || (blocked != null && blocked[next] != 0)) continue;
                    distances[next] = distances[current] + 1;
                    queue[tail++] = next;
                }
            }
            return distances;
        }

        static void CountTerrain(byte[] terrain, MapValidationReport report)
        {
            for (int i = 0; i < terrain.Length; i++)
            {
                if (terrain[i] == (byte)TerrainCell.Grass) report.GrassCells++;
                else report.WaterCells++;
            }
        }

        static bool InBounds(int x, int y, int width, int height)
            => x >= 0 && y >= 0 && x < width && y < height;
    }
}
