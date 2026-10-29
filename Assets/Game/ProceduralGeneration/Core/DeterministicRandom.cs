namespace RTS.ProceduralGeneration
{
    public struct DeterministicRandom
    {
        uint m_State;

        public DeterministicRandom(int seed, uint phase = 0)
        {
            m_State = Mix(unchecked((uint)seed) ^ (phase * 0x9E3779B9u));
            if (m_State == 0) m_State = 0x6D2B79F5u;
        }

        public uint NextUInt()
        {
            uint value = m_State;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            m_State = value;
            return value;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));
        }

        public float NextFloat()
        {
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        public static int DeriveSeed(int seed, uint phase, int attempt)
        {
            uint value = unchecked((uint)seed) ^ (phase * 0x9E3779B9u) ^ unchecked((uint)attempt * 0x85EBCA6Bu);
            return unchecked((int)Mix(value));
        }

        static uint Mix(uint value)
        {
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value;
        }
    }
}
