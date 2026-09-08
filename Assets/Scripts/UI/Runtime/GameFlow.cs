using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Jinhyeong_UI.Framework;
using Jinhyeong_UI.Lobby;

namespace Jinhyeong_UI
{

    public static class GameFlow
    {
        public static bool IsRunning { get; private set; }
        public static bool IsEnded { get; private set; }

        /// <summary>이번 런이 클리어로 끝났는가. 결과 화면이 문구를 가른다.</summary>
        public static bool IsCleared { get; private set; }
        public static event Action OnGameStarted;
        public static event Action OnGameEnded;

        private static Jinhyeong_SkillSystem.Damageable _playerHp;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            IsRunning = false;
            IsEnded = false;
            OnGameStarted = null;
            OnGameEnded = null;
            Time.timeScale = 0f;

            // 플레이어가 스폰되면 사망을 구독해 런을 닫는다. 씬 리로드마다 다시 건다.
            Jinhyeong_Managers.GameEvents.OnPlayerSpawned -= HookPlayer;
            Jinhyeong_Managers.GameEvents.OnPlayerSpawned += HookPlayer;
            Jinhyeong_Managers.GameEvents.OnPlayerDespawned -= UnhookPlayer;
            Jinhyeong_Managers.GameEvents.OnPlayerDespawned += UnhookPlayer;

            // PC 게임이라 해상도는 사용자가 정한다. 저장된 값을 켤 때 한 번 적용한다.
            Jinhyeong_Common.DisplaySettings.LoadAndApply();

            // 이동키 배치도 같은 이유로 사용자 몫이다. 플레이어가 서기 전에 읽어 둔다.
            Jinhyeong_Input.InputSettings.Load();

            // 기록·재화·해금. 캐릭터 선택도 여기서 복원된다.
            Jinhyeong_Common.SaveData.Load();

            // 효과음. 소스를 미리 여러 개 만들어 두므로 한 번만 세우면 된다.
            Jinhyeong_Managers.SfxManager.Ensure();

            EnsureEventSystem();

            // 첫 화면은 **키로 연다.** 씬에 박아 두지 않는다 — 창의 수명은 Management 가 쥔다.
            // 씬 리로드(Restart)에서는 이 Bootstrap 이 다시 안 돌므로 sceneLoaded 가 같은 길을 탄다.
            //
            // [사고] 한때 로비 프리팹을 씬 인스턴스로 두고 여기서 FindFirstObjectByType 으로 찾았다.
            // 규약(UIFramework.md)은 「Window 는 키로 로드」인데 첫 화면만 예외로 두면 예외가 규칙이 된다.
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            OpenLobby();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            IsRunning = false;
            IsEnded = false;
            IsCleared = false;
            Time.timeScale = 0f;
            EnsureEventSystem();
            OpenLobby();
        }

        private static void OpenLobby()
        {
            OpenLobbyAsync().Forget();
        }

        private static async UniTaskVoid OpenLobbyAsync()
        {
            LobbyWindow lobby = await UIManagement.Ensure().OpenAsync(LobbyWindow.Key);
            if (lobby == null)
            {
                Debug.LogWarning("[GameFlow] 로비 창을 열지 못해 시작 게이트를 건너뜀. 게임이 바로 진행됩니다.");
                StartGame();
                return;
            }
            lobby.Open();
        }

        public static void StartGame()
        {
            if (IsRunning)
                return;
            // Bootstrap 은 앱 시작 때 한 번만 돈다(씬 리로드에선 안 돈다). 런 상태는 여기서 리셋한다.
            IsEnded = false;
            IsCleared = false;
            IsRunning = true;
            Time.timeScale = 1f;

            if (Jinhyeong_AI.WaveDirector.Instance != null)
                Jinhyeong_AI.WaveDirector.Instance.BeginRun();

            if (OnGameStarted != null)
                OnGameStarted.Invoke();
        }

        /// <summary>씬 리로드 직전에 런 상태를 지운다. Bootstrap 이 다시 돌지 않기 때문에 필요하다.</summary>
        public static void ResetForReload()
        {
            IsRunning = false;
            IsEnded = false;
            IsCleared = false;
            Time.timeScale = 1f;
        }

        /// <summary>
        /// 12라운드 보스를 잡았다. 종료 경로는 사망과 같고 결과 문구만 갈린다 —
        /// 정리 로직을 두 벌 만들면 한쪽만 고치는 사고가 난다.
        /// </summary>
        public static void ClearGame()
        {
            if (IsRunning == false || IsEnded)
                return;
            IsCleared = true;
            Jinhyeong_Managers.SfxManager.Play(Jinhyeong_Managers.ESfx.Clear);
            EndGame();
        }

        /// <summary>런 종료. 진입점은 둘 — 플레이어 사망, 그리고 보스 처치(ClearGame).</summary>
        public static void EndGame()
        {
            if (IsRunning == false || IsEnded)
                return;

            IsEnded = true;
            IsRunning = false;
            Time.timeScale = 0f;

            RecordRun();

            if (Jinhyeong_AI.WaveDirector.Instance != null)
                Jinhyeong_AI.WaveDirector.Instance.StopRun();

            if (OnGameEnded != null)
                OnGameEnded.Invoke();
        }

        /// <summary>
        /// 이번 런의 성과를 저장한다.
        ///
        /// 죽어도 남긴다 — 아무것도 안 남으면 진 판이 통째로 버려진 시간이 된다.
        /// 종료 경로가 사망·클리어 둘이라, 둘 다 지나는 EndGame 에서 한 번만 부른다.
        /// </summary>
        private static void RecordRun()
        {
            Jinhyeong_AI.WaveDirector wave = Jinhyeong_AI.WaveDirector.Instance;
            Jinhyeong_AI.EnemySpawner spawner = Jinhyeong_AI.EnemySpawner.Instance;

            float elapsed = wave != null ? wave.Elapsed : 0f;
            int round = Jinhyeong_SkillSystem.RoundRegistry.RoundOf(elapsed);
            int kills = spawner != null ? spawner.KillCount : 0;

            Jinhyeong_Common.SaveData.RecordRun(
                Mathf.FloorToInt(elapsed), round, kills, IsCleared);
        }

        private static void HookPlayer(Jinhyeong_Character.Player p)
        {
            UnhookPlayer(p);
            _playerHp = p != null ? p.GetComponent<Jinhyeong_SkillSystem.Damageable>() : null;
            if (_playerHp != null)
                _playerHp.OnDied += HandlePlayerDied;
        }

        private static void UnhookPlayer(Jinhyeong_Character.Player p)
        {
            if (_playerHp != null)
                _playerHp.OnDied -= HandlePlayerDied;
            _playerHp = null;
        }

        private static void HandlePlayerDied(Jinhyeong_SkillSystem.Damageable victim, Jinhyeong_SkillSystem.SkillObject source)
        {
            Jinhyeong_Managers.SfxManager.Play(Jinhyeong_Managers.ESfx.Dead);
            EndGame();
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
