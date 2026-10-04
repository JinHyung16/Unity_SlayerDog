using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Jinhyeong_UI.Framework;

namespace Jinhyeong_UI.Lobby
{

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
