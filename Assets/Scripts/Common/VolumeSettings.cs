using UnityEngine;

namespace Jinhyeong_Common
{
    // 전체 음량 (AudioListener.volume). 다섯 단계 · PlayerPrefs 에 남긴다 (TASKS C-3)
    public static class VolumeSettings
    {
        public static readonly float[] Steps = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        public const int DefaultIndex = 3;

        private const string KeyIndex = "audio.master";

        public static int Index { get; private set; } = DefaultIndex;

        public static string LabelOf(int i)
        {
            return Mathf.RoundToInt(Steps[Mathf.Clamp(i, 0, Steps.Length - 1)] * 100f) + "%";
        }

        public static void LoadAndApply()
        {
            Index = Mathf.Clamp(PlayerPrefs.GetInt(KeyIndex, DefaultIndex), 0, Steps.Length - 1);
            Apply();
        }

        public static void Set(int index)
        {
            Index = Mathf.Clamp(index, 0, Steps.Length - 1);
            PlayerPrefs.SetInt(KeyIndex, Index);
            PlayerPrefs.Save();
            Apply();
        }

        private static void Apply()
        {
            AudioListener.volume = Steps[Index];
        }
    }
}
