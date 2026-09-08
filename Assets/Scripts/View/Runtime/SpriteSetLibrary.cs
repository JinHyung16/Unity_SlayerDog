using System;
using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_View
{
    /// <summary>
    /// 한 프리팹이 여러 비주얼을 갈아 끼울 때 쓰는 스프라이트 묶음.
    ///
    /// 적 프리팹은 하나이고 타입(꼬물이/쌩쌩이/…)만 DB 로 바뀐다 — 타입마다 프리팹을 늘리면
    /// 적 5종이 50종 될 때 프리팹 50개를 관리해야 한다. 키로 골라 끼우는 편이 데이터 주도적이다.
    ///
    /// 배열이라 Awake 에서 딕셔너리로 옮긴다. 적 수백 마리가 스폰될 때 선형 탐색은 티가 난다.
    /// </summary>
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

        /// <summary>키에 맞는 프레임으로 갈아 끼운다. 없으면 아무것도 안 한다(기존 비주얼 유지).</summary>
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
