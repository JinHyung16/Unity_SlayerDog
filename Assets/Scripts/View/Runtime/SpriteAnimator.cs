using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_View
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnimator : BaseBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Sprite[] _frames;

        [Tooltip("초당 프레임. 픽셀 애니는 8~12 가 자연스럽다.")]
        [SerializeField] private float _fps = 10f;

        [SerializeField] private bool _loop = true;

        [Tooltip("끝나면 게임오브젝트를 끈다. 풀 반환은 소유자가 한다.")]
        [SerializeField] private bool _disableOnFinish = false;

        private float _time;
        private int _index;
        private bool _finished;

        public bool Finished { get { return _finished; } }
        public int FrameCount { get { return _frames != null ? _frames.Length : 0; } }

        public void SetFrames(Sprite[] frames, float fps, bool loop)
        {
            _frames = frames;
            _fps = fps > 0f ? fps : 10f;
            _loop = loop;
            Rewind();
        }

        public void SetFps(float fps, bool loop)
        {
            _fps = fps > 0f ? fps : 10f;
            _loop = loop;
            Rewind();
        }

        public void Rewind()
        {
            _time = 0f;
            _index = 0;
            _finished = false;
            Apply();
        }

        private void Awake()
        {
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();
        }

        protected override void OnEnabled()
        {
            Rewind();
        }

        private void Update()
        {
            if (_finished || _frames == null || _frames.Length <= 1)
                return;

            _time += Time.deltaTime;
            float step = 1f / _fps;
            while (_time >= step)
            {
                _time -= step;
                _index++;
                if (_index < _frames.Length)
                    continue;

                if (_loop)
                {
                    _index = 0;
                }
                else
                {
                    _index = _frames.Length - 1;
                    _finished = true;
                    if (_disableOnFinish)
                        gameObject.SetActive(false);
                    break;
                }
            }
            Apply();
        }

        private void Apply()
        {
            if (_renderer == null || _frames == null || _frames.Length == 0)
                return;
            int i = Mathf.Clamp(_index, 0, _frames.Length - 1);
            _renderer.sprite = _frames[i];
        }
    }
}
