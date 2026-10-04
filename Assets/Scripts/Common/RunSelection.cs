using UnityEngine;

namespace Jinhyeong_Common
{
    public static class RunSelection
    {
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

        public static readonly Hero[] Heroes =
        {
            new Hero(1, "시바", "Lobby/portrait_shiba"),
            new Hero(2, "코기", "Lobby/portrait_corgi"),
            new Hero(3, "그레이하운드", "Lobby/portrait_greyhound"),
            new Hero(4, "포메라니안", "Lobby/portrait_pomeranian"),
            new Hero(5, "사모예드", "Lobby/portrait_samoyed")
        };

        private static int _characterIndex = 1;

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
