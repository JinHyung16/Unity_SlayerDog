using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_Managers;

namespace Jinhyeong_SkillSystem.Skill
{

    /// <summary>
    /// DB 의 visual 모듈에 대응하는 컴포넌트 — 연출만 책임진다.
    ///
    /// 아트는 자식으로 붙인다. 아트 프리팹에는 스크립트가 하나도 없어야 한다 —
    /// 그래야 아티스트가 만든 프리팹이든 에셋 스토어 것이든 그대로 꽂을 수 있다.
    /// 로직은 전부 이 루트 프리팹(Skill_Root)에 있다.
    /// </summary>
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

        /// <summary>한 방짜리 연출이 화면에 머무는 시간. 눈이 형태를 읽기에 이 정도는 필요하다.</summary>
        private const float DefaultLife = 0.45f;

        /// <summary>느려도 이보다는 움직여야 정지 화면으로 안 보인다.</summary>
        private const float MinFps = 6f;

        /// <summary>빨라도 이 위로 가면 깜빡임이 된다.</summary>
        private const float MaxFps = 14f;

        private void Awake()
        {
            _config = GetComponent<SkillConfig>();
        }

        /// <summary>
        /// 아트 자식을 붙인다. 풀은 아트 키별로 나뉘어 있어서 재사용 시 키가 바뀌지 않는다 —
        /// 그래서 한 번 붙이면 계속 쓴다.
        /// </summary>
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

