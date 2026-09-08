using UnityEngine;

namespace Jinhyeong_AI
{

    /// <summary>
    /// 적 하나의 컬링 상태. MonoBehaviour 가 아니다 —
    /// 적이 수백이면 컴포넌트 Update 수백 개보다 시스템 루프 하나가 훨씬 싸다.
    ///
    /// 화면 밖이라고 AI 를 완전히 멈추면 적이 영영 다가오지 않는다.
    /// 그래서 '끄는' 게 아니라 '느리게 도는' 쪽으로 처리한다.
    /// </summary>
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
                // 화면 밖이면 Unity 가 알아서 본 갱신을 멈추게 한다.
                // 직접 끄는 것과 별개로, 이 설정만으로도 스키닝 비용이 크게 준다.
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

            // 렌더러는 Unity 프러스텀 컬링이 이미 처리하지만,
            // 그림자 캐스팅은 화면 밖 오브젝트도 계속 도는 경우가 있어 같이 끈다.
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
