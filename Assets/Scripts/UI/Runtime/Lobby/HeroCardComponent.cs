using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Jinhyeong_UI.Framework;

namespace Jinhyeong_UI.Lobby
{

    public class HeroCardComponent : BaseComponent
    {
        public static readonly PrefabAuto<HeroCardComponent> PrefabAuto = new PrefabAuto<HeroCardComponent>(PrefabPath);
        public const string PrefabPath = "UI/Lobby/HeroCard";

        public interface IListener
        {
            void OnClickHero(int index);
        }

        [SerializeField] private Image _bg;
        [SerializeField] private Image _face;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Button _button;

        public int Index { get; private set; }
        public bool Locked { get; private set; }

        private IListener _listener;
        private string _name = string.Empty;
        private string _detail = string.Empty;

        public void Open(IListener listener, int index, string heroName, Sprite face)
        {
            Clear();

            _listener = listener;
            Index = index;
            _name = heroName ?? string.Empty;

            if (_face != null)
            {
                _face.sprite = face;
                _face.enabled = face != null;
            }
            if (_button != null)
            {
                _button.onClick.AddListener(OnClick);
            }
            SetSelected(false);
            RefreshLabel();
        }

        public void SetSelected(bool on)
        {
            if (_bg != null)
            {
                _bg.color = Locked ? UIBuild.ColorLocked : (on ? UIBuild.ColorCardOn : UIBuild.ColorCard);
            }
        }

        // 잠긴 카드는 초상화를 어둡게, 눌러도 고르지 않는다
        public void SetLocked(bool locked)
        {
            Locked = locked;
            if (_face != null)
            {
                _face.color = locked ? new Color(0.15f, 0.15f, 0.2f, 1f) : Color.white;
            }
            if (_label != null)
            {
                _label.color = locked ? UIBuild.ColorTextLocked : UIBuild.ColorText;
            }
        }

        public void SetDetail(string detail)
        {
            _detail = detail ?? string.Empty;
            RefreshLabel();
        }

        public override void Clear()
        {
            _listener = null;
            _name = string.Empty;
            _detail = string.Empty;
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
            }
            base.Clear();
        }

        private void RefreshLabel()
        {
            if (_label != null)
            {
                _label.text = _name + System.Environment.NewLine + _detail;
            }
        }

        private void OnClick()
        {
            if (_listener != null)
            {
                _listener.OnClickHero(Index);
            }
        }
    }
}
