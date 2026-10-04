using UnityEngine;

namespace Jinhyeong_Common
{
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

        public static readonly Preset[] Presets =
        {
            new Preset(1280, 720),
            new Preset(1600, 900),
            new Preset(1920, 1080)
        };

        public const int DefaultIndex = 0;

        private const string KeyIndex = "display.preset";
        private const string KeyFullscreen = "display.fullscreen";

        public static int PresetIndex { get; private set; } = DefaultIndex;
        public static bool Fullscreen { get; private set; }

        public static Preset Current
        {
            get { return Presets[Mathf.Clamp(PresetIndex, 0, Presets.Length - 1)]; }
        }

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
