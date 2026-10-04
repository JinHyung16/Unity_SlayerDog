namespace Jinhyeong_Constellation
{

    public sealed class MulberryRandom
    {
        private uint _state;

        public MulberryRandom(int seed)
        {
            _state = unchecked((uint)seed);
        }

        public float Next()
        {
            unchecked
            {
                _state += 0x6D2B79F5u;
                uint a = _state;
                uint t = (uint)((int)(a ^ (a >> 15)) * (int)(a | 1u));
                t = (t + (uint)((int)(t ^ (t >> 7)) * (int)(t | 61u))) ^ t;
                return (t ^ (t >> 14)) / 4294967296f;
            }
        }

        public bool Chance(float p)
        {
            return Next() < p;
        }

        public int Range(int count)
        {
            if (count <= 1)
            {
                return 0;
            }
            int v = (int)(Next() * count);
            return v >= count ? count - 1 : v;
        }
    }
}
