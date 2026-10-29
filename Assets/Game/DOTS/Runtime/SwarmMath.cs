using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace RTS.DOTS
{
    [BurstCompile]
    public static class SwarmMath
    {
        public const float SpatialCellSize = 1.5f;
        public static int2 SpatialCell(float2 position) => (int2)math.floor(position / SpatialCellSize);
        public static int GridIndex(int2 cell, int2 size) => cell.y * size.x + cell.x;
        public static bool InBounds(int2 cell, int2 size) => math.all(cell >= 0) && math.all(cell < size);
        public static float2 FlowDirection(float2 position, float2 direction, float2 origin, float cellSize)
        {
            if (math.lengthsq(direction) < 0.001f) return 0;
            int2 cell = (int2)math.floor((position - origin) / cellSize);
            // Aim at the next cell's center so an agent clears a wall before turning around its corner.
            float2 center = origin + ((float2)cell + math.round(direction) + 0.5f) * cellSize;
            return math.normalizesafe(center - position);
        }

        public static bool Walkable(float2 position, float2 origin, float cellSize, int2 size, NativeArray<byte> walkable)
        {
            int2 cell = (int2)math.floor((position - origin) / cellSize);
            return InBounds(cell, size) && walkable[GridIndex(cell, size)] != 0;
        }

        public static bool Fits(float2 position, float radius, float2 origin, float cellSize, int2 size, NativeArray<byte> walkable)
        {
            float r = radius * 0.7f;
            return Walkable(position, origin, cellSize, size, walkable)
                && Walkable(position + new float2(r, r), origin, cellSize, size, walkable)
                && Walkable(position + new float2(-r, r), origin, cellSize, size, walkable)
                && Walkable(position + new float2(r, -r), origin, cellSize, size, walkable)
                && Walkable(position - new float2(r, r), origin, cellSize, size, walkable);
        }

        public static float2 MoveOnGrid(float2 from, float2 delta, float radius, float2 origin,
            float cellSize, int2 size, NativeArray<byte> walkable)
        {
            int steps = math.max(1, (int)math.ceil(math.length(delta) / math.max(0.05f, cellSize * 0.25f)));
            delta /= steps;
            for (int i = 0; i < steps; i++)
            {
                if (Fits(from + delta, radius, origin, cellSize, size, walkable)) from += delta;
                else
                {
                    float2 x = new float2(delta.x, 0), y = new float2(0, delta.y);
                    if (Fits(from + x, radius, origin, cellSize, size, walkable)) from += x;
                    if (Fits(from + y, radius, origin, cellSize, size, walkable)) from += y;
                }
            }
            return from;
        }
    }
}
