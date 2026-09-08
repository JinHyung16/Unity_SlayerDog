using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Jinhyeong_Character;
using Jinhyeong_Managers;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_GameData.Editor
{

    /// <summary>
    /// 사망 → 결과 패널 → 재시작 → 시작 화면 복귀 → 로비에서 다시 「게임 시작」까지 실측.
    /// 플레이어를 HP 1 로 띄워 빨리 죽게 하고, 각 단계의 상태값을 리포트에 남긴다.
    /// </summary>
    [InitializeOnLoad]
    public static class DeathFlowCheck
    {
        private const string RequestFile = "Temp/deathflow.request";
        private const string ActiveFile = "Temp/deathflow.active";
        private const string ReportFile = "Temp/deathflow.report";
        private const string ShotFile = "Temp/deathflow_result.png";

        private enum Phase { Setup, WaitDeath, ShowResult, Restarted, LobbyStart, Done }

        private static double _nextPoll;
        private static Phase _phase;
        private static float _phaseReal;
        private static float _startReal;
        private static bool _shot;
        private static bool _forcedDeath;
        private static bool _enemyLoadStarted;
        private static int _errorCount;
        private static bool _lobbyStartClicked;
        private static string _firstError;
        private static Damageable _hp;
        /// <summary>적에게 맡겨 보는 시간. 이 안에 못 죽이면 직접 처리한다 — 이 하네스의 관심사는 「죽은 뒤」다.</summary>
        private const float EnemyGraceSeconds = 8f;
        /// <summary>재시작 뒤 로비가 뜰 때까지 주는 시간. 창은 어드레서블로 비동기 로드된다.</summary>
        private const float LobbyLoadSeconds = 4f;
        private static readonly StringBuilder _report = new StringBuilder(1024);

        static DeathFlowCheck()
        {
            // [사고] 요청 감시를 «플레이 중이 아닐 때만» 걸었더니, 도메인 리로드가 다른 하네스의
            // 플레이 종료와 겹친 판에서 감시자가 아예 안 붙었다. 그 뒤로는 요청 파일을 넣어도
            // 아무 일도 일어나지 않고 조용히 시간만 초과했다 — 「하네스가 죽었다」를 알 방법이 없었다.
            //
            // 감시자는 «항상» 건다. 플레이 중이면 WatchRequest 가 스스로 첫 줄에서 빠진다.
            // 조건을 거는 자리를 둘로 나누면, 둘이 어긋난 순간을 아무도 못 잡는다.
            EditorApplication.update -= WatchRequest;
            EditorApplication.update += WatchRequest;

            if (File.Exists(ActiveFile) == false)
                return;

            // 재시작(씬 리로드)은 도메인 리로드를 동반하지 않으므로 static 상태가 유지된다.
            // 단 플레이 진입 직후의 리로드에서는 Setup 부터다.
            EditorApplication.update += Drive;
        }

        private static void WatchRequest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (EditorApplication.timeSinceStartup < _nextPoll)
                return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            if (File.Exists(RequestFile) == false)
                return;

            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            File.Delete(RequestFile);
            File.WriteAllText(ActiveFile, "1");
            EditorApplication.update -= WatchRequest;
            EditorApplication.EnterPlaymode();
        }

        private static void Drive()
        {
            if (EditorApplication.isPlaying == false)
                return;

            float now = Time.realtimeSinceStartup;

            switch (_phase)
            {
                case Phase.Setup:
                    // 적 프리팹이 캐시에 오르기 전에 시작하면 첫 스폰이 통째로 비고 에러만 남는다 (RunLoopCheck 와 같은 대기).
                    AddressableManager am = AddressableManager.Ensure();
                    if (_enemyLoadStarted == false)
                    {
                        _enemyLoadStarted = true;
                        Cysharp.Threading.Tasks.UniTaskExtensions.Forget(am.LoadAsync("obj_enemy"));
                    }
                    if (am.IsLoaded("obj_enemy") == false)
                        return;
                    Setup();
                    break;

                case Phase.WaitDeath:
                    if (Jinhyeong_UI.GameFlow.IsEnded)
                    {
                        _report.AppendLine($"[사망] 경과 {now - _startReal:0.0}s  IsEnded={Jinhyeong_UI.GameFlow.IsEnded}  IsRunning={Jinhyeong_UI.GameFlow.IsRunning}  timeScale={Time.timeScale}");
                        _phase = Phase.ShowResult;
                        _phaseReal = now;
                    }
                    else if (_forcedDeath == false && now - _startReal > EnemyGraceSeconds)
                    {
                        // [사고] 5회 연속 「60초 안에 사망하지 않음」으로 끝나 뒤 단계(결과→재시작→로비)는
                        // 한 번도 실측되지 못했다. 적이 죽이느냐는 밸런스 하네스(runloop mortal)의 몫이다.
                        _forcedDeath = true;
                        bool hit = _hp != null && _hp.TakeDamage(_hp.Hp + 1f, null);
                        _report.AppendLine($"[강제 사망] {EnemyGraceSeconds:0}초 동안 적이 못 죽임 — 직접 처리 {(hit ? "성공" : "실패")}");
                    }
                    else if (now - _startReal > 60f)
                    {
                        Finish("60초 안에 사망하지 않음");
                    }
                    break;

                case Phase.ShowResult:
                    if (now - _phaseReal < 0.6f)
                        return;
                    if (_shot == false)
                    {
                        ScreenCapture.CaptureScreenshot(ShotFile);
                        _shot = true;
                    }
                    if (now - _phaseReal < 1.5f)
                        return;

                    bool clicked = ClickButton("Restart");
                    _report.AppendLine($"[결과패널] 재시작 버튼 {(clicked ? "클릭" : "못 찾음")}");
                    _phase = Phase.Restarted;
                    _phaseReal = now;
                    break;

                case Phase.Restarted:
                    if (now - _phaseReal < LobbyLoadSeconds)
                        return;
                    Jinhyeong_UI.Lobby.LobbyWindow[] lobbies = Object.FindObjectsByType<Jinhyeong_UI.Lobby.LobbyWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    Jinhyeong_UI.Lobby.LobbyWindow ss = lobbies.Length > 0 ? lobbies[0] : null;
                    // 로비가 **하나**여야 한다. Bootstrap 과 sceneLoaded 가 둘 다 열면 겹쳐 보여도 화면으론 모른다.
                    _report.AppendLine($"[재시작 후] IsRunning={Jinhyeong_UI.GameFlow.IsRunning}  IsEnded={Jinhyeong_UI.GameFlow.IsEnded}  timeScale={Time.timeScale}  LobbyWindow={(ss != null ? (ss.IsOpen() ? "열림" : "닫힘") : "NULL")} x{lobbies.Length}  player={(GameEvents.CurrentPlayer != null ? "있음" : "없음")}");
                    if (ss == null || ss.IsOpen() == false)
                    {
                        Finish("재시작 뒤 로비가 없다");
                        return;
                    }
                    _phase = Phase.LobbyStart;
                    _phaseReal = now;
                    _lobbyStartClicked = false;
                    break;

                case Phase.LobbyStart:
                    // 하네스는 평소 로비를 우회한다(StartGame 직접 호출). 진짜 버튼 경로 —
                    // StartRun → WorldSpawner 사전로드 → 스폰 → StartGame → OnGameStarted 로 로비 닫힘 — 는 여기서만 실측된다.
                    Jinhyeong_UI.Lobby.LobbyWindow lobby = Object.FindFirstObjectByType<Jinhyeong_UI.Lobby.LobbyWindow>(FindObjectsInactive.Include);
                    if (_lobbyStartClicked == false)
                    {
                        if (lobby == null || lobby.IsReady == false)
                        {
                            if (now - _phaseReal > LobbyLoadSeconds)
                                Finish("로비가 준비(IsReady)되지 않음");
                            return;
                        }
                        bool hitStart = ClickButton("StartButton");
                        _report.AppendLine($"[로비] 게임 시작 버튼 {(hitStart ? "클릭" : "못 찾음")}");
                        _lobbyStartClicked = true;
                        _phaseReal = now;
                        return;
                    }
                    if (now - _phaseReal < LobbyLoadSeconds)
                        return;
                    int lobbyCount = Object.FindObjectsByType<Jinhyeong_UI.Lobby.LobbyWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
                    _report.AppendLine($"[로비 시작 후] IsRunning={Jinhyeong_UI.GameFlow.IsRunning}  timeScale={Time.timeScale}  LobbyWindow x{lobbyCount}  player={(GameEvents.CurrentPlayer != null ? "있음" : "없음")}");
                    Finish(null);
                    break;
            }
        }

        private static void Setup()
        {
            Application.runInBackground = true;

            GameObject prefab = UnityEngine.AddressableAssets.Addressables
                .LoadAssetAsync<GameObject>("obj_player").WaitForCompletion();
            if (prefab == null)
            {
                Finish("obj_player 로드 실패");
                return;
            }
            GameObject go = Object.Instantiate(prefab, new Vector3(0f, 0.5f, 0f), Quaternion.identity);
            Damageable hp = go.GetComponent<Damageable>();
            if (hp != null)
            {
                hp.MaxHp = 1f;
                hp.Hp = 1f;
            }
            _hp = hp;
            _forcedDeath = false;
            _enemyLoadStarted = false;

            // 이 런에서 난 에러를 리포트에 센다. Editor.log 는 늦게 비워져 「이 런의 에러」를 가려낼 수 없었다.
            _errorCount = 0;
            _firstError = null;
            Application.logMessageReceived -= CountError;
            Application.logMessageReceived += CountError;

            Jinhyeong_UI.GameFlow.StartGame();

            _report.AppendLine("=== 사망 루프 실측 ===");
            _startReal = Time.realtimeSinceStartup;
            _phase = Phase.WaitDeath;
        }

        private static bool ClickButton(string objectName)
        {
            UnityEngine.UI.Button[] buttons = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name != objectName)
                    continue;
                buttons[i].onClick.Invoke();
                return true;
            }
            return false;
        }

        private static void CountError(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception)
                return;
            if (_errorCount == 0)
                _firstError = message;
            _errorCount++;
        }

        private static void Finish(string error)
        {
            EditorApplication.update -= Drive;
            Application.logMessageReceived -= CountError;
            _report.AppendLine(_errorCount == 0
                ? "[에러] 0개"
                : $"[에러] {_errorCount}개 — 첫 번째: {_firstError}");
            if (string.IsNullOrEmpty(error) == false)
                _report.AppendLine($"[중단] {error}");

            File.WriteAllText(ReportFile, _report.ToString(), Encoding.UTF8);
            if (File.Exists(ActiveFile))
                File.Delete(ActiveFile);

            _phase = Phase.Done;
            EditorApplication.isPlaying = false;
        }
    }
}
