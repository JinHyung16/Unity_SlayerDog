using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Jinhyeong_Character;
using Jinhyeong_Managers;
using Jinhyeong_SkillSystem;
using Jinhyeong_UI.Framework;

namespace Jinhyeong_UI.Lobby
{

    /// <summary>
    /// 로비 창 — 게임을 켜면 처음 보이는 화면. 4탭(시작 · 캐릭터 · 스킬 트리 · 설정).
    ///
    /// 프리팹은 **골격**만 — Canvas · SafeArea · 배경 · 탭 버튼 4개 · 빈 컨테이너.
    /// 탭 내용(Panel)은 **처음 누를 때** 로드되고, 안 본 탭은 존재하지 않는다 (UIFramework.md 3계층).
    ///
    /// [사고] 한때 프리팹에서 Canvas 를 걷어내고 화면 전부를 코드로 만들었다. 「스킬 장착 슬롯을 없애라」를
    /// 「골격까지 없애라」로 읽은 것이다. 4탭을 한 번에 만들어 SetActive 로 껐다 켰고, 그게 규약이 ✗ 로 적은
    /// 「통짜」를 코드로 옮긴 것이었다. 프리팹은 골격, 내용은 필요할 때.
    ///
    /// 데이터 로드는 <see cref="Opening"/> 에서 시작한다. <c>Awake</c>/<c>Start</c> 는 안 쓴다.
    /// </summary>
    public class LobbyWindow : BaseWindow
    {
        public static readonly WindowKey<LobbyWindow> Key = new WindowKey<LobbyWindow>(PrefabPath);
        public const string PrefabPath = "UI/LobbyWindow";

        public enum ETab
        {
            Start = 0,
            Character = 1,
            Skill = 2,
            Settings = 3,
        }

        /// <summary>좌측 내비 폭(기준 1280 중). PC 가로라 세로 목록이 자연스럽다.</summary>
        public const float NavWidth = 220f;

        public static readonly string[] TabNames = { "시작", "캐릭터", "스킬 트리", "설정" };

        public const string GameTitle = "개많이잡아";
        public const string GameTagline = "12분. 외계인은 많고, 나는 개쌘놈이다.";

        /// <summary>이 창이 쓰는 조각 전부. 열기 전에 올라와야 이후가 동기로 돈다.</summary>
        private static readonly string[] Preload =
        {
            LobbyStartPanel.PrefabPath,
            LobbyCharacterPanel.PrefabPath,
            LobbySkillPanel.PrefabPath,
            LobbySettingsPanel.PrefabPath,
            HeroCardComponent.PrefabPath,
            ConstellationNodeComponent.PrefabPath,
            LabelButtonComponent.PrefabPath,
        };

        [Header("골격 바인딩")]
        [SerializeField] private RectTransform _container;
        [SerializeField] private Button[] _tabButtons = new Button[4];
        [SerializeField] private Image[] _tabImages = new Image[4];

        private readonly BasePanel[] _panels = new BasePanel[4];

        public ETab Current { get; private set; }

        /// <summary>데이터·이펙트 로드가 끝나 시작 가능한 상태.</summary>
        public bool IsReady { get; private set; }

        /// <summary>로딩 진행 문구. 패널이 이벤트로 받아 그린다 — 매 프레임 폴링하지 않는다.</summary>
        public string Status { get; private set; } = string.Empty;

        public event Action OnStatusChanged;

        public override IReadOnlyList<string> PreloadPaths
        {
            get { return Preload; }
        }

        /// <summary>
        /// 연다. 이미 런이 도는 중이면(하네스가 먼저 시작시킨 경우) 열지 않고 스스로 사라진다 —
        /// 그 위에 로비가 덮이면 게임 월드가 한 픽셀도 안 보인다.
        /// </summary>
        public bool Open()
        {
            if (GameFlow.IsRunning)
            {
                Destroy(gameObject);
                return false;
            }

            OpenInternal(() =>
            {
                IsReady = false;
                Status = string.Empty;

                for (int i = 0; i < _tabButtons.Length; i++)
                {
                    if (_tabButtons[i] == null)
                    {
                        continue;
                    }
                    ETab captured = (ETab)i;
                    _tabButtons[i].onClick.AddListener(() => ShowTab(captured));
                }

                GameFlow.OnGameStarted -= HandleGameStarted;
                GameFlow.OnGameStarted += HandleGameStarted;

                // 개수를 아는 목록은 미리 만든다. 첫 열림에서 Instantiate 가 터지면 프레임이 끊긴다.
                HeroCardComponent.PrefabAuto.Preload(Jinhyeong_Common.RunSelection.Heroes.Length);
                LabelButtonComponent.PrefabAuto.Preload(6);

                // 성좌는 별이 200개 넘는다. 탭을 처음 누를 때 한꺼번에 만들면 그 프레임이 통째로 멈춘다.
                ConstellationNodeComponent.PrefabAuto.Preload(
                    Jinhyeong_Constellation.ConstellationManager.Graph.Nodes.Count);
            });
            return true;
        }

