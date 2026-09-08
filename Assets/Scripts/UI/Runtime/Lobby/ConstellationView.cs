using Jinhyeong_Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Jinhyeong_UI.Lobby
{

    /// <summary>
    /// 성좌를 «보는 창» — 끌어서 옮기고 휠로 확대한다.
    ///
    /// 노드 220개를 매번 다시 배치하지 않는다. 좌표 변환은 아핀 변환 하나라
    /// <c>Content</c> 의 스케일과 위치만 바꾸면 전 노드가 따라온다 —
    /// 그래서 줌·팬이 노드 수와 무관하게 일정한 비용이다.
    ///
    ///   화면 = (그래프 + 이동) × 배율
    ///
    /// [사고] 마우스 자리를 고정하지 않고 그냥 배율만 키우면 «화면 중앙»이 기준이 되어,
    /// 보고 있던 갈래가 확대할 때마다 화면 밖으로 도망간다. 초점 보정 한 줄이 그걸 막는다.
    /// </summary>
    public class ConstellationView : BaseBehaviour, IDragHandler, IScrollHandler
    {
        [SerializeField] private RectTransform _content;

        /// <summary>배율 한계. 최소는 「성좌 전체가 보이는」 값, 최대는 「이름이 읽히는」 값이다.</summary>
        public const float MinZoom = 0.14f;
        public const float MaxZoom = 1.30f;

        /// <summary>창 크기를 못 읽었을 때의 대비값. 정상 경로는 <see cref="FitTo"/> 가 계산한다.</summary>
        public const float DefaultZoom = 0.24f;

        /// <summary>창 가장자리에 남기는 여백 비율. 0.94 면 성좌가 테두리에 닿지 않는다.</summary>
        private const float FitMargin = 0.94f;

        /// <summary>이 배율보다 작으면 이름을 그리지 않는다 — 읽히지도 않으면서 서로 겹치기만 한다.</summary>
        public const float LabelZoom = 0.52f;

        /// <summary>휠 한 칸의 배율. 곱셈이라야 어느 배율에서도 «같은 정도»로 확대된다.</summary>
        private const float ZoomStep = 1.15f;

        private float _zoom = DefaultZoom;
        private Vector2 _pan;

        public float Zoom
        {
            get { return _zoom; }
        }

        /// <summary>배율이 이름 문턱을 넘나들었다. 패널이 이름을 켜고 끄는 신호.</summary>
        public event System.Action OnZoomBandChanged;

        public void Bind(RectTransform content)
        {
            _content = content;
        }

        /// <summary>처음 상태로. 창을 닫았다 열면 보던 자리가 아니라 코어에서 시작한다.</summary>
        public void ResetView()
        {
            _zoom = DefaultZoom;
            _pan = Vector2.zero;
            Apply();
        }

        /// <summary>
        /// 성좌 «전체»가 창에 들어오는 배율로 맞춘다.
        ///
        /// [사고] 배율을 상수로 박아 뒀더니 위아래 갈래의 키스톤이 창 밖으로 잘려 나갔다.
        /// 링 반지름을 한 번 조정할 때마다 그 상수를 손으로 다시 맞춰야 했고, 한 번은 잊었다.
        /// 창 크기와 성좌 반경에서 «계산»하면 둘 중 무엇이 바뀌어도 따라온다.
        ///
        /// 창의 rect 는 레이아웃이 돈 뒤에야 실제 크기를 갖는다. 만들자마자 읽으면 0 이 나와
        /// 배율이 한계값으로 튄다 — 그래서 캔버스를 먼저 한 번 돌린다.
        /// </summary>
        public void FitTo(Vector2 graphExtent)
        {
            RectTransform self = transform as RectTransform;
            if (self == null || graphExtent.x <= 0.01f || graphExtent.y <= 0.01f)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            Rect r = self.rect;
            if (r.width <= 1f || r.height <= 1f)
            {
                _zoom = DefaultZoom;
                _pan = Vector2.zero;
                Apply();
                return;
            }

            // 가로·세로를 «각각» 재서 빡빡한 쪽에 맞춘다. 반경 하나로 맞추면 눌린 성좌에서 가로가 남는다.
            float byWidth = r.width * FitMargin / graphExtent.x;
            float byHeight = r.height * FitMargin / graphExtent.y;

            _zoom = Mathf.Clamp(Mathf.Min(byWidth, byHeight), MinZoom, MaxZoom);
            _pan = Vector2.zero;
            Apply();
        }

        /// <summary>
        /// 그래프 좌표 → 창 안의 «화면» 좌표(창 가운데가 0). 별 위에 설명 패널을 띄울 때 쓴다.
        ///
        ///   화면 = (그래프 + 이동) × 배율
        ///
        /// Content 를 통째로 옮기고 키우는 것과 «같은 식»을 쓴다 — 두 곳에서 각자 계산하면
        /// 한쪽만 고쳤을 때 패널이 별에서 떨어져 뜬다.
        /// </summary>
        public Vector2 GraphToLocal(Vector2 graph)
        {
            return (graph + _pan) * _zoom;
        }

        private void Apply()
        {
            if (_content == null)
            {
                return;
            }
            _content.localScale = new Vector3(_zoom, _zoom, 1f);
            _content.anchoredPosition = _pan * _zoom;
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (_content == null)
            {
                return;
            }
            // 화면에서 끈 거리를 그래프 유닛으로 되돌린다. 배율로 나누지 않으면
            // 확대했을 때 손보다 성좌가 빨리 움직인다.
            _pan += eventData.delta / _zoom;
            Apply();
        }

        void IScrollHandler.OnScroll(PointerEventData eventData)
        {
            if (_content == null)
            {
                return;
            }

            float before = _zoom;
            float factor = eventData.scrollDelta.y > 0f ? ZoomStep : 1f / ZoomStep;
            _zoom = Mathf.Clamp(_zoom * factor, MinZoom, MaxZoom);
            if (Mathf.Approximately(before, _zoom))
            {
                return;
            }

            // 커서 밑에 있던 그래프 좌표가 커서 밑에 그대로 남게 이동값을 다시 푼다.
            Vector2 local;
            RectTransform self = transform as RectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    self, eventData.position, eventData.pressEventCamera, out local))
            {
                Vector2 graphUnderCursor = local / before - _pan;
                _pan = local / _zoom - graphUnderCursor;
            }
            Apply();

            bool wasLabel = before >= LabelZoom;
            bool nowLabel = _zoom >= LabelZoom;
            if (wasLabel != nowLabel && OnZoomBandChanged != null)
            {
                OnZoomBandChanged.Invoke();
            }
        }
    }
}
