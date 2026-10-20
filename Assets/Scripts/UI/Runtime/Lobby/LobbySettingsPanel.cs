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
        private const int VolumeIndexBase = 200;
        private const int LangIndexBase = 300;
        private static readonly string[] Langs = { Loc.Korean, Loc.English };
        private static readonly string[] LangLabels = { "한국어", "English" };

        [SerializeField] private RectTransform _resRow;
        [SerializeField] private RectTransform _moveRow;
        [SerializeField] private RectTransform _volRow;
        [SerializeField] private RectTransform _langRow;
        [SerializeField] private Button _fullscreenButton;
        [SerializeField] private TMP_Text _fullscreenLabel;
        [SerializeField] private Button _wipeButton;
        [SerializeField] private TMP_Text _wipeLabel;

        private readonly List<LabelButtonComponent> _resButtons = new List<LabelButtonComponent>(3);
        private readonly List<LabelButtonComponent> _moveButtons = new List<LabelButtonComponent>(2);
        private readonly List<LabelButtonComponent> _volButtons = new List<LabelButtonComponent>(5);
        private readonly List<LabelButtonComponent> _langButtons = new List<LabelButtonComponent>(2);
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

            for (int i = 0; i < VolumeSettings.Steps.Length; i++)
            {
                LabelButtonComponent b = LabelButtonComponent.PrefabAuto.CreateForUI(_volRow);
                if (b == null)
                {
                    continue;
                }
                b.Open(this, VolumeIndexBase + i, VolumeSettings.LabelOf(i));
                _volButtons.Add(b);
            }

#if UNITY_EDITOR == false
            // 해상도 안내는 에디터 전용 메모 — 빌드에서는 숨긴다
            Transform note = transform.Find("Note");
            if (note != null)
                note.gameObject.SetActive(false);
#endif
            for (int i = 0; i < Langs.Length && _langRow != null; i++)
            {
                LabelButtonComponent b = LabelButtonComponent.PrefabAuto.CreateForUI(_langRow);
                if (b == null)
                {
                    continue;
                }
                b.Open(this, LangIndexBase + i, LangLabels[i]);
                _langButtons.Add(b);
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
            for (int i = 0; i < _volButtons.Count; i++)
            {
                UILoader.Restore(_volButtons[i]);
            }
            _volButtons.Clear();
            for (int i = 0; i < _langButtons.Count; i++)
            {
                UILoader.Restore(_langButtons[i]);
            }
            _langButtons.Clear();
            _wipeArmed = false;
        }

        private void Refresh()
        {
            _wipeArmed = false;
            if (_wipeLabel != null)
            {
                _wipeLabel.text = Loc.T("Settings_Wipe", "데이터 초기화");
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

            for (int i = 0; i < _volButtons.Count; i++)
            {
                _volButtons[i].SetSelected(i == VolumeSettings.Index);
            }

            for (int i = 0; i < _langButtons.Count; i++)
            {
                _langButtons[i].SetSelected(Langs[i] == Loc.Language);
            }

            if (_fullscreenLabel != null)
            {
                _fullscreenLabel.text = DisplaySettings.Fullscreen
                    ? Loc.T("Settings_FullscreenOn", "전체화면: 켬")
                    : Loc.T("Settings_FullscreenOff", "전체화면: 끔");
            }
            if (_fullscreenButton != null && _fullscreenButton.image != null)
            {
                _fullscreenButton.image.color = DisplaySettings.Fullscreen ? UIBuild.ColorCardOn : UIBuild.ColorCard;
            }
        }

        void LabelButtonComponent.IListener.OnClickLabelButton(int index)
        {
            if (index >= LangIndexBase)
            {
                string id = Langs[Mathf.Clamp(index - LangIndexBase, 0, Langs.Length - 1)];
                if (id != Loc.Language)
                {
                    Loc.SetLanguage(id);
                    // 화면 글자를 전부 새 언어로 — 장면을 다시 읽어 로비부터 다시 만든다
                    UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                    return;
                }
            }
            else if (index >= VolumeIndexBase)
            {
                VolumeSettings.Set(index - VolumeIndexBase);
            }
            else if (index >= MoveIndexBase)
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
                    _wipeLabel.text = Loc.T("Settings_WipeConfirm", "정말 지울까요?");
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
