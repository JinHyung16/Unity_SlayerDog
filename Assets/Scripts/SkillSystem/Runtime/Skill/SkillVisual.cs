using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_Managers;

namespace Jinhyeong_SkillSystem.Skill
{

    public class SkillVisual : BaseBehaviour
    {
        private SkillConfig _config;
        private GameObject _art;
        private string _artKey;
        private readonly List<SpriteRenderer> _sprites = new List<SpriteRenderer>(4);
        private string _tintHex;
        private Jinhyeong_View.SkillArtOrient _orient;
        private readonly List<Jinhyeong_View.SpriteAnimator> _anims =
            new List<Jinhyeong_View.SpriteAnimator>(4);

        // 스킬 색 (데이터 VisualColor). 적중 버스트 · 잔상이 같은 색을 쓴다
        public Color TintColor { get; private set; } = Color.white;

        private SpriteRenderer _core;
        private Vector3 _lastGhostPos;
        private float _lastGhostTime;

        // 하네스 계측: 이번 판에 움직인 스킬 아트 / 잔상을 남긴 스킬 아트
        public static readonly HashSet<string> MovedKeys = new HashSet<string>();
        public static readonly HashSet<string> GhostKeys = new HashSet<string>();

        public static void ResetTrailWatch()
        {
            MovedKeys.Clear();
            GhostKeys.Clear();
        }

        private const float DefaultLife = 0.45f;

        private const float MinFps = 6f;

        private const float MaxFps = 14f;

        private void Awake()
        {
            _config = GetComponent<SkillConfig>();
        }

        public void BindArt(string artKey)
        {
            if (_art != null && _artKey == artKey)
                return;

            if (_art != null)
            {
                Destroy(_art);
                _art = null;
            }
            _anims.Clear();

            if (string.IsNullOrEmpty(artKey) || artKey == PoolManager.KeyEmpty)
                return;

            AddressableManager am = AddressableManager.Instance;
            GameObject prefab = am != null ? am.Get(artKey) : null;
            if (prefab == null)
            {
                Debug.LogWarning($"[SkillVisual] 아트 '{artKey}' 가 로드되지 않았습니다 — 연출 없이 진행");
                return;
            }

            _art = Instantiate(prefab, transform);
            _art.transform.localPosition = Vector3.zero;
            _art.transform.localRotation = Quaternion.identity;
            _artKey = artKey;

            _art.GetComponentsInChildren(true, _anims);
            _art.GetComponentsInChildren(true, _sprites);
            _tintHex = null;

            // 잔상은 심지(Core) 그림을 남긴다. 단층 프리팹이면 첫 렌더러
            _core = null;
            Transform coreTr = _art.transform.Find("Core");
            if (coreTr != null)
                _core = coreTr.GetComponent<SpriteRenderer>();
            if (_core == null && _sprites.Count > 0)
                _core = _sprites[0];

            _orient = _art.GetComponent<Jinhyeong_View.SkillArtOrient>();
            if (_orient == null)
                _orient = _art.AddComponent<Jinhyeong_View.SkillArtOrient>();
        }

        public void ApplyTint(string hex)
        {
            if (string.IsNullOrEmpty(hex) || _tintHex == hex)
                return;

            Color c;
            if (ColorUtility.TryParseHtmlString(hex, out c) == false)
            {
                Debug.LogWarning($"[SkillVisual] 색 '{hex}' 을 읽을 수 없습니다 (아트 '{_artKey}')");
                _tintHex = hex;
                return;
            }

            _tintHex = hex;
            TintColor = c;
            for (int i = 0; i < _sprites.Count; i++)
            {
                if (_sprites[i] != null)
                    _sprites[i].color = Jinhyeong_View.KeepOwnColor.Resolve(_sprites[i], c);
            }
        }

        public void SetOrient(Jinhyeong_View.SkillArtOrient.EMode mode)
        {
            if (_orient != null)
                _orient.Mode = mode;
        }

        public void Play()
        {
            float life = LifeSeconds();
            _lastGhostPos = transform.position;
            _lastGhostTime = Time.time;

            for (int i = 0; i < _anims.Count; i++)
            {
                Jinhyeong_View.SpriteAnimator anim = _anims[i];
                if (anim == null)
                    continue;

                int frames = anim.FrameCount;
                if (frames <= 1)
                    continue;

                float fps = Mathf.Clamp(frames / Mathf.Max(0.12f, life), MinFps, MaxFps);
                anim.SetFps(fps, true);
            }
        }

        // 이동형 스킬 잔상 — 움직인 거리 · 간격이 둘 다 넘으면 한 장. 제자리 장판 · 오라는 남기지 않는다
        private void LateUpdate()
        {
            if (_core == null || _config == null || _config.Despawning)
                return;

            Vector3 pos = transform.position;
            float step = (pos - _lastGhostPos).sqrMagnitude;
            float min = CommonConfig.Skill.GhostMinStep;
            if (step < min * min)
                return;

            MovedKeys.Add(_artKey);
            if (Time.time - _lastGhostTime < CommonConfig.Skill.GhostInterval)
                return;

            GhostFade.Spawn(_core, TintColor);
            GhostKeys.Add(_artKey);
            _lastGhostPos = pos;
            _lastGhostTime = Time.time;
        }

        private float LifeSeconds()
        {
            if (_config == null || _config.DespawnNode == null)
            {
                return DefaultLife;
            }
            if (_config.DespawnNode.NodeType != ESkillNodeType.DespawnAfterTime)
            {
                return DefaultLife;
            }
            float d = _config.DespawnNode.GetFloat(ESkillParamKey.Value, null, DefaultLife);
            return d > 0.05f ? d : DefaultLife;
        }

        private const float ArtWorldDiameter = 2f;

        private const float MaxScale = 3.5f;

        private const float DecorScale = 1.45f;

        public void ApplyHitScale()
        {
            if (_config == null || _config.HitNode == null)
                return;

            float radius = _config.GetHitFloat(ESkillParamKey.Radius, 0f);
            if (radius <= 0f)
            {
                transform.localScale = Vector3.one * DecorScale;
                return;
            }

            float scale = radius * 2f / ArtWorldDiameter;
            transform.localScale = Vector3.one * Mathf.Clamp(scale, 1.15f, MaxScale);
        }

        public void ShowChain(IList<Vector3> points)
        {
            if (points == null || points.Count < 2)
                return;
            ChainLightningEffect.Spawn(points, new Color(0.18f, 0.90f, 1f, 1f));
        }

        protected override void OnDisabled()
        {
            transform.localScale = Vector3.one;
        }
    }
}
