using TMPro;
using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_UI.Framework
{

    // 프리팹에 구워진 고정 글자 (UIPrefabBuilder) 를 켜질 때 지금 언어로 바꾼다.
    // 언어를 바꾸면 장면을 다시 읽으므로 OnEnable 한 번이면 된다
    [RequireComponent(typeof(TMP_Text))]
    public class LocLabel : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private string _korean;

        public string Key { get { return _key; } }

        // 원문 표 추출기가 LocLabel.Attach(<글자>, "키", "한국어") 꼴을 읽는다 — 키와 한국어는 글자 그대로 쓴다
        public static LocLabel Attach(TMP_Text text, string key, string korean)
        {
            if (text == null)
                return null;
            LocLabel label = text.GetComponent<LocLabel>();
            if (label == null)
                label = text.gameObject.AddComponent<LocLabel>();
            label._key = key;
            label._korean = korean;
            text.text = korean;
            return label;
        }

        private void OnEnable()
        {
            TMP_Text text = GetComponent<TMP_Text>();
            if (text == null)
                return;
            text.text = Loc.T(_key, _korean);
        }
    }
}
