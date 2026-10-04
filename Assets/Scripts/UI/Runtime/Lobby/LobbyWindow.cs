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

        public const float NavWidth = 220f;

        public static readonly string[] TabNames = { "시작", "캐릭터", "스킬 트리", "설정" };

        public const string GameTitle = "개많이잡아";
        public const string GameTagline = "12분. 파스텔 군단은 많고, 우리는 밴드다.";

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

        public bool IsReady { get; private set; }

        public string Status { get; private set; } = string.Empty;

        public event Action OnStatusChanged;

        public override IReadOnlyList<string> PreloadPaths
        {
            get { return Preload; }
        }

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

                HeroCardComponent.PrefabAuto.Preload(Jinhyeong_Common.RunSelection.Heroes.Length);
                LabelButtonComponent.PrefabAuto.Preload(6);

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

        // 씬에 미리 배치된 패널(메인 화면이 플레이 전에도 보이게 — 10-04)이 Container 밑에 있으면 그것을 쓴다.
        // 경로를 달아 두면 닫을 때 다른 패널과 똑같이 풀로 돌아간다
        private T TakePlaced<T>(PrefabAuto<T> auto) where T : BasePanel
        {
            if (_container == null)
            {
                return null;
            }
            T[] found = _container.GetComponentsInChildren<T>(true);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null && found[i].IsOpen == false && string.IsNullOrEmpty(found[i].Path))
                {
                    found[i].SetPath(auto.Path);
                    found[i].gameObject.SetActive(true);
                    return found[i];
                }
            }
            return null;
        }

        private BasePanel CreatePanel(ETab tab)
        {
            switch (tab)
            {
                case ETab.Start: return TakePlaced(LobbyStartPanel.PrefabAuto) ?? LobbyStartPanel.PrefabAuto.CreateForUI(_container);
                case ETab.Character: return TakePlaced(LobbyCharacterPanel.PrefabAuto) ?? LobbyCharacterPanel.PrefabAuto.CreateForUI(_container);
                case ETab.Skill: return TakePlaced(LobbySkillPanel.PrefabAuto) ?? LobbySkillPanel.PrefabAuto.CreateForUI(_container);
                case ETab.Settings: return TakePlaced(LobbySettingsPanel.PrefabAuto) ?? LobbySettingsPanel.PrefabAuto.CreateForUI(_container);
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

        public async void StartRun()
        {
            if (IsReady == false || IsOpen() == false)
            {
                return;
            }

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
