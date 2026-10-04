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
            public string Label;
            public KeyCode Up;
            public KeyCode Down;
            public KeyCode Left;
            public KeyCode Right;
        }

        public static readonly Scheme[] Schemes =
        {
            new Scheme
            {
                Label = "WASD",
                Up = KeyCode.W, Down = KeyCode.S, Left = KeyCode.A, Right = KeyCode.D
            },
            new Scheme
            {
                Label = "방향키",
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
