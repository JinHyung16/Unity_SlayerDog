using UnityEngine;

namespace Jinhyeong_Common
{
    /// <summary>
    /// 런을 시작하기 전에 로비에서 고른 것들.
    ///
    /// [사고] 캐릭터 번호를 `PartLoadout` 의 SerializeField 로만 뒀더니 **아무도 바꾸지 않아**
    /// 항상 시바(1)로 고정이었다. 코기·그레이하운드의 고유 스킬이 도달 불가능했고,
    /// 고유 스킬이 곧 선호 계열이라 빌드 방향도 하나뿐이었다.
    /// 로비와 스폰이 서로를 모르므로, 그 사이를 잇는 자리가 하나 필요하다.
    ///
    /// 씬을 넘어 유지돼야 해서 정적이다. 저장은 아직 없다(E-1) — 껐다 켜면 기본값으로 돌아간다.
    /// </summary>
    public static class RunSelection
    {
        /// <summary>슈퍼독 정의. 캐릭터가 늘면 데이터로 옮긴다 — 3종뿐이라 아직 코드에 둔다.</summary>
        public struct Hero
        {
            public int Index;
            public string Name;
            public string PortraitPath;

            public Hero(int index, string name, string portraitPath)
            {
                Index = index;
                Name = name;
                PortraitPath = portraitPath;
            }
        }

        /// <summary>고유 파츠의 소유 번호(Part.owner)와 1:1 로 맞춘다.</summary>
        public static readonly Hero[] Heroes =
        {
            new Hero(1, "시바", "Lobby/portrait_shiba"),
            new Hero(2, "코기", "Lobby/portrait_corgi"),
            new Hero(3, "그레이하운드", "Lobby/portrait_greyhound")
        };

        private static int _characterIndex = 1;

        /// <summary>이번 런에서 쓸 슈퍼독. 로비가 정하고 스폰이 읽는다.</summary>
        public static int CharacterIndex
        {
            get { return _characterIndex; }
            set { _characterIndex = Mathf.Clamp(value, 1, Heroes.Length); }
        }

        public static Hero CurrentHero
        {
            get { return Heroes[Mathf.Clamp(_characterIndex - 1, 0, Heroes.Length - 1)]; }
        }

        public static Hero HeroOf(int index)
        {
            return Heroes[Mathf.Clamp(index - 1, 0, Heroes.Length - 1)];
        }
    }
}
