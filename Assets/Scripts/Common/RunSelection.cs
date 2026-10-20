using UnityEngine;

namespace Jinhyeong_Common
{
    public static class RunSelection
    {
        public struct Hero
        {
            public int Index;
            public string PortraitPath;
            private readonly string _korean;

            // 이름은 읽을 때 지금 언어로 — 표는 타입 초기화 때 만들어져 그때는 번역이 아직 없다 (키 Hero_<Index>)
            public string Name
            {
                get { return Loc.T("Hero_" + Index, _korean); }
            }

            public Hero(int index, string name, string portraitPath)
            {
                Index = index;
                _korean = name;
                PortraitPath = portraitPath;
            }
        }

        // 순서 = 기획서 캐릭터 표 = Part.owner = build_heroes.py HEROES = Art2DBuilder.HeroKeys
        public static readonly Hero[] Heroes =
        {
            new Hero(1, "시바 무사", "Lobby/portrait_shiba"),
            new Hero(2, "진돗개 무관", "Lobby/portrait_jindo"),
            new Hero(3, "아키타 닌자", "Lobby/portrait_akita"),
            new Hero(4, "차우차우 무승", "Lobby/portrait_chow"),
            new Hero(5, "퍼그 도사", "Lobby/portrait_pug"),
            new Hero(6, "셰퍼드 성기사", "Lobby/portrait_shepherd"),
            new Hero(7, "푸들 대마법사", "Lobby/portrait_poodle"),
            new Hero(8, "허스키 광전사", "Lobby/portrait_husky"),
            new Hero(9, "비글 레인저", "Lobby/portrait_beagle"),
            new Hero(10, "닥스훈트 포수", "Lobby/portrait_dachshund")
        };

        private static int _characterIndex = 1;
        private static int _mapIndex = 1;

        // 맵 순서 (MapRegistry). 범위 밖이면 MapRegistry.Get 이 첫 맵으로 돌려준다
        public static int MapIndex
        {
            get { return _mapIndex; }
            set { _mapIndex = Mathf.Max(1, value); }
        }

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
