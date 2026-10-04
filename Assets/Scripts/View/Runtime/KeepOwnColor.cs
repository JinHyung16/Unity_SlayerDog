using UnityEngine;

namespace Jinhyeong_View
{
    // 데이터 틴트를 얼마나 받을지. 0 = 제 색 그대로(물건형 이펙트) · 0.25 = 흰 심지에 계열 색이 살짝(기운형 이펙트).
    // 이 컴포넌트가 없는 렌더러는 틴트를 전부 받는다 (번짐 층).
    public class KeepOwnColor : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float _tintWeight;

        public float TintWeight
        {
            get { return _tintWeight; }
        }

        public static Color Resolve(SpriteRenderer renderer, Color tint)
        {
            KeepOwnColor keep = renderer.GetComponent<KeepOwnColor>();
            if (keep == null)
                return tint;
            return Color.Lerp(Color.white, tint, keep._tintWeight);
        }
    }
}
