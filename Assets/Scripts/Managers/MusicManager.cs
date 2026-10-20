using UnityEngine;

namespace Jinhyeong_Managers
{
    // 배경음악 한 곳. 로비 = bgm_lobby, 판 = bgm_<맵키> (Resources/Bgm, build_bgm.py)
    // 음량은 전체 음량(VolumeSettings → AudioListener)을 따르고 여기서는 효과음과의 비율만 정한다
    public class MusicManager : MonoBehaviour
    {
        private const float Volume = 0.45f;
        private const string Folder = "Bgm/bgm_";

        private static MusicManager _instance;
        private AudioSource _source;
        private string _current = string.Empty;

        public static void Play(string key)
        {
            Ensure();
            _instance.PlayInternal(key);
        }

        public static void Stop()
        {
            if (_instance != null && _instance._source != null)
            {
                _instance._source.Stop();
                _instance._current = string.Empty;
            }
        }

        private static void Ensure()
        {
            if (_instance != null)
                return;
            GameObject go = new GameObject("_MusicManager");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MusicManager>();
            _instance._source = go.AddComponent<AudioSource>();
            _instance._source.loop = true;
            _instance._source.playOnAwake = false;
            _instance._source.volume = Volume;
            _instance._source.ignoreListenerPause = true;
        }

        private void PlayInternal(string key)
        {
            if (string.IsNullOrEmpty(key) || key == _current)
                return;
            AudioClip clip = Resources.Load<AudioClip>(Folder + key);
            if (clip == null)
            {
                Debug.LogWarning($"[MusicManager] '{Folder}{key}' 이 없다 — 음악 없이 진행");
                return;
            }
            _current = key;
            _source.clip = clip;
            _source.Play();
        }
    }
}