        protected override void Opening()
        {
            ShowTab(ETab.Start);
            LoadAsync().Forget();
        }

        protected override void OnClose()
        {
            GameFlow.OnGameStarted -= HandleGameStarted;

            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] != null)
                {
                    _tabButtons[i].onClick.RemoveAllListeners();
                }
            }

            for (int i = 0; i < _panels.Length; i++)
            {
                if (_panels[i] != null)
                {
                    UILoader.Restore(_panels[i]);
                    _panels[i] = null;
                }
            }

            OnStatusChanged = null;
        }

        /// <summary>탭을 연다. 처음이면 패널을 로드하고, 이미 있으면 다시 보이며 값을 새로 읽는다.</summary>
        /// <summary>
        /// 지금 떠 있는 패널. 하네스가 「스킬 트리에서 별을 하나 골라 둔 상태」를 만들 때 쓴다 —
        /// 사람 손 없이 찍으려면 그 패널을 잡을 문이 하나는 있어야 한다.
        /// </summary>
        public BasePanel CurrentPanel
        {
            get { return _panels[(int)Current]; }
        }

        public void ShowTab(ETab tab)
        {
            Current = tab;

            int index = (int)tab;
            if (_panels[index] == null)
            {
                _panels[index] = CreatePanel(tab);
                if (_panels[index] != null)
                {
                    _panels[index].Open(this);
                }
            }
            else
            {
                _panels[index].Show();
            }

            for (int i = 0; i < _panels.Length; i++)
            {
                if (i != index && _panels[i] != null)
                {
                    _panels[i].Hide();
                }
                if (_tabImages[i] != null)
                {
                    _tabImages[i].color = i == index ? UIBuild.ColorCardOn : UIBuild.ColorTabOff;
                }
            }
        }

        private BasePanel CreatePanel(ETab tab)
        {
            switch (tab)
            {
                case ETab.Start: return LobbyStartPanel.PrefabAuto.CreateForUI(_container);
                case ETab.Character: return LobbyCharacterPanel.PrefabAuto.CreateForUI(_container);
                case ETab.Skill: return LobbySkillPanel.PrefabAuto.CreateForUI(_container);
                case ETab.Settings: return LobbySettingsPanel.PrefabAuto.CreateForUI(_container);
                default: return null;
            }
        }

        private async UniTaskVoid LoadAsync()
        {
            if (SkillRegistry.IsLoaded == false)
            {
                SetStatus("스킬 데이터 로딩 중...");
                try
                {
                    await SkillRegistry.LoadAsync();
                }
                catch (Exception e)
                {
                    SetStatus($"스킬 로드 실패: {e.Message}");
                    return;
                }
            }

            if (IsOpen() == false)
            {
                return;
            }

            SetStatus("스킬 이펙트 로딩 중...");
            try
            {
                await SkillRegistry.PreloadVisualsAsync();
            }
            catch (Exception e)
            {
                // 이펙트가 없어도 게임은 돈다. 로딩을 여기서 멈추면 아무것도 못 한다.
                Debug.LogWarning($"[LobbyWindow] 이펙트 사전로드 실패(연출 없이 진행): {e.Message}");
            }

            if (IsOpen() == false)
            {
                return;
            }

            int count = PartRegistry.Count;
            IsReady = true;
            SetStatus(count > 0 ? $"사용 가능 스킬 {count}개" : "사용 가능 스킬 없음 (Resources/GameData 확인)");
        }

        /// <summary>시작 패널의 버튼이 부른다. 준비 전이면 무시.</summary>
        public async void StartRun()
        {
            if (IsReady == false || IsOpen() == false)
            {
                return;
            }

            // 스킬은 파츠가 준다. 빈 로드아웃을 넘겨 「장착할 것 없음」을 분명히 한다.
            SkillLoadout empty = ScriptableObject.CreateInstance<SkillLoadout>();
            empty.Entries = new List<EquippedSkillEntry>(0);

            Player player = GameEvents.CurrentPlayer;
            if (player == null)
            {
                SetStatus("월드 로딩 중...");
                try
                {
                    await WorldSpawner.SpawnPlayerWorldAsync(empty, Vector3.zero);
                }
                catch (Exception e)
                {
                    SetStatus($"스폰 실패: {e.Message}");
                    return;
                }
            }

            // 런이 시작되면 OnGameStarted 로 이 창이 닫힌다. 여기서 직접 닫지 않는다 —
            // 하네스 · 다른 진입점이 시작시켜도 같은 경로로 닫혀야 한다.
            GameFlow.StartGame();
        }

        private void HandleGameStarted()
        {
            ForcedClose();
        }

        private void SetStatus(string text)
        {
            Status = text;
            if (OnStatusChanged != null)
            {
                OnStatusChanged.Invoke();
            }
        }
    }
}
