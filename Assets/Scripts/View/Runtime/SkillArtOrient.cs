using UnityEngine;
using Jinhyeong_Character;
using Jinhyeong_Common;

namespace Jinhyeong_View
{
    /// <summary>
    /// 스킬 아트를 탑다운 화면에 맞게 세운다.
    ///
    /// [사고] FX 프리팹에 방향 처리가 아무것도 없어서, 스프라이트가 **스킬 루트의 회전을 그대로**
    /// 따라갔다. 루트는 조준 방향(XZ 평면)으로 돌기 때문에 카메라(피치 50°)에서 보면
    /// 스프라이트가 비스듬히 서거나 거의 옆날로 보였다. 「스킬을 썼는데 뭐가 나갔는지 모르겠다」의 원인.
    ///
    /// 이 게임의 시각 언어는 「평평한 바닥 + 세워 놓은 스프라이트」다. 그래서 두 가지만 쓴다.
    ///   지면(Ground)  — 바닥에 눕는다. 부채꼴·장판·빔처럼 **어디를 향하는지**가 정보인 스킬.
    ///                   조준 방향으로 돌아가므로 방향이 그대로 읽힌다.
    ///   빌보드(Billboard) — 카메라를 향해 선다. 자기 몸에 붙는 각성·보호막처럼 방향이 없는 스킬.
    /// </summary>
    public class SkillArtOrient : BaseBehaviour
    {
        public enum EMode
        {
            /// <summary>바닥에 눕고 조준 방향을 가리킨다.</summary>
            Ground,
            /// <summary>카메라를 향해 선다.</summary>
            Billboard
        }

        [SerializeField] private EMode _mode = EMode.Ground;

        public EMode Mode { get { return _mode; } set { _mode = value; } }

        /// <summary>
        /// 그림의 「앞쪽」이 텍스처 어느 축인지.
        ///
        /// 이 프로젝트의 FX 는 오른쪽(+X)을 향해 그려져 있다(왈왈의 부채꼴, 눈빛 빔 모두 오른쪽으로 뻗는다).
        /// 바닥에 눕힐 때 그 +X 가 조준 방향을 향하도록 한 번 더 돌린다.
        /// </summary>
        private const float ArtForwardYaw = -90f;

        /// <summary>지면 연출이 쓸 정렬 순서. 캐릭터(10)보다 아래, 픽업(5)보다 위.</summary>
        private const int GroundOrder = 7;

        /// <summary>빌보드 연출이 쓸 정렬 순서. 캐릭터보다 위.</summary>
        private const int OverOrder = 20;

        private SpriteRenderer[] _renderers;
        private EMode _appliedTo;
        private bool _orderApplied;

        /// <summary>
        /// 지면 연출은 캐릭터 **아래**로 깔린다.
        ///
        /// [사고] 모든 연출이 캐릭터 위에 그려져, 바닥에 눕힌 장판·알갱이가 주인공을 덮었다.
        /// 탑다운에서 바닥에 있는 것이 캐릭터를 가리면 「어디에 서 있는지」가 안 보인다.
        /// 몸에 붙는 버프만 위로 올린다.
        /// </summary>
        private void ApplyOrder()
        {
            if (_orderApplied && _appliedTo == _mode)
                return;

            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponentsInChildren<SpriteRenderer>(true);

            int order = _mode == EMode.Ground ? GroundOrder : OverOrder;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].sortingOrder = order;
            }

            _appliedTo = _mode;
            _orderApplied = true;
        }

        private void LateUpdate()
        {
            ApplyOrder();

            if (_mode == EMode.Billboard)
            {
                Camera cam = CameraFollow.GameCamera;
                if (cam != null)
                    transform.rotation = cam.transform.rotation;
                return;
            }

            // 부모(스킬 루트)의 조준 방향만 가져와 바닥에 눕힌다.
            // 부모 회전을 그대로 쓰면 피치까지 따라가 스프라이트가 서 버린다.
            Vector3 fwd = transform.parent != null ? transform.parent.forward : Vector3.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f)
                fwd = Vector3.forward;

            float yaw = Quaternion.LookRotation(fwd, Vector3.up).eulerAngles.y;

            // X +90 이면 스프라이트가 XZ 평면에 눕고 앞면이 위(카메라 쪽)를 본다.
            transform.rotation = Quaternion.Euler(90f, yaw + ArtForwardYaw, 0f);
        }
    }
}