            // 아트를 탑다운 화면에 맞게 세운다. 이게 없으면 스프라이트가 스킬 루트의 조준 회전을
            // 그대로 따라가 카메라(피치 50°)에서 옆날로 보인다.
            _orient = _art.GetComponent<Jinhyeong_View.SkillArtOrient>();
            if (_orient == null)
                _orient = _art.AddComponent<Jinhyeong_View.SkillArtOrient>();
        }

        /// <summary>
        /// 이펙트 색을 입힌다.
        ///
        /// [사고] 이펙트를 흰색 한 벌로 그려 놓고 **아무도 색을 입히지 않았다.**
        /// DB 의 visual.color 는 있었지만 런타임까지 이어진 적이 없어, 27개 스킬이
        /// 전부 흰 그림으로 나갔다. 색이 계열을 구분하는 유일한 수단인데 그게 죽어 있었다.
        ///
        /// 풀에서 재사용되는 오브젝트라 같은 색이면 건너뛴다 — 매 발사마다
        /// 스프라이트를 훑을 이유가 없다.
        /// </summary>
        public void ApplyTint(string hex)
        {
            if (string.IsNullOrEmpty(hex) || _tintHex == hex)
                return;

            // 제 색을 가진 그림은 건드리지 않는다.
            if (_art != null && _art.GetComponent<Jinhyeong_View.KeepOwnColor>() != null)
            {
                _tintHex = hex;   // 같은 판정을 매 발사마다 반복하지 않는다
                return;
            }

            Color c;
            if (ColorUtility.TryParseHtmlString(hex, out c) == false)
            {
                Debug.LogWarning($"[SkillVisual] 색 '{hex}' 을 읽을 수 없습니다 (아트 '{_artKey}')");
                _tintHex = hex;   // 같은 실패를 매 프레임 반복하지 않는다
                return;
            }

            _tintHex = hex;
            for (int i = 0; i < _sprites.Count; i++)
            {
                if (_sprites[i] != null)
                    _sprites[i].color = c;
            }
        }

        /// <summary>
        /// 이 스킬 아트를 어떻게 세울지 정한다.
        ///
        /// 방향이 정보인 스킬(부채꼴·빔·투척)은 바닥에 눕혀 조준 방향을 가리키게 하고,
        /// 자기 몸에 붙는 스킬(각성·보호막)은 카메라를 향해 세운다.
        /// </summary>
        public void SetOrient(Jinhyeong_View.SkillArtOrient.EMode mode)
        {
            if (_orient != null)
                _orient.Mode = mode;
        }

        /// <summary>풀에서 꺼내 재사용할 때 파티클을 처음부터 다시 재생한다.</summary>
        /// <summary>
        /// 연출을 재생한다.
        ///
        /// [사고] 이 함수는 **ParticleSystem 만** 돌리고 있었다. 그런데 이 프로젝트의 이펙트
        /// 프리팹에는 파티클이 하나도 없다(전부 스프라이트 프레임 애니다).
        /// 그래서 발사할 때마다 불렸지만 **아무 일도 하지 않았고**, 애니는 풀에서 꺼내질 때
        /// OnEnable 이 한 번 되감는 것에만 의존했다.
        ///
        /// 게다가 프리팹이 20fps · 반복 없음이라 5프레임이 **0.25초**만에 끝나고
        /// 마지막 프레임에서 얼어붙었다. 장판처럼 3초를 사는 스킬은 나머지 2.75초 동안
        /// 정지 화면이었다. 「이펙트가 말도 안 되게 나온다」의 정체다.
        ///
        /// 이제 수명에 맞춰 속도를 정한다 — 오래 사는 연출은 반복해서 계속 움직이고,
        /// 한 방짜리는 despawn 전에 한 바퀴가 돌도록 맞춘다.
        /// </summary>
        public void Play()
        {
            float life = LifeSeconds();

            for (int i = 0; i < _anims.Count; i++)
            {
                Jinhyeong_View.SpriteAnimator anim = _anims[i];
                if (anim == null)
                    continue;

                int frames = anim.FrameCount;
                if (frames <= 1)
                    continue;

                // 한 바퀴가 수명 안에 들어오게. 너무 느리면 정지 화면, 너무 빠르면 깜빡임이다.
                float fps = Mathf.Clamp(frames / Mathf.Max(0.12f, life), MinFps, MaxFps);
                anim.SetFps(fps, true);
            }
        }

        /// <summary>이 연출이 몇 초를 사는지. Despawn 노드가 알고, 없으면 짧은 한 방으로 본다.</summary>
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

        /// <summary>타격 반경이 있으면 그만큼 키워서 범위를 눈으로 보이게 한다.</summary>
        /// <summary>
        /// 이펙트 스프라이트가 스케일 1 에서 차지하는 월드 폭(유닛).
        /// 64px 그림을 PPU 32 로 넣으므로 2 유닛이고, 그 2 유닛이 곧 **지름**이다.
        /// </summary>
        private const float ArtWorldDiameter = 2f;

        /// <summary>
        /// 확대 상한. 픽셀 아트는 키울수록 무너진다 — 8배로 늘리면 이빨이 판때기가 되고
        /// 화면 절반을 덮어 무슨 스킬인지 알 수 없게 된다.
        /// 카메라가 세로 약 18 유닛을 보므로 7 유닛이 「큰 광역」의 상한으로 알맞다.
        /// </summary>
        private const float MaxScale = 3.5f;

        /// <summary>반경이 없는 장식 연출의 배율. 캐릭터(2유닛)보다 커야 눈에 띈다.</summary>
        private const float DecorScale = 1.45f;

        public void ApplyHitScale()
        {
            if (_config == null || _config.HitNode == null)
                return;

            float radius = _config.GetHitFloat(ESkillParamKey.Radius, 0f);
            if (radius <= 0f)
            {
                // 반경이 없는 연출은 「어디까지 맞는가」를 나타내지 않는다 — 순수한 타격 표현이다.
                // 그래서 키워도 거짓말이 아니다.
                //
                // [측정] 화면에서 이펙트가 캐릭터의 6할 크기였다. 레퍼런스 도트 시트는
                // 폭발 · 탄이 캐릭터보다 **크다** — 그래야 「무슨 일이 일어났다」가 읽힌다.
                // 캐릭터 스프라이트를 줄이는 방법도 있지만, 1px 외곽선이 축소에서 끊긴다.
                transform.localScale = Vector3.one * DecorScale;
                return;
            }

            // [사고] 예전 수식은 radius * 2 였다. 스프라이트가 이미 지름 2 유닛인데 지름을
            // 한 번 더 곱한 셈이라 실제 판정보다 **두 배 큰** 연출이 나갔다.
            float scale = radius * 2f / ArtWorldDiameter;
            // 반경이 있는 연출은 **범위를 정직하게** 보여야 한다 — 부풀리면 사거리를 오인한다.
            // 다만 너무 작으면 안 보이므로 하한만 조금 올린다.
            transform.localScale = Vector3.one * Mathf.Clamp(scale, 1.15f, MaxScale);
        }

        public void ShowChain(IList<Vector3> points)
        {
            if (points == null || points.Count < 2)
                return;
            ChainLightningEffect.Spawn(points, new Color(0.6f, 0.85f, 1f, 1f));
        }

        protected override void OnDisabled()
        {
            transform.localScale = Vector3.one;
        }
    }
}
