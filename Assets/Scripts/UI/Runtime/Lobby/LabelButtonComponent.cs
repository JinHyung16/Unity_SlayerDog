using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Jinhyeong_UI.Framework;

namespace Jinhyeong_UI.Lobby
{

    /// <summary>
    /// 글자 하나짜리 버튼. 해상도 프리셋 · 강화 칸처럼 「같은 모양이 N개」인 자리에 쓴다.
    /// 어느 칸이 눌렸는지는 index 로만 말한다 — 의미는 부모가 안다.
    /// </summary>
    public class LabelButtonComponent : BaseComponent
    {
        public static readonly PrefabAuto<LabelButtonComponent> PrefabAuto = new PrefabAuto<LabelButtonComponent>(PrefabPath);
        public const string PrefabPath = "UI/Lobby/LabelButton";

        public interface IListener
        {
            void OnClickLabelButton(int index);
        }

        [SerializeField] private Image _bg;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Button _button;

        private IListener _listener;
        private int _index;
        private Color _baseColor = UIBuild.ColorCard;

        public void Open(IListener listener, int index, string text)
        {
            Clear();
            _listener = listener;
            _index = index;
            _baseColor = UIBuild.ColorCard;
            SetText(text);
            SetSelected(false);
            if (_button != null)
            {
                _button.onClick.AddListener(OnClick);
            }
        }

        public void SetText(string text)
        {
            if (_label != null)
            {
                _label.text = text ?? string.Empty;
            }
        }

        /// <summary>선택 안 됐을 때의 바탕색. 강화 칸처럼 기본색이 다른 자리용.</summary>
        public void SetColor(Color baseColor)
        {
            _baseColor = baseColor;
            if (_bg != null)
            {
                _bg.color = baseColor;
            }
        }

        public void SetSelected(bool on)
        {
            if (_bg != null)
            {
                _bg.color = on ? UIBuild.ColorCardOn : _baseColor;
            }
        }

        public override void Clear()
        {
            _listener = null;
            _index = 0;
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
            }
            base.Clear();
        }

        private void OnClick()
        {
            if (_listener != null)
            {
                _listener.OnClickLabelButton(_index);
            }
        }
    }
}
