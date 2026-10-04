using UnityEngine;
using UnityEngine.Rendering.Universal;
using Jinhyeong_Common;
using Jinhyeong_Managers;

namespace Jinhyeong_Character
{
    public class CameraFollow : BaseBehaviour
    {
        [Header("Follow")]
        [Tooltip("타겟 위 응시 지점. 캐릭터 발밑이 아니라 몸 중앙을 보게 살짝 올린다.")]
        [SerializeField] private Vector3 _pivotOffset = new Vector3(0f, 0.9f, 0f);

        [Tooltip("위치 SmoothDamp 시간상수. 작을수록 빠르게 따라감.")]
        [SerializeField] private float _smoothTime = 0.08f;

        [Header("View")]
        [Tooltip("오쏘그래픽 세로 반높이. 값이 클수록 넓게 보인다.")]
        [SerializeField] private float _orthoSize = CommonConfig.SurvivorView.CameraOrthoSize;

        [Tooltip("카메라 내림각. 스프라이트가 서 있는 것처럼 보이려면 90° 로 눕히면 안 된다.")]
        [SerializeField] private float _pitch = CommonConfig.SurvivorView.CameraPitch;

        [Tooltip("타겟에서 뒤로 물러나는 거리. 오쏘그래픽이라 화면 크기와 무관하고 클리핑에만 영향.")]
        [SerializeField] private float _distance = CommonConfig.SurvivorView.CameraDistance;

        private Transform _target;
        private Vector3 _velocity;
        private Camera _gameCamera;
        private Camera _uiCamera;

        public float Yaw { get { return 0f; } }
        public float Pitch { get { return _pitch; } }
        public float Distance { get { return _distance; } }

        public Camera GameCameraRef { get { return _gameCamera; } }
        public Camera UICameraRef { get { return _uiCamera; } }

        public static CameraFollow Active { get; private set; }

        public static Camera GameCamera { get { return Active != null ? Active._gameCamera : null; } }

        public static Camera UICamera { get { return Active != null ? Active._uiCamera : null; } }

        public static Quaternion ViewRotation { get; private set; } = Quaternion.Euler(50f, 0f, 0f);

        private void Awake()
        {
            if (transform.parent != null)
                transform.SetParent(null, true);

            ResolveChildCameras();
            ApplyProjection();
        }

        protected override void OnEnabled()
        {
            Active = this;
            GameEvents.OnPlayerSpawned += HandlePlayerSpawned;
            GameEvents.OnPlayerDespawned += HandlePlayerDespawned;

            Player p = GameEvents.CurrentPlayer;
            if (p != null)
                _target = p.transform;

            ApplyProjection();
            SnapToTarget();
        }

        protected override void OnDisabled()
        {
            if (Active == this)
                Active = null;
            GameEvents.OnPlayerSpawned -= HandlePlayerSpawned;
            GameEvents.OnPlayerDespawned -= HandlePlayerDespawned;
        }

        private void HandlePlayerSpawned(Player p)
        {
            if (p == null)
            {
                _target = null;
                return;
            }
            _target = p.transform;
            SnapToTarget();
        }

        private void HandlePlayerDespawned(Player p)
        {
            if (_target != null && p != null && _target == p.transform)
                _target = null;
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            Vector3 desired = ComputePosition(_target.position);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, _smoothTime);
            transform.rotation = Quaternion.Euler(_pitch, 0f, 0f);
            ViewRotation = transform.rotation;
        }

        private Vector3 ComputePosition(Vector3 targetPos)
        {
            Vector3 pivot = targetPos + _pivotOffset;
            Quaternion rot = Quaternion.Euler(_pitch, 0f, 0f);
            return pivot - (rot * Vector3.forward) * _distance;
        }

        private void SnapToTarget()
        {
            if (_target == null)
                return;
            transform.position = ComputePosition(_target.position);
            transform.rotation = Quaternion.Euler(_pitch, 0f, 0f);
            ViewRotation = transform.rotation;
            _velocity = Vector3.zero;
        }

        private void ApplyProjection()
        {
            if (_gameCamera == null)
                ResolveChildCameras();
            if (_gameCamera == null)
                return;

            _gameCamera.orthographic = true;
            _gameCamera.orthographicSize = _orthoSize;
            _gameCamera.nearClipPlane = 0.05f;
            _gameCamera.farClipPlane = _distance * 4f;
            _gameCamera.transparencySortMode = TransparencySortMode.Orthographic;

            // 비네트는 바닥 프리팹의 전역 볼륨이 준다. 후처리를 켜야 게임 카메라에 걸린다
            UniversalAdditionalCameraData data = _gameCamera.GetUniversalAdditionalCameraData();
            if (data != null)
                data.renderPostProcessing = true;
            ViewRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void ResolveChildCameras()
        {
            CameraRig rig = GetComponentInChildren<CameraRig>(true);
            if (rig != null)
            {
                _gameCamera = rig.GameCamera;
                _uiCamera = rig.UICamera;
            }

            if (_gameCamera != null)
                return;

            Camera[] cams = GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cams.Length; i++)
            {
                Camera c = cams[i];
                if (c == null)
                    continue;
                if (c.CompareTag("MainCamera"))
                {
                    _gameCamera = c;
                    break;
                }
            }

            if (_gameCamera == null && cams.Length > 0)
                _gameCamera = cams[0];
        }
    }
}
