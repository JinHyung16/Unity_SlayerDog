using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_Managers
{
    /// <summary>효과음 종류. 파일명(`sfx_<소문자>`)과 1:1 이다.</summary>
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

    /// <summary>
    /// 효과음 재생.
    ///
    /// [사고] `AudioSource` 사용처가 **한 곳도 없었다.** 타격감의 절반이 소리인데 그게 없으니
    /// 화면만 보고 「맞았나」를 판단해야 했다.
    ///
    /// 두 가지를 조심한다.
    ///   1. 초당 수십 번 겹친다 — 매번 AudioSource 를 만들면 그것만으로 프레임이 죽는다.
    ///      소스를 미리 여러 개 만들어 돌려 쓴다.
    ///   2. 같은 소리가 한 프레임에 여러 번 나면 음량이 겹쳐 찢어진다.
    ///      같은 종류는 아주 짧은 간격 안에서 한 번만 낸다.
    /// </summary>
    public class SfxManager : MonoBehaviour
    {
        /// <summary>동시에 울릴 수 있는 소리 수. 이보다 많으면 가장 오래된 것을 덮어쓴다.</summary>
        private const int VoiceCount = 12;

        /// <summary>같은 종류를 다시 낼 수 있게 되기까지의 간격(초).</summary>
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
                // 2D 로 둔다. 화면 안에서만 벌어지는 일이라 위치별 정위가 이득이 없고,
                // 3D 로 두면 카메라와의 거리 때문에 소리가 들쭉날쭉해진다.
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

        /// <summary>
        /// Resources 에서 읽는다. 어드레서블에 8개를 따로 등록하는 것보다 짧다.
        /// 없는 파일은 조용히 건너뛴다 — 소리가 없다고 게임이 멈출 이유는 없다.
        /// </summary>
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

        /// <summary>몇 종이 실제로 로드됐나. 소리는 스크린샷으로 확인할 수 없어 실측이 이걸 읽는다.</summary>
        public static int LoadedCount { get { return Instance != null ? Instance._clips.Count : 0; } }

        /// <summary>이번 런에서 소리를 낸 횟수. 0 이면 「배선은 됐는데 안 울린다」는 뜻이다.</summary>
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

            // 같은 소리가 한 프레임에 몰리면 음량이 겹쳐 찢어진다.
            _lastPlayed.TryGetValue(kind, out float last);
            if (Time.unscaledTime - last < SameSoundGap)
                return;
            _lastPlayed[kind] = Time.unscaledTime;

            AudioSource src = _voices[_next];
            _next = (_next + 1) % _voices.Length;

            src.clip = clip;
            src.volume = Mathf.Clamp01(volume);
            // 같은 소리가 반복되면 귀가 금방 지친다. 음정을 조금씩 흔든다.
            src.pitch = 0.94f + (Mathf.PingPong(Time.unscaledTime * 7f, 1f) * 0.12f);
            src.Play();
            PlayCount++;
        }
    }
}
