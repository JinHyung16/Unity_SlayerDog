using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Jinhyeong_Common;
using Jinhyeong_Input;
using Jinhyeong_UI.Framework;

namespace Jinhyeong_UI.Lobby
{

    public class LobbySettingsPanel : BasePanel, LabelButtonComponent.IListener
    {
        public static readonly PrefabAuto<LobbySettingsPanel> PrefabAuto = new PrefabAuto<LobbySettingsPanel>(PrefabPath);
        public const string PrefabPath = "UI/Lobby/SettingsPanel";

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
            Refresh();
        }
    }
}
