using System;
using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_View
{
    public class SpriteSetLibrary : BaseBehaviour
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("DB 의 EnemyType.sprite 값과 같아야 한다.")]
            public string Key = string.Empty;
            public Sprite[] Frames;

            [Tooltip("방향 프레임. 비우면 정면 그림을 그대로 쓴다 — 납치선처럼 방향 개념이 없는 개체.")]
            public Sprite[] FramesUp;
            public Sprite[] FramesSide;
        }

        [SerializeField] private CharacterView _view;
        [SerializeField] private Entry[] _entries;

        private System.Collections.Generic.Dictionary<string, Entry> _map;

        private void Awake()
        {
            if (_view == null)
                _view = GetComponent<CharacterView>();

            _map = new System.Collections.Generic.Dictionary<string, Entry>(
                _entries != null ? _entries.Length : 0, StringComparer.Ordinal);

            if (_entries == null)
                return;
            for (int i = 0; i < _entries.Length; i++)
            {
                Entry e = _entries[i];
                if (e == null || string.IsNullOrEmpty(e.Key) || e.Frames == null || e.Frames.Length == 0)
                    continue;
                _map[e.Key] = e;
            }
        }

        public bool Apply(string key)
        {
            if (_view == null || _map == null || string.IsNullOrEmpty(key))
                return false;
            if (_map.TryGetValue(key, out Entry e) == false)
                return false;
            _view.SetFrameSet(e.Frames, e.Frames, e.FramesUp, e.FramesSide);
            return true;
        }
    }
}
