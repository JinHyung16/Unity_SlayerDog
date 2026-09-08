using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_AI.BehaviorTree
{

    public class BehaviorTreeRunner : BaseBehaviour
    {
        public bool Paused = false;

        private BTNode _root;

        private float _tickInterval;
        private float _accumulated;

        /// <summary>
        /// 틱 간격(초). 0이면 매 프레임.
        ///
        /// 화면 밖 적을 완전히 멈추면 영영 다가오지 않으므로, 끄는 대신 드문드문 돌린다.
        /// 누적된 시간을 그대로 넘기기 때문에 이동 거리는 보존된다 — 띄엄띄엄 움직일 뿐 느려지지 않는다.
        /// </summary>
        public void SetTickInterval(float seconds)
        {
            _tickInterval = Mathf.Max(0f, seconds);
            if (_tickInterval <= 0f)
                _accumulated = 0f;
        }

        public void SetRoot(BTNode root)
        {
            if (_root != null)
                _root.Reset();
            _root = root;
        }

        protected override void OnDisabled()
        {
            // 풀에 반납됐다 다시 나올 때 이전 상태가 남으면 안 된다
            _tickInterval = 0f;
            _accumulated = 0f;
        }

        private void Update()
        {
            if (Paused || _root == null)
                return;

            if (_tickInterval <= 0f)
            {
                _root.Tick(Time.deltaTime);
                return;
            }

            _accumulated += Time.deltaTime;
            if (_accumulated < _tickInterval)
                return;

            float dt = _accumulated;
            _accumulated = 0f;
            _root.Tick(dt);
        }
    }
}
