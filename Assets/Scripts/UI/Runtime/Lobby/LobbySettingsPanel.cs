using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Jinhyeong_Common;
using Jinhyeong_Input;
using Jinhyeong_UI.Framework;

namespace Jinhyeong_UI.Lobby
{

    /// <summary>
    /// 설정 — PC 라서 해상도와 이동키를 고를 수 있어야 한다. 둘 다 프리셋만 준다 —
    /// 해상도는 임의 입력을 받으면 이상한 화면비가 들어와 UI 가 깨지고,
    /// 이동키는 임의 리바인딩을 받으면 중복 키 검사를 전부 해야 하는데 그걸 빠뜨리면 「움직일 수 없는 저장」이 된다.
    /// 프리셋 버튼은 풀에서, 전체화면·초기화는 정적 골격.
    /// </summary>
    public class LobbySettingsPanel : BasePanel, LabelButtonComponent.IListener
    {
        public static readonly PrefabAuto<LobbySettingsPanel> PrefabAuto = new PrefabAuto<LobbySettingsPanel>(PrefabPath);
        public const string PrefabPath = "UI/Lobby/SettingsPanel";

        /// <summary>
        /// 이동키 버튼의 index 오프셋. 한 패널에서 두 줄이 같은 <see cref="LabelButtonComponent.IListener"/> 를 쓰므로
        /// 번호가 겹치면 해상도를 눌렀는데 이동키가 바뀐다. 줄이 늘면 오프셋을 하나 더 뗀다.
        /// </summary>
        private const int MoveIndexBase = 100;

        [SerializeField] private RectTransform _resRow;
        [SerializeField] private RectTransform _moveRow;
        [SerializeField] private Button _fullscreenButton;
        [SerializeField] private TMP_Text _fullscreenLabel;
        [SerializeField] private Button _wipeButton;
        [SerializeField] private TMP_Text _wipeLabel;

        private readonly List<LabelButtonComponent> _resButtons = new List<LabelButtonComponent>(3);
        private readonly List<LabelButtonComponent> _moveButtons = new List<LabelButtonComponent>(2);
        private bool _wipeArmed;

        protected override void Opening()
        {
            DisplaySettings.Preset[] presets = DisplaySettings.Presets;
            for (int i = 0; i < presets.Length; i++)
            {
                LabelButtonComponent b = LabelButtonComponent.PrefabAuto.CreateForUI(_resRow);
                if (b == null)
                {
                    continue;
                }
                b.Open(this, i, presets[i].Label);
                _resButtons.Add(b);
            }

            InputSettings.Scheme[] schemes = InputSettings.Schemes;
            for (int i = 0; i < schemes.Length; i++)
            {
                LabelButtonComponent b = LabelButtonComponent.PrefabAuto.CreateForUI(_moveRow);
                if (b == null)
                {
                    continue;
                }
                b.Open(this, MoveIndexBase + i, schemes[i].Label);
                _moveButtons.Add(b);
            }

            if (_fullscreenButton != null)
            {
                _fullscreenButton.onClick.AddListener(OnClickFullscreen);
            }
            if (_wipeButton != null)
            {
                _wipeButton.onClick.AddListener(OnClickWipe);
            }

            Refresh();
        }

        /// <summary>탭에 들어올 때마다 초기화 버튼은 안전한 상태로 되돌린다.</summary>
        protected override void OnReShown()
        {
            Refresh();
        }

        protected override void OnClose()
        {
            if (_fullscreenButton != null)
            {
                _fullscreenButton.onClick.RemoveAllListeners();
            }
            if (_wipeButton != null)
            {
                _wipeButton.onClick.RemoveAllListeners();
            }
            for (int i = 0; i < _resButtons.Count; i++)
            {
                UILoader.Restore(_resButtons[i]);
            }
            _resButtons.Clear();
            for (int i = 0; i < _moveButtons.Count; i++)
            {
                UILoader.Restore(_moveButtons[i]);
            }
            _moveButtons.Clear();
            _wipeArmed = false;
        }

        private void Refresh()
        {
            _wipeArmed = false;
            if (_wipeLabel != null)
            {
                _wipeLabel.text = "데이터 초기화";
            }
            if (_wipeButton != null && _wipeButton.image != null)
            {
                _wipeButton.image.color = UIBuild.ColorDanger;
            }

            for (int i = 0; i < _resButtons.Count; i++)
            {
                _resButtons[i].SetSelected(i == DisplaySettings.PresetIndex);
            }

            for (int i = 0; i < _moveButtons.Count; i++)
            {
                _moveButtons[i].SetSelected(i == (int)InputSettings.MoveScheme);
            }

            if (_fullscreenLabel != null)
            {
                _fullscreenLabel.text = DisplaySettings.Fullscreen ? "전체화면: 켬" : "전체화면: 끔";
            }
            if (_fullscreenButton != null && _fullscreenButton.image != null)
            {
                _fullscreenButton.image.color = DisplaySettings.Fullscreen ? UIBuild.ColorCardOn : UIBuild.ColorCard;
            }
        }

        void LabelButtonComponent.IListener.OnClickLabelButton(int index)
        {
            if (index >= MoveIndexBase)
            {
                InputSettings.Set((InputSettings.EMoveScheme)(index - MoveIndexBase));
            }
            else
            {
                DisplaySettings.Set(index, DisplaySettings.Fullscreen);
            }
            Refresh();
        }

        private void OnClickFullscreen()
        {
            DisplaySettings.Set(DisplaySettings.PresetIndex, DisplaySettings.Fullscreen == false);
            Refresh();
        }

        /// <summary>
        /// 되돌릴 수 없는 조작이라 **두 번 눌러야** 지워진다. 한 번 누르면 「정말?」로 바뀌고,
        /// 다른 탭에 갔다 오면 원래대로 돌아간다. 개발 중에 자주 쓰지만, 자주 쓴다고 실수로 눌러도 되는 건 아니다.
        /// </summary>
        private void OnClickWipe()
        {
            if (_wipeArmed == false)
            {
                _wipeArmed = true;
                if (_wipeLabel != null)
                {
                    _wipeLabel.text = "정말 지운다";
                }
                if (_wipeButton != null && _wipeButton.image != null)
                {
                    _wipeButton.image.color = UIBuild.ColorDangerArmed;
                }
                return;
            }

            SaveData.Wipe();
            // 다른 탭은 다시 들어올 때(OnReShown) 새 값을 읽는다. 여기서 남의 패널을 건드리지 않는다.
            Refresh();
        }
    }
}
