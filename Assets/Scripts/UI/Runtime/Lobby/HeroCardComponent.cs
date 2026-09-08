using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Jinhyeong_UI.Framework;

namespace Jinhyeong_UI.Lobby
{

    /// <summary>
    /// 슈퍼독 카드 한 장. 부모 창의 타입을 모른다 — <see cref="IListener"/> 로만 위로 말한다.
    /// </summary>
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
                _bg.color = on ? UIBuild.ColorCardOn : UIBuild.ColorCard;
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
