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
        [SerializeField] private Button _mapPrev;
        [SerializeField] private Button _mapNext;
        [SerializeField] private TMP_Text _mapLabel;

        // 난이도 별 (맵 순서 2개마다 하나, 최대 5) — 배율 숫자는 플레이어에게 의미가 없다
        private static string Stars(int order)
        {
            int n = Mathf.Clamp((order + 1) / 2, 1, 5);
            return "<color=#FFD45A>" + new string('★', n) + "</color><color=#5A5A6A>" + new string('★', 5 - n) + "</color>";
        }
        [SerializeField] private TMP_Text _version;

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
            if (_mapPrev != null)
            {
                _mapPrev.onClick.AddListener(OnClickMapPrev);
            }
            if (_mapNext != null)
            {
                _mapNext.onClick.AddListener(OnClickMapNext);
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
            if (_mapPrev != null)
            {
                _mapPrev.onClick.RemoveAllListeners();
            }
            if (_mapNext != null)
            {
                _mapNext.onClick.RemoveAllListeners();
            }
            _lobby = null;
        }

        private void RefreshAll()
        {
            if (_version != null)
            {
                // 글꼴 출처 표기 (도스고딕 MIT 라이선스 조건)
                _version.text = "v" + Application.version + "  ·  Font: DOSGothic by leedheo (MIT)";
            }
            RefreshHero();
            RefreshMap();
            RefreshRecord();
            RefreshStatus();
        }

        private void RefreshMap()
        {
            if (_mapLabel == null)
            {
                return;
            }
            MapData map = MapRegistry.Current;
            if (map == null)
            {
                _mapLabel.text = Loc.T("Start_NoMap", "맵 없음");
                return;
            }
            _mapLabel.text = SaveData.IsMapUnlocked(map.Order)
                ? Loc.F("Start_MapName", "맵 {0} · {1}", map.Order, map.DisplayName) + System.Environment.NewLine
                  + "<size=15>" + map.Description + "</size>" + System.Environment.NewLine
                  + Loc.F("Start_MapInfo", "<size=15>보스 {0} · 난이도 {1}</size>", map.Boss, Stars(map.Order))
                : Loc.F("Start_MapLocked", "맵 {0} · 잠김", map.Order) + System.Environment.NewLine
                  + Loc.F("Start_MapUnlockHint", "<size=15>맵 {0}을(를) 클리어하면 열립니다</size>", map.Order - 1);
            if (_mapPrev != null)
            {
                _mapPrev.interactable = map.Order > 1;
            }
            if (_mapNext != null)
            {
                _mapNext.interactable = map.Order < MapRegistry.Count;
            }
        }

        private void StepMap(int delta)
        {
            int next = Mathf.Clamp(RunSelection.MapIndex + delta, 1, Mathf.Max(1, MapRegistry.Count));
            if (next == RunSelection.MapIndex)
            {
                return;
            }
            RunSelection.MapIndex = next;
            SaveData.Save();
            RefreshMap();
            RefreshStatus();
        }

        private void OnClickMapPrev()
        {
            StepMap(-1);
        }

        private void OnClickMapNext()
        {
            StepMap(1);
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
                ? Loc.T("Start_FirstRun", "첫 판입니다")
                : Loc.F("Start_RecordBest", "최고 {0}분 {1}초 · 라운드 {2} · 처치 {3}",
                    SaveData.BestSeconds / 60, SaveData.BestSeconds % 60, SaveData.BestRound, SaveData.BestKills)
                  + System.Environment.NewLine
                  + Loc.F("Start_RecordTotals", "클리어 {0}회 / {1}판   ·   사료 {2}  ·  슈퍼 DNA {3}",
                    SaveData.ClearCount, SaveData.PlayCount, SaveData.Kibble, SaveData.SuperDna);
        }

        private void RefreshStatus()
        {
            if (_lobby == null)
            {
                return;
            }
            bool ready = _lobby.IsReady && SaveData.IsMapUnlocked(RunSelection.MapIndex);
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
                return Loc.T("Start_NoStartSkill", "시작 스킬 없음");
            }
            return Loc.T("Start_StartSkillHeader", "시작 스킬") + System.Environment.NewLine
                + Loc.F("Start_StartSkillLine", "· {0}  ({1} 계열)", unique.DisplayName, LobbyText.FamilyName(unique.Family))
                + System.Environment.NewLine + System.Environment.NewLine
                + Loc.T("Start_StartSkillFooter", "<size=17>나머지는 필드에서 줍습니다</size>");
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
