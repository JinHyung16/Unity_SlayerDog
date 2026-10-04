using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;
using Jinhyeong_UI.Framework;

namespace Jinhyeong_UI.Lobby
{

    public class LobbyStartPanel : BasePanel
    {
        public static readonly PrefabAuto<LobbyStartPanel> PrefabAuto = new PrefabAuto<LobbyStartPanel>(PrefabPath);
        public const string PrefabPath = "UI/Lobby/StartPanel";

        [SerializeField] private Image _portrait;
        [SerializeField] private TMP_Text _loadout;
        [SerializeField] private TMP_Text _record;
        [SerializeField] private TMP_Text _status;
        [SerializeField] private Button _startButton;

        private LobbyWindow _lobby;

        protected override void Opening()
        {
            _lobby = Window as LobbyWindow;
            if (_lobby == null)
            {
                return;
            }

            _lobby.OnStatusChanged -= RefreshStatus;
            _lobby.OnStatusChanged += RefreshStatus;

            if (_startButton != null)
            {
                _startButton.onClick.AddListener(OnClickStart);
            }

            RefreshAll();
        }

        protected override void OnReShown()
        {
            RefreshAll();
        }

        protected override void OnClose()
        {
            if (_lobby != null)
            {
                _lobby.OnStatusChanged -= RefreshStatus;
            }
            if (_startButton != null)
            {
                _startButton.onClick.RemoveAllListeners();
            }
            _lobby = null;
        }

        private void RefreshAll()
        {
            RefreshHero();
            RefreshRecord();
            RefreshStatus();
        }

        private void RefreshHero()
        {
            if (_portrait == null)
            {
                return;
            }
            Sprite s = Resources.Load<Sprite>(RunSelection.CurrentHero.PortraitPath);
            _portrait.sprite = s;
            _portrait.enabled = s != null;
        }

        private void RefreshRecord()
        {
            if (_record == null)
            {
                return;
            }
            _record.text = SaveData.PlayCount == 0
                ? "첫 판입니다"
                : $"최고 {SaveData.BestSeconds / 60}분 {SaveData.BestSeconds % 60}초 · 라운드 {SaveData.BestRound} · 처치 {SaveData.BestKills}"
                  + System.Environment.NewLine
                  + $"클리어 {SaveData.ClearCount}회 / {SaveData.PlayCount}판   ·   사료 {SaveData.Kibble}  ·  슈퍼 DNA {SaveData.SuperDna}";
        }

        private void RefreshStatus()
        {
            if (_lobby == null)
            {
                return;
            }
            bool ready = _lobby.IsReady;
            if (_status != null)
            {
                _status.text = _lobby.Status;
            }
            if (_startButton != null && _startButton.interactable != ready)
            {
                _startButton.interactable = ready;
            }
            if (_loadout != null)
            {
                _loadout.text = ready ? BuildLoadoutText() : string.Empty;
            }
        }

        private static string BuildLoadoutText()
        {
            PartData unique = PartRegistry.GetUniqueFor(RunSelection.CharacterIndex);
            if (unique == null)
            {
                return "시작 스킬 없음";
            }
            return "시작 스킬" + System.Environment.NewLine
                + "· " + unique.DisplayName + "  (" + LobbyText.FamilyName(unique.Family) + " 계열)"
                + System.Environment.NewLine + System.Environment.NewLine
                + "<size=17>나머지는 필드에서 줍는다</size>";
        }

        private void OnClickStart()
        {
            if (_lobby == null)
            {
                return;
            }
            if (_startButton != null)
            {
                _startButton.interactable = false;
            }
            _lobby.StartRun();
        }
    }
}
