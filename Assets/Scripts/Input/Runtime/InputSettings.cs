using UnityEngine;

namespace Jinhyeong_Input
{

    /// <summary>
    /// 이동키 프리셋 — WASD 와 방향키 중 하나. 사람마다 손이 다르고, 그 차이는 취향이 아니라 조작 성능이다.
    ///
    /// [사고] 처음엔 <see cref="InputBindings"/>(ScriptableObject) 를 런타임에 직접 고치려 했다.
    /// 에디터에서는 **에셋이 영구히 오염**되고(플레이를 끝내도 남는다), 빌드에서는 반대로
    /// 껐다 켜면 사라진다 — 같은 코드가 두 환경에서 정반대로 동작한다.
    /// 그래서 「기본 배치」는 에셋에 두고, 「사용자가 고른 것」은 여기서 <c>PlayerPrefs</c> 로 든다.
    /// 에셋은 읽기 전용으로만 쓴다.
    ///
    /// 프리셋만 준다 — 임의 리바인딩은 중복 키·모디파이어 조합을 전부 검사해야 하고,
    /// 그 검사를 안 하면 「움직일 수 없는 저장」이 만들어진다. 지금 필요한 것은 두 가지 손 모양뿐이다.
    /// </summary>
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

        /// <summary>고를 수 있는 배치. 순서가 곧 저장값이라 중간에 끼워 넣지 않는다 — 뒤에만 더한다.</summary>
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

        /// <summary>저장된 배치를 읽는다. 게임 시작 때 한 번 부른다.</summary>
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
