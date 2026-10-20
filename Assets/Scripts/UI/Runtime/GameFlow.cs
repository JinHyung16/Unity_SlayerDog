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

            Jinhyeong_Managers.GameEvents.OnPlayerSpawned -= HookPlayer;
            Jinhyeong_Managers.GameEvents.OnPlayerSpawned += HookPlayer;
            Jinhyeong_Managers.GameEvents.OnPlayerDespawned -= UnhookPlayer;
            Jinhyeong_Managers.GameEvents.OnPlayerDespawned += UnhookPlayer;

            Jinhyeong_Common.DisplaySettings.LoadAndApply();
            Jinhyeong_Common.VolumeSettings.LoadAndApply();
            Jinhyeong_Common.SteamService.Init();

            Jinhyeong_Input.InputSettings.Load();

            Jinhyeong_Common.Loc.Boot();
            Jinhyeong_Common.SaveData.Load();

            Jinhyeong_Managers.SfxManager.Ensure();

            EnsureEventSystem();

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
            Jinhyeong_Managers.MusicManager.Play("lobby");
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
            IsEnded = false;
            IsCleared = false;
            IsRunning = true;
            Time.timeScale = 1f;

            Jinhyeong_SkillSystem.MapData map = Jinhyeong_SkillSystem.MapRegistry.Current;
            Jinhyeong_Managers.MusicManager.Play(map != null ? map.Name : "snow_sakura");

            if (Jinhyeong_AI.WaveDirector.Instance != null)
                Jinhyeong_AI.WaveDirector.Instance.BeginRun();

            if (OnGameStarted != null)
                OnGameStarted.Invoke();
        }

        public static void ResetForReload()
        {
            IsRunning = false;
            IsEnded = false;
            IsCleared = false;
            Time.timeScale = 1f;
        }

        public static void ClearGame()
        {
            if (IsRunning == false || IsEnded)
                return;
            IsCleared = true;
            Jinhyeong_Managers.SfxManager.Play(Jinhyeong_Managers.ESfx.Clear);
            EndGame();
        }

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
