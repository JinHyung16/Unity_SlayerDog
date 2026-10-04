using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_Managers
{
    public enum ESfx
    {
        Hit,
        Kill,
        Pickup,
        LevelUp,
        Hurt,
        Clear,
        Dead,
        Click
    }

    public class SfxManager : MonoBehaviour
    {
        private const int VoiceCount = 12;

        private const float SameSoundGap = 0.04f;

        public static SfxManager Instance { get; private set; }

        private AudioSource[] _voices;
        private int _next;

        private readonly Dictionary<ESfx, AudioClip> _clips = new Dictionary<ESfx, AudioClip>(8);
        private readonly Dictionary<ESfx, float> _lastPlayed = new Dictionary<ESfx, float>(8);

        public static SfxManager Ensure()
        {
            if (Instance != null)
                return Instance;
            GameObject go = new GameObject("_SfxManager");
            return go.AddComponent<SfxManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _voices[i] = src;
            }

            LoadClips();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void LoadClips()
        {
            foreach (ESfx kind in System.Enum.GetValues(typeof(ESfx)))
            {
                string path = "Sfx/sfx_" + kind.ToString().ToLowerInvariant();
                AudioClip clip = Resources.Load<AudioClip>(path);
                if (clip == null)
                {
                    Debug.LogWarning($"[SfxManager] '{path}' 를 찾을 수 없습니다 — 그 소리는 나지 않습니다");
                    continue;
                }
                _clips[kind] = clip;
            }
        }

        public static int LoadedCount { get { return Instance != null ? Instance._clips.Count : 0; } }

        public static int PlayCount { get; private set; }

        public static void ResetPlayCount()
        {
            PlayCount = 0;
        }

        public static void Play(ESfx kind, float volume = 1f)
        {
            if (Instance == null)
                return;
            Instance.PlayInternal(kind, volume);
        }

        private void PlayInternal(ESfx kind, float volume)
        {
            if (_clips.TryGetValue(kind, out AudioClip clip) == false || clip == null)
                return;

            _lastPlayed.TryGetValue(kind, out float last);
            if (Time.unscaledTime - last < SameSoundGap)
                return;
            _lastPlayed[kind] = Time.unscaledTime;

            AudioSource src = _voices[_next];
            _next = (_next + 1) % _voices.Length;

            src.clip = clip;
            src.volume = Mathf.Clamp01(volume);
            src.pitch = 0.94f + (Mathf.PingPong(Time.unscaledTime * 7f, 1f) * 0.12f);
            src.Play();
            PlayCount++;
        }
    }
}
