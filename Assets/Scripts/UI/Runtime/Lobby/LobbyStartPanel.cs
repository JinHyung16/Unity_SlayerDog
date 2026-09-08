using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;
using Jinhyeong_UI.Framework;

namespace Jinhyeong_UI.Lobby
{

    /// <summary>
    /// 시작 탭 — 첫 화면. 왼쪽 주인공 · 오른쪽 제목/한 줄/시작 스킬/기록/상태/시작 버튼.
    /// 정적 배치는 프리팹, 동적 값(초상화 · 시작 스킬 · 기록 · 상태)만 여기서 채운다.
    ///
    /// 상태는 창의 이벤트로 받는다. 매 프레임 폴링하면 값이 안 바뀌어도 캔버스가 리빌드된다.
    /// </summary>
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

        /// <summary>저장된 기록. 이게 화면에 없으면 「저장이 되는지」를 확인할 방법이 없다.</summary>
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

        /// <summary>
        /// 이번 판을 무엇으로 시작하나. 시작 스킬은 **캐릭터의 고유 파츠 하나**뿐이고 나머지는 필드에서 줍는다.
        /// 고를 것이 없다는 사실 자체가 이 게임의 규칙이라, 화면도 그렇게 말해야 한다.
        /// </summary>
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
