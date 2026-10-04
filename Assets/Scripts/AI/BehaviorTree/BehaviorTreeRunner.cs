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
