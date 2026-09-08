using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_Managers;

namespace Jinhyeong_SkillSystem
{
    public class ChainLightningEffect : BaseBehaviour
    {
        private const int SubPerSegment = 4;
        private const float JagFactor = 0.15f;

        private static readonly List<Vector3> _pathBuf = new List<Vector3>(64);

        [SerializeField] private LineRenderer _lr;
        private float _life;
        private float _age;
        private Color _color;

        public static void Spawn(IList<Vector3> nodes, Color color, float life = 0.22f, float width = 0.22f)
        {
            if (nodes == null || nodes.Count < 2)
                return;

            if (PoolManager.Instance == null)
                return;

            GameObject go = PoolManager.Instance.Pool_Fx_Get(CommonConfig.Skill.ChainFxKey);
            if (go == null)
                return;

            ChainLightningEffect fx = go.GetComponent<ChainLightningEffect>();
            if (fx == null)
            {
                Debug.LogWarning($"[ChainLightningEffect] '{CommonConfig.Skill.ChainFxKey}' 프리팹에 컴포넌트가 없습니다");
                return;
            }

            if (fx._lr == null)
                fx._lr = go.GetComponent<LineRenderer>();

            fx._lr.widthMultiplier = width;

            BuildJaggedPath(nodes);
            fx._lr.positionCount = _pathBuf.Count;
            for (int i = 0; i < _pathBuf.Count; i++)
                fx._lr.SetPosition(i, _pathBuf[i]);

            fx._life = life > 0f ? life : 0.18f;
            fx._age = 0f;
            fx._color = color;
            fx.ApplyAlpha(1f);
        }

        private static void BuildJaggedPath(IList<Vector3> nodes)
        {
            _pathBuf.Clear();
            _pathBuf.Add(nodes[0]);

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                Vector3 a = nodes[i];
                Vector3 b = nodes[i + 1];
                Vector3 dir = b - a;
                float len = dir.magnitude;
                if (len < 0.0001f)
                {
                    _pathBuf.Add(b);
                    continue;
                }

                Vector3 fwd = dir / len;
                Vector3 side = Vector3.Cross(fwd, Vector3.up);
                if (side.sqrMagnitude < 0.0001f)
                    side = Vector3.right;
                side.Normalize();
                Vector3 up = Vector3.Cross(side, fwd);
                float mag = len * JagFactor;

                for (int s = 1; s <= SubPerSegment; s++)
                {
                    float t = (float)s / (SubPerSegment + 1);
                    Vector3 p = Vector3.Lerp(a, b, t);
                    p += side * Random.Range(-mag, mag) + up * Random.Range(-mag, mag);
                    _pathBuf.Add(p);
                }
                _pathBuf.Add(b);
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = _age / _life;
            if (t >= 1f)
            {
                if (PoolManager.Instance != null)
                    PoolManager.Instance.Pool_Fx_Return(CommonConfig.Skill.ChainFxKey, gameObject);
                else
                    Destroy(gameObject);
                return;
            }
            ApplyAlpha(1f - t);
        }

        private void ApplyAlpha(float alpha)
        {
            if (_lr == null)
                return;
            Color c = _color;
            c.a = alpha;
            _lr.startColor = c;
            _lr.endColor = c;
        }

    }
}
