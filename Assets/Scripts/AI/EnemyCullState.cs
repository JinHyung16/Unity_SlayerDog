using UnityEngine;

namespace Jinhyeong_AI
{

    public sealed class EnemyCullState
    {
        private readonly Enemy _enemy;
        private readonly Animator _animator;
        private readonly Renderer[] _renderers;

        private bool _visible = true;
        private bool _applied;

        public EnemyCullState(Enemy enemy)
        {
            _enemy = enemy;
            _animator = enemy.GetComponentInChildren<Animator>(true);
            _renderers = enemy.GetComponentsInChildren<Renderer>(true);

            if (_animator != null)
            {
                _animator.cullingMode = AnimatorCullingMode.CullCompletely;
            }
        }

        public void SetVisible(bool visible, float offscreenTickInterval)
        {
            if (_applied && _visible == visible)
                return;

            _visible = visible;
            _applied = true;

            if (_animator != null)
                _animator.enabled = visible;

            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];
                if (r == null)
                    continue;
                r.shadowCastingMode = visible
                    ? UnityEngine.Rendering.ShadowCastingMode.On
                    : UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            Jinhyeong_AI.BehaviorTree.BehaviorTreeRunner runner = _enemy != null ? _enemy.BTRunner : null;
            if (runner != null)
                runner.SetTickInterval(visible ? 0f : offscreenTickInterval);
        }
    }
}
