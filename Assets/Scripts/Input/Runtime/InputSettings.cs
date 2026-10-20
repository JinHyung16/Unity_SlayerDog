using UnityEngine;

namespace Jinhyeong_Input
{

    public static class InputSettings
    {
        public enum EMoveScheme
        {
            Wasd = 0,
            Arrow = 1
        }

        public struct Scheme
        {
            public EMoveScheme Id;
            public KeyCode Up;
            public KeyCode Down;
            public KeyCode Left;
            public KeyCode Right;

            public string Label
            {
                get { return LabelOf(Id); }
            }
        }

        public static string LabelOf(EMoveScheme scheme)
        {
            switch (scheme)
            {
                case EMoveScheme.Arrow: return Jinhyeong_Common.Loc.T("Input_Arrow", "방향키");
                default: return "WASD";
            }
        }

        public static readonly Scheme[] Schemes =
        {
            new Scheme
            {
                Id = EMoveScheme.Wasd,
                Up = KeyCode.W, Down = KeyCode.S, Left = KeyCode.A, Right = KeyCode.D
            },
            new Scheme
            {
                Id = EMoveScheme.Arrow,
                Up = KeyCode.UpArrow, Down = KeyCode.DownArrow,
                Left = KeyCode.LeftArrow, Right = KeyCode.RightArrow
            }
        };

        private const string KeyScheme = "input.movescheme";

        public static EMoveScheme MoveScheme { get; private set; } = EMoveScheme.Wasd;

        public static Scheme Current
        {
            get { return Schemes[Mathf.Clamp((int)MoveScheme, 0, Schemes.Length - 1)]; }
        }

        public static void Load()
        {
            int raw = PlayerPrefs.GetInt(KeyScheme, (int)EMoveScheme.Wasd);
            MoveScheme = (EMoveScheme)Mathf.Clamp(raw, 0, Schemes.Length - 1);
        }

        public static void Set(EMoveScheme scheme)
        {
            MoveScheme = scheme;
            PlayerPrefs.SetInt(KeyScheme, (int)scheme);
            PlayerPrefs.Save();
        }
    }
}
