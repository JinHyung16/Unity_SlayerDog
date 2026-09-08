using UnityEngine;

namespace Jinhyeong_Common
{
    /// <summary>
    /// 화면 설정 — PC 게임이라 사용자가 해상도를 고를 수 있어야 한다.
    ///
    /// 프리셋만 제공한다. 임의 입력을 받으면 이상한 화면비가 들어와 UI 가 깨지는데,
    /// 그걸 다 감당하느니 16:9 만 주는 편이 낫다. 이 게임의 UI 와 카메라는 16:9 기준이다.
    ///
    /// 저장은 `PlayerPrefs` 다. 설정 하나 때문에 저장 체계를 세우는 건 과하고,
    /// 나중에 진행도 저장(E-1)이 들어오면 그쪽으로 옮긴다.
    /// </summary>
    public static class DisplaySettings
    {
        public struct Preset
        {
            public int Width;
            public int Height;

            public Preset(int w, int h)
            {
                Width = w;
                Height = h;
            }

            public string Label { get { return $"{Width} x {Height}"; } }
        }

        /// <summary>고를 수 있는 해상도. 전부 16:9 다.</summary>
        public static readonly Preset[] Presets =
        {
            new Preset(1280, 720),
            new Preset(1600, 900),
            new Preset(1920, 1080)
        };

        /// <summary>기본값. 어떤 PC 에서도 뜨는 크기여야 한다.</summary>
        public const int DefaultIndex = 0;

        private const string KeyIndex = "display.preset";
        private const string KeyFullscreen = "display.fullscreen";

        public static int PresetIndex { get; private set; } = DefaultIndex;
        public static bool Fullscreen { get; private set; }

        public static Preset Current
        {
            get { return Presets[Mathf.Clamp(PresetIndex, 0, Presets.Length - 1)]; }
        }

        /// <summary>
        /// 저장된 설정을 읽어 적용한다. 게임 시작 때 한 번 부른다.
        ///
        /// [주의] 에디터에서는 `Screen.SetResolution` 이 Game 뷰를 바꾸지 않는다.
        /// 실측 하네스가 Game 뷰 크기를 따로 잡는 이유가 이것이다 — 둘을 헷갈리면
        /// 「설정이 안 먹는다」로 잘못 진단하게 된다.
        /// </summary>
        public static void LoadAndApply()
        {
            PresetIndex = Mathf.Clamp(PlayerPrefs.GetInt(KeyIndex, DefaultIndex), 0, Presets.Length - 1);
            Fullscreen = PlayerPrefs.GetInt(KeyFullscreen, 0) != 0;
            Apply();
        }

        public static void Set(int presetIndex, bool fullscreen)
        {
            PresetIndex = Mathf.Clamp(presetIndex, 0, Presets.Length - 1);
            Fullscreen = fullscreen;

            PlayerPrefs.SetInt(KeyIndex, PresetIndex);
            PlayerPrefs.SetInt(KeyFullscreen, Fullscreen ? 1 : 0);
            PlayerPrefs.Save();

            Apply();
        }

        private static void Apply()
        {
            Preset p = Current;
            Screen.SetResolution(p.Width, p.Height, Fullscreen);
        }
    }
}
