using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace RTS.DOTS
{
    [BurstCompile]
    public struct FlowFieldJob : IJob
    {
        [ReadOnly] public NativeArray<byte> Walkable;
        public NativeArray<int> Costs, Queue;
        public NativeArray<float2> Directions;
        public int2 Size, Goal;

        public void Execute()
        {
            for (int i = 0; i < Costs.Length; i++) { Costs[i] = int.MaxValue; Directions[i] = 0; }
            int head = 0, tail = 0;
            // A building can occupy the goal cell. Seed the first reachable perimeter, on every side.
            for (int r = 0; r <= math.max(Size.x, Size.y) && tail == 0; r++)
            {
                for (int y = -r; y <= r; y++)
                for (int x = -r; x <= r; x++)
                {
                    if (math.max(math.abs(x), math.abs(y)) != r) continue;
                    int2 c = Goal + new int2(x, y);
                    if (!SwarmMath.InBounds(c, Size)) continue;
                    int index = SwarmMath.GridIndex(c, Size);
                    if (Walkable[index] == 0) continue;
                    Costs[index] = 0;
                    Queue[tail++] = index;
                }
            }
            while (head < tail)
            {
                int current = Queue[head++];
                int2 c = new int2(current % Size.x, current / Size.x);
                Visit(c + new int2(1, 0), current, ref tail);
                Visit(c + new int2(-1, 0), current, ref tail);
                Visit(c + new int2(0, 1), current, ref tail);
                Visit(c + new int2(0, -1), current, ref tail);
            }
            for (int i = 0; i < Costs.Length; i++)
            {
                if (Costs[i] == int.MaxValue || Costs[i] == 0) continue;
                int2 c = new int2(i % Size.x, i / Size.x), best = c;
                int cost = Costs[i];
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    int2 n = c + new int2(x, y);
                    if (!SwarmMath.InBounds(n, Size)) continue;
                    // Do not cut the corner of an occupied tile.
                    if (x != 0 && y != 0 && (Walkable[SwarmMath.GridIndex(c + new int2(x, 0), Size)] == 0
                        || Walkable[SwarmMath.GridIndex(c + new int2(0, y), Size)] == 0)) continue;
                    int nc = Costs[SwarmMath.GridIndex(n, Size)];
                    if (nc < cost) { cost = nc; best = n; }
                }
                Directions[i] = math.normalizesafe((float2)(best - c));
            }
        }

        void Visit(int2 c, int parent, ref int tail)
        {
            if (!SwarmMath.InBounds(c, Size)) return;
            int i = SwarmMath.GridIndex(c, Size);
            if (Walkable[i] == 0 || Costs[i] != int.MaxValue) return;
            Costs[i] = Costs[parent] + 1;
            Queue[tail++] = i;
        }
    }
}
