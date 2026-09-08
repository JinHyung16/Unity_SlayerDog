namespace Jinhyeong_Constellation
{

    /// <summary>
    /// mulberry32 — 32비트 상태 하나로 도는 결정적 난수.
    ///
    /// <see cref="UnityEngine.Random"/> 을 쓰지 않는 이유는 **전역 상태**라서다. 다른 시스템이 한 번만
    /// 뽑아도 성좌 모양이 통째로 바뀌고, 그러면 「어제 본 성좌」와 「오늘 본 성좌」가 달라진다.
    /// 성좌는 세이브가 노드 id 로 걸리므로 모양이 바뀌면 **산 노드가 엉뚱한 자리로 옮겨간다.**
    ///
    /// [주의] 소비 순서가 곧 레이아웃이다. 생성기 중간에 호출을 하나 끼우면 그 뒤가 전부 밀린다.
    /// 생성 순서를 고칠 때 제일 먼저 의심할 자리다.
    /// </summary>
    public sealed class MulberryRandom
    {
        private uint _state;

        public MulberryRandom(int seed)
        {
            _state = unchecked((uint)seed);
        }

        /// <summary>[0, 1) 실수.</summary>
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

        /// <summary>[0, count) 정수.</summary>
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
