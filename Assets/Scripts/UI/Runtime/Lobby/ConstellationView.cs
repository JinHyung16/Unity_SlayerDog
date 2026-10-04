using Jinhyeong_Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Jinhyeong_UI.Lobby
{

    public class ConstellationView : BaseBehaviour, IDragHandler, IScrollHandler
    {
        [SerializeField] private RectTransform _content;

        public const float MinZoom = 0.14f;
        public const float MaxZoom = 1.30f;

        public const float DefaultZoom = 0.24f;

        private const float FitMargin = 0.94f;

        public const float LabelZoom = 0.52f;

        private const float ZoomStep = 1.15f;

        private float _zoom = DefaultZoom;
        private Vector2 _pan;

        public float Zoom
        {
            get { return _zoom; }
        }

        public event System.Action OnZoomBandChanged;

        public void Bind(RectTransform content)
        {
            _content = content;
        }

        public void ResetView()
        {
            _zoom = DefaultZoom;
            _pan = Vector2.zero;
            Apply();
        }

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

            float byWidth = r.width * FitMargin / graphExtent.x;
            float byHeight = r.height * FitMargin / graphExtent.y;

            _zoom = Mathf.Clamp(Mathf.Min(byWidth, byHeight), MinZoom, MaxZoom);
            _pan = Vector2.zero;
            Apply();
        }

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
