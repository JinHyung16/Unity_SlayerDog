using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Jinhyeong_AI;
using Jinhyeong_Character;
using Jinhyeong_Managers;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_GameData.Editor
{

    [InitializeOnLoad]
    public static class RunLoopCheck
    {
        private const string RequestFile = "Temp/runloop.request";
        private const string ActiveFile = "Temp/runloop.active";
        private const string ReportFile = "Temp/runloop.report";
        // 판마다 한 줄 덧붙는 결과 표 (B-1 다중 시드 일괄). 지우는 건 일괄 스크립트가 한다
        private const string ResultsFile = "Temp/runloop_results.tsv";
        private const string ResultsHeader = "hero\tmap\tseed\tcleared\tsurvived\tkills\tkills_r5\tlevel\tlate_min_hp\tsynergies\ttop_family";

        // 빨리 감기 (필멸 판): 게임 카메라를 끄고 프레임당 게임 시간을 1/30 초로 묶는다
        // 렌더를 안 하니 프레임이 오르는 만큼 빨리 돈다. 1/30 은 실시간 30fps 판과 같은 간격 — 최고 탄속 24 가 한 프레임 0.8 이동
        // (1/60 으로 묶으면 백그라운드 에디터가 60fps 를 못 내 실시간보다 느려진다 — 10-12 실측)
        private const float FastStep = 1f / 30f;
        private static Camera[] _mutedCameras;

        // 투어 캡처 (아트 · 감각 점검 봇용): 8번째 칸 = 몇 게임 초마다 한 장. Temp/tour/m<맵>_h<주인공>_<초>.png
        private static float _tourEvery;
        private static float _tourNext;
        // 촬영 모드 (트레일러 · 스크린샷): 9 · 10번째 칸 = 찍을 구간(게임 초). 간격이 한 프레임(1/30) 이하면
        // 매 프레임을 Temp/video/f<번호>.png 로 바로 찍는다 — 빨리 감기 간격이 고정이라 그대로 30fps 영상이 된다
        private static float _tourStart;
        private static float _tourEnd = float.MaxValue;
        private static int _videoFrame;

        private static void CaptureTour(float elapsed)
        {
            if (_tourEvery <= 0f || elapsed < _tourStart || elapsed > _tourEnd)
                return;
            if (_tourEvery <= FastStep + 0.0001f)
            {
                Directory.CreateDirectory("Temp/video");
                ScreenCapture.CaptureScreenshot($"Temp/video/f{_videoFrame:00000}.png");
                _videoFrame++;
                return;
            }
            if (elapsed < _tourNext)
                return;
            _tourNext += _tourEvery;
            Directory.CreateDirectory("Temp/tour");
            string file = $"Temp/tour/m{Jinhyeong_Common.RunSelection.MapIndex:00}_h{Jinhyeong_Common.RunSelection.CharacterIndex:00}_{Mathf.RoundToInt(elapsed):000}.png";
            Shot(file, true);
        }

        // 찍을 파일 하나를 들고 있다가, 카메라를 강제로 켠 뒤 ForceDrawFrames 프레임 지나 찍는다
        // combat = 레벨업 창이 열려 있으면 닫힐 때까지 기다린다 (전투 장면이 창에 가리지 않게)
        private const int ForceDrawFrames = 3;
        private static readonly List<(string file, bool combat)> _shots = new List<(string, bool)>(4);
        private static int _forceDraw;
        private static int _drawHold;

        private static void Shot(string file, bool combat)
        {
            for (int i = 0; i < _shots.Count; i++)
            {
                if (_shots[i].file == file)
                    return;
            }
            _shots.Add((file, combat));
        }

        private static void PumpShots()
        {
            if (_drawHold > 0)
                _drawHold--;
            if (_shots.Count == 0)
                return;
            bool levelOpen = Jinhyeong_UI.SurvivorHud.Instance != null && Jinhyeong_UI.SurvivorHud.Instance.LevelUpOpen;
            if (_shots[0].combat && levelOpen)
            {
                _forceDraw = 0;
                return;
            }
            if (_forceDraw <= 0)
            {
                _forceDraw = ForceDrawFrames;
                return;
            }
            _forceDraw--;
            if (_forceDraw > 0)
                return;
            ScreenCapture.CaptureScreenshot(_shots[0].file);
            _shots.RemoveAt(0);
            _drawHold = ForceDrawFrames + 2;
        }

        // 빨리 감기 중 렌더는 이 프레임에 한 번 — 끄면 Game 뷰가 검게 보여 사람이 판을 못 본다
        private const int FastRenderEvery = 6;

        private static void ThrottleCameras()
        {
            if (_mutedCameras == null)
                return;
            bool draw = Time.frameCount % FastRenderEvery == 0 || _forceDraw > 0 || _drawHold > 0;
            for (int i = 0; i < _mutedCameras.Length; i++)
            {
                if (_mutedCameras[i] != null)
                    _mutedCameras[i].enabled = draw;
            }
        }
        private static int _startFrame;
        private static float _endShotReal;
        private static string _saveBackup;
        private const string BossShotFile = "Temp/runloop_boss.png";
        private static float _bossSeenAt = -1f;
        private static bool _bossShotTaken;

        // 보스가 나오고 게임 4초 뒤 한 장 — 끝 캡처 때는 보스가 이미 죽어 있기 쉽다 (카메라를 끈 빨리 감기 판은 검게 찍힌다)
        // 성장 재현 — 판당 사료 1,500 · DNA 31, 앞 챕터 첫 클리어마다 DNA 30 (SaveData 경제와 같은 가정)
        // 챕터 첫 클리어 목표 시점(누적 판). 볼트 SlayerDog 기획서 진행 절과 같다
        private static readonly int[] ChapterRuns = { 2, 5, 9, 14, 20, 27, 35, 44, 54, 65 };
        private const int KibblePerRun = 1500;
        private const int DnaPerRun = 31;
        private static int _growth;
        private static string _growthNote = string.Empty;

        private static void ApplyGrowth()
        {
            _growth = 0;
            _growthNote = string.Empty;
            string spec = string.Empty;
            try { spec = File.ReadAllText(ActiveFile).Trim(); } catch { }
            string[] parts = spec.Split(':');
            if (parts.Length <= 6 || int.TryParse(parts[6], out _growth) == false || _growth <= 0)
                return;

            int cleared = 0;
            for (int i = 0; i < ChapterRuns.Length; i++)
            {
                if (ChapterRuns[i] < _growth)
                    cleared++;
            }
            Jinhyeong_Common.SaveData.DebugSetProgress(_growth * KibblePerRun,
                _growth * DnaPerRun + cleared * Jinhyeong_Common.SaveData.FirstClearDna, Mathf.Max(1, cleared), cleared);
            Jinhyeong_Constellation.ConstellationManager.Reload();

            // 스탯: 가장 싼 것부터 (사람이 고르는 순서의 근사)
            Jinhyeong_Common.SaveData.EStat[] stats = { Jinhyeong_Common.SaveData.EStat.Attack, Jinhyeong_Common.SaveData.EStat.Hp, Jinhyeong_Common.SaveData.EStat.Speed };
            for (int guard = 0; guard < 200; guard++)
            {
                Jinhyeong_Common.SaveData.EStat pick = stats[0];
                int best = int.MaxValue;
                for (int i = 0; i < stats.Length; i++)
                {
                    if (Jinhyeong_Common.SaveData.StatLevel(stats[i]) >= Jinhyeong_Common.SaveData.StatMaxLevel)
                        continue;
                    int c = Jinhyeong_Common.SaveData.StatCost(stats[i]);
                    if (c < best)
                    {
                        best = c;
                        pick = stats[i];
                    }
                }
                if (best == int.MaxValue || Jinhyeong_Common.SaveData.BuyStat(pick) == false)
                    break;
            }

            // 스킬 트리: 닿는 노드 중 가장 싼 것부터 하나씩
            int bought = 0;
            for (int guard = 0; guard < 600; guard++)
            {
                Jinhyeong_Constellation.ConstellationNode cheapest = null;
                int best = int.MaxValue;
                foreach (Jinhyeong_Constellation.ConstellationNode n in Jinhyeong_Constellation.ConstellationManager.Graph.Nodes)
                {
                    if (Jinhyeong_Constellation.ConstellationManager.CanReach(n) == false)
                        continue;
                    int c = Jinhyeong_Constellation.ConstellationManager.NextCost(n);
                    if (c < best)
                    {
                        best = c;
                        cheapest = n;
                    }
                }
                if (cheapest == null || best > Jinhyeong_Common.SaveData.SuperDna)
                    break;
                if (Jinhyeong_Constellation.ConstellationManager.BuyPath(cheapest.Id) == false)
                    break;
                bought++;
            }
            _growthNote = $"성장 {_growth}판 — 스탯 공 {Jinhyeong_Common.SaveData.StatLevel(Jinhyeong_Common.SaveData.EStat.Attack)} · 체 {Jinhyeong_Common.SaveData.StatLevel(Jinhyeong_Common.SaveData.EStat.Hp)} · 이 {Jinhyeong_Common.SaveData.StatLevel(Jinhyeong_Common.SaveData.EStat.Speed)}, 트리 {bought}노드, 남은 DNA {Jinhyeong_Common.SaveData.SuperDna}";
        }

        private static void CaptureBoss(float elapsed)
        {
            if (_bossShotTaken)
                return;
            Jinhyeong_AI.EnemySpawner sp = Jinhyeong_AI.EnemySpawner.Instance;
            if (sp == null || sp.BossSpawnCount <= 0)
                return;
            if (_bossSeenAt < 0f)
                _bossSeenAt = elapsed;
            if (elapsed - _bossSeenAt < 4f)
                return;
            Shot(BossShotFile, true);
            _bossShotTaken = true;
            _bossPatternCount = -1;
            foreach (Jinhyeong_AI.Enemy e in Object.FindObjectsByType<Jinhyeong_AI.Enemy>(FindObjectsSortMode.None))
            {
                if (e != null && e.IsBoss && e.Skills != null)
                    _bossPatternCount = e.Skills.AutoSkillCount;
            }
        }

        private static int _bossPatternCount = -1;
        private static bool _resultWritten;
        private const string PanelShotFile = "Temp/runloop_panel.png";
        private const string EndShotFile = "Temp/runloop_end.png";
        private const string LobbyShotFile = "Temp/runloop_lobby.png";
        private const string LevelShotFile = "Temp/runloop_levelup.png";
        private static bool _levelShotTaken;
        private static int _levelPicks;

        private const int ShotWidth = 1280;
        private const int ShotHeight = 720;

        private const float DefaultRunSeconds = 75f;
        private static float _runSeconds = DefaultRunSeconds;

        private static double _nextPoll;
        private static bool _spawned;
        private static float _startReal;
        private static float _panelOpenedReal = -1f;
        private static bool _panelShotTaken;
        private static bool _endShotTaken;
        private static int _picks;
        private static Player _player;
        private static string _errorNote;
        private static bool _enemyLoadStarted;
        private static bool _refreshed;
        private static bool _mortal;
        private static float _deathTime;
        private static int _seed;
        private static float _startGame;
        private static int _killsR5 = -1;
        private static float _lateMinHp = 100f;
        private static bool _lobbyShotDone;
        private static int _lobbyWaitTicks;
        private const int LobbyWaitMaxTicks = 30;
        private static bool _heroRequested;
        private static int _shotTab = -1;
        private static bool _levelShotDone;
        private static readonly List<string> _samples = new List<string>(16);
        private static int _lastSampledRound;

        private static readonly List<string> _pickedNames = new List<string>(8);

        static RunLoopCheck()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode == false)
            {
                EditorApplication.update += WatchRequest;
            }
            else if (File.Exists(ActiveFile))
            {
                EditorApplication.update += Drive;
            }

            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        }

        private static void HandlePlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;
            EditorApplication.update -= WatchRequest;
            EditorApplication.update += WatchRequest;
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

            if (_refreshed == false)
            {
                _refreshed = true;
                AssetDatabase.Refresh();
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            _refreshed = false;

            string mode = "god";
            try { mode = File.ReadAllText(RequestFile).Trim(); } catch { }
            File.Delete(RequestFile);
            File.WriteAllText(ActiveFile, string.IsNullOrEmpty(mode) ? "god" : mode);

            // 하네스 판은 플레이어 세이브를 남기지 않는다 — Play 에 들어가기 전에 한 번 떠 두고 Finish 에서 되돌린다
            // (Play 안에서 뜨면 Setup 이 다시 불릴 때 성장 재현이 들어간 세이브를 뜬다)
            _saveBackup = PlayerPrefs.HasKey(Jinhyeong_Common.SaveData.Key)
                ? PlayerPrefs.GetString(Jinhyeong_Common.SaveData.Key) : null;

            _lobbyShotDone = false;
            _lobbyWaitTicks = 0;
            _levelShotDone = false;
            GameViewSizer.SetShotSize(ShotWidth, ShotHeight);

            EditorApplication.update -= WatchRequest;
            EditorApplication.EnterPlaymode();
        }

        private static void Drive()
        {
            if (EditorApplication.isPlaying == false)
                return;

            if (_spawned == false)
            {
                AddressableManager am = AddressableManager.Ensure();
                if (_enemyLoadStarted == false)
                {
                    _enemyLoadStarted = true;
                    Cysharp.Threading.Tasks.UniTaskExtensions.Forget(am.LoadAsync("obj_enemy"));
                }
                if (am.IsLoaded("obj_enemy") == false)
                    return;

                Setup();
                return;
            }

            float elapsed = Time.time - _startGame;

            if (_mortal && _deathTime < 0f && _player != null && _player.Damageable != null
                && _player.Damageable.Hp <= 0f)
            {
                _deathTime = elapsed;

                ScreenCapture.CaptureScreenshot(EndShotFile);
                _endShotTaken = true;
                Finish(null);
                return;
            }

            // 레벨업 3택1 — 하네스는 정책대로 바로 고른다 (멈춘 시간이 측정에 새지 않게)
            if (Jinhyeong_UI.SurvivorHud.Instance != null && Jinhyeong_UI.SurvivorHud.Instance.LevelUpOpen)
            {
                _levelPicks++;
                if (_levelShotTaken == false)
                {
                    _levelShotTaken = true;
                    ScreenCapture.CaptureScreenshot(LevelShotFile);
                    return;
                }
                Jinhyeong_UI.SurvivorHud.Instance.DebugAutoPick();
            }
            PumpShots();
            ThrottleCameras();
            CaptureTour(elapsed);
            DriveMoveTowardGems();
            SampleRound(elapsed);
            CapturePeak(elapsed);
            TrackPacing(elapsed);

            // 끝: 정한 게임 시간 · 보스 격파(클리어되면 게임이 멈춰 게임 시간이 안 흐른다) · 실시간 안전판
            bool clearedNow = Jinhyeong_UI.GameFlow.IsCleared;
            CaptureBoss(elapsed);
            bool stalled = Time.realtimeSinceStartup - _startReal > _runSeconds * 1.2f + 60f;
            if (_endShotTaken == false && (elapsed >= _runSeconds || clearedNow || stalled))
            {
                if (stalled && elapsed < _runSeconds && clearedNow == false)
                    _errorNote = $"실시간 안전판으로 끝냄 — 게임 {elapsed:0}초에서 멈춤";
                if (_mortal && Jinhyeong_UI.GameFlow.IsRunning)
                    Jinhyeong_UI.GameFlow.EndGame();

                ScreenCapture.CaptureScreenshot(EndShotFile);
                _endShotTaken = true;
                _endShotReal = Time.realtimeSinceStartup;
                return;
            }
            // 판이 끝나면 게임이 멈춰 게임 시간이 안 흐른다 — 캡처 저장 대기는 실시간으로
            if (_endShotTaken && Time.realtimeSinceStartup - _endShotReal >= 2f)
                Finish(null);
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

            if (Jinhyeong_SkillSystem.PartRegistry.Count == 0)
            {
                Jinhyeong_SkillSystem.PartRegistry.Load();
                Jinhyeong_SkillSystem.PartOptionRegistry.Load();
            }
            if (Jinhyeong_SkillSystem.SkillRegistry.Count == 0)
                Jinhyeong_SkillSystem.SkillRegistry.Load();

            ApplyRequestedHero();
            ApplyGrowth();

            Jinhyeong_Character.Player already = Object.FindFirstObjectByType<Jinhyeong_Character.Player>();
            GameObject go = already != null
                ? already.gameObject
                : Object.Instantiate(prefab, new Vector3(0f, 0.5f, 0f), Quaternion.identity);
            _player = go.GetComponent<Player>();

            Jinhyeong_Parts.PartLoadout parts = go.GetComponent<Jinhyeong_Parts.PartLoadout>();
            if (parts != null)
            {
                parts.CharacterIndex = Jinhyeong_Common.RunSelection.CharacterIndex;
                parts.BeginRun();
                // 밸런스 측정(mortal)은 실제 판처럼 기본 공격 하나로 시작한다. god 은 시너지 검사용으로 둘을 끼운다
                if (IsMortalRequest() == false)
                {
                    EquipTestPart(parts, 2003);
                    EquipTestPart(parts, 2021);
                }
            }
            else
            {
                _errorNote = "PartLoadout 이 obj_player 에 없습니다";
            }

            string groundKey = Jinhyeong_Managers.WorldSpawner.GroundKey;
            GameObject groundPrefab = UnityEngine.AddressableAssets.Addressables
                .LoadAssetAsync<GameObject>(groundKey).WaitForCompletion();
            if (groundPrefab != null)
                Object.Instantiate(groundPrefab, Vector3.zero, Quaternion.identity);
            else
                _errorNote = groundKey + " 로드 실패";

            Cysharp.Threading.Tasks.UniTaskExtensions.Forget(
                AddressableManager.Ensure().LoadAsync(Jinhyeong_Parts.PartDropper.PickupKey));

            if (Jinhyeong_Parts.PartDropper.Instance == null)
            {
                GameObject dropperGo = new GameObject("_PartDropper");
                dropperGo.AddComponent<Jinhyeong_Parts.PartDropper>();
            }

            string spec = "god";
            try { spec = File.ReadAllText(ActiveFile).Trim(); } catch { }
            string[] parts2 = spec.Split(':');
            _mortal = parts2.Length > 0 && parts2[0] == "mortal";

            _runSeconds = DefaultRunSeconds;
            if (parts2.Length > 1 && float.TryParse(parts2[1], out float secs) && secs > 10f)
                _runSeconds = secs;
            _samples.Clear();
            _peakEnemies = 0;
            _levelShotTaken = false;
            _levelPicks = 0;
            _roundMinHp = 100f;
            _lateMinHp = 100f;
            _killsR5 = -1;
            _resultWritten = false;
            _tourEvery = 0f;
            _tourNext = 0f;
            if (parts2.Length > 7 && float.TryParse(parts2[7], out float tour) && tour > 0f)
            {
                _tourEvery = tour;
                _tourNext = tour;
            }
            _tourStart = 0f;
            _tourEnd = float.MaxValue;
            _videoFrame = 0;
            if (parts2.Length > 8 && float.TryParse(parts2[8], out float tStart))
            {
                _tourStart = tStart;
                _tourNext = Mathf.Max(_tourNext, tStart);
            }
            if (parts2.Length > 9 && float.TryParse(parts2[9], out float tEnd) && tEnd > 0f)
                _tourEnd = tEnd;
            _bossSeenAt = -1f;
            _bossShotTaken = false;
            _slotsFullAt = -1f;

            // 6번째 칸 = 시드 (mortal:820:<주인공>:-1:<맵>:<시드>). 0 이면 고정하지 않는다
            _seed = 0;
            if (parts2.Length > 5 && int.TryParse(parts2[5], out int seed))
                _seed = seed;
            if (_seed != 0)
                UnityEngine.Random.InitState(_seed);
            if (_mortal)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                Time.captureDeltaTime = FastStep;
                // 투어 판(사진이 목적)은 카메라를 끄지 않는다 — HUD 는 오버레이 캔버스라 카메라가 안 그린 프레임에서는
                // 화면을 지우지 않고 덧그려, 사진에 지난 레벨업 창이 남고 어두운 막이 쌓여 월드가 검게 찍혔다
                if (_tourEvery <= 0f)
                {
                    _mutedCameras = Camera.allCameras;
                    for (int i = 0; i < _mutedCameras.Length; i++)
                        _mutedCameras[i].enabled = false;
                }
            }
            _peakShotAt = -100f;
            _lastSampledRound = 0;

            if (_mortal == false && _player != null && _player.Damageable != null)
            {
                _player.Damageable.MaxHp = 1000000f;
                _player.Damageable.Hp = 1000000f;
            }
            _deathTime = -1f;
            Damageable.ResetFriendlyFireWatch();
            Jinhyeong_SkillSystem.Skill.SkillVisual.ResetTrailWatch();
            Jinhyeong_SkillSystem.Skill.SkillHit.ResetBurstWatch();
            Jinhyeong_Managers.SfxManager.ResetPlayCount();

            PlayerController controller = go.GetComponent<PlayerController>();
            if (controller != null)
                controller.enabled = false;

            Jinhyeong_UI.Lobby.LobbyWindow lobby = Jinhyeong_UI.Framework.UIManagement.Instance != null
                ? Jinhyeong_UI.Framework.UIManagement.Instance.Find<Jinhyeong_UI.Lobby.LobbyWindow>()
                : null;

            if (_lobbyShotDone == false)
            {
                bool wantShot = _shotTab >= 0 || _heroRequested;
                if (wantShot && lobby == null && _lobbyWaitTicks++ < LobbyWaitMaxTicks)
                    return;

                _lobbyShotDone = true;
                if (lobby != null)
                {
                    if (_shotTab >= 0)
                    {
                        lobby.ShowTab((Jinhyeong_UI.Lobby.LobbyWindow.ETab)_shotTab);

                        Jinhyeong_UI.Lobby.LobbySkillPanel star =
                            lobby.CurrentPanel as Jinhyeong_UI.Lobby.LobbySkillPanel;
                        if (star != null)
                            star.DebugSelectShowcase();
                    }
                    else if (_heroRequested)
                        lobby.ShowTab(Jinhyeong_UI.Lobby.LobbyWindow.ETab.Character);

                    ScreenCapture.CaptureScreenshot(LobbyShotFile);
                    return;
                }
            }

            Jinhyeong_UI.GameFlow.StartGame();

            if (RunProgress.Instance != null)
                RunProgress.Instance.OnLevelUp += HandleLevelUp;

            _startReal = Time.realtimeSinceStartup;
            _startGame = Time.time;
            _startFrame = Time.frameCount;
            _spawned = true;
        }

        private static bool IsMortalRequest()
        {
            string spec = string.Empty;
            try { spec = File.ReadAllText(ActiveFile).Trim(); } catch { }
            return spec.StartsWith("mortal");
        }

        private static void ApplyRequestedHero()
        {
            _heroRequested = false;

            string spec = string.Empty;
            try { spec = File.ReadAllText(ActiveFile).Trim(); } catch { }
            string[] parts = spec.Split(':');
            if (parts.Length > 2 && int.TryParse(parts[2], out int heroIdx) && heroIdx > 0)
            {
                Jinhyeong_Common.RunSelection.CharacterIndex = heroIdx;
                _heroRequested = true;
            }

            _shotTab = -1;
            if (parts.Length > 3 && int.TryParse(parts[3], out int tabIdx) && tabIdx >= 0)
                _shotTab = tabIdx;

            // 5번째 칸 = 맵 순서 (mortal:820:<주인공>:-1:<맵>). 없으면 맵 1
            Jinhyeong_Common.RunSelection.MapIndex = 1;
            if (parts.Length > 4 && int.TryParse(parts[4], out int mapIdx) && mapIdx > 0)
                Jinhyeong_Common.RunSelection.MapIndex = mapIdx;
        }

        private static void HandleLevelUp(int level)
        {
            _panelOpenedReal = Time.realtimeSinceStartup;

            if (_levelShotDone)
                return;
            _levelShotDone = true;
            ScreenCapture.CaptureScreenshot(PanelShotFile);
        }

        private static void DriveMoveTowardGems()
        {
            if (_player == null || Time.timeScale <= 0f)
                return;

            CharacterMotor motor = _player.GetComponent<CharacterMotor>();
            if (motor == null)
                return;

            Jinhyeong_Parts.PartPickup[] boxes =
                Object.FindObjectsByType<Jinhyeong_Parts.PartPickup>(FindObjectsSortMode.None);
            Jinhyeong_Parts.PartPickup box = null;
            float boxSq = float.MaxValue;
            Vector3 me = _player.transform.position;
            for (int i = 0; i < boxes.Length; i++)
            {
                if (boxes[i] == null || boxes[i].gameObject.activeSelf == false)
                    continue;
                float sq = (boxes[i].transform.position - me).sqrMagnitude;
                if (sq < boxSq) { boxSq = sq; box = boxes[i]; }
            }
            if (box != null)
            {
                Vector3 bd = box.transform.position - me;
                Steer(motor, new Vector2(bd.x, bd.z), me);
                return;
            }

            XpGem[] gems = Object.FindObjectsByType<XpGem>(FindObjectsSortMode.None);
            XpGem nearest = null;
            float bestSq = float.MaxValue;
            Vector3 pos = _player.transform.position;
            for (int i = 0; i < gems.Length; i++)
            {
                if (gems[i].gameObject.activeSelf == false)
                    continue;
                float sq = (gems[i].transform.position - pos).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    nearest = gems[i];
                }
            }

            Vector2 goal = Vector2.zero;
            if (nearest != null)
            {
                Vector3 dir = nearest.transform.position - pos;
                goal = new Vector2(dir.x, dir.z);
            }
            Steer(motor, goal, pos);
        }

        private const float AvoidRadius = 4.5f;

        private const float AvoidWeight = 2.2f;

        private static void Steer(Jinhyeong_Character.CharacterMotor motor, Vector2 goal, Vector3 me)
        {
            Vector2 dir = goal.sqrMagnitude > 0.01f ? goal.normalized : Vector2.zero;

            Vector2 push = Vector2.zero;
            IReadOnlyList<Jinhyeong_SkillSystem.Damageable> all = Jinhyeong_SkillSystem.Damageable.All;
            for (int i = 0; i < all.Count; i++)
            {
                Jinhyeong_SkillSystem.Damageable d = all[i];
                if (d == null || d.IsAlive == false)
                    continue;
                if (d.Team != Jinhyeong_SkillSystem.ESkillTeam.Enemy)
                    continue;

                Vector3 off = me - d.transform.position;
                Vector2 flat = new Vector2(off.x, off.z);
                float dist = flat.magnitude;
                if (dist < 0.01f || dist > AvoidRadius)
                    continue;

                push += flat / dist * ((AvoidRadius - dist) / AvoidRadius);
            }

            Vector2 axis = dir + push * AvoidWeight;
            if (axis.sqrMagnitude < 0.01f)
            {
                axis = push.sqrMagnitude > 0.0001f ? push : Vector2.right;
            }
            motor.MoveAxis = axis.normalized;
        }

        private static void EquipTestPart(Jinhyeong_Parts.PartLoadout loadout, int partId)
        {
            Jinhyeong_SkillSystem.PartData data = Jinhyeong_SkillSystem.PartRegistry.Get(partId);
            if (data == null)
            {
                _errorNote = $"파츠 {partId} 가 레지스트리에 없습니다 (Part.json 확인)";
                return;
            }
            Jinhyeong_Parts.PartInstance inst =
                Jinhyeong_Parts.PartInstance.Create(data, Jinhyeong_Parts.EPartGrade.Legendary);
            if (loadout.TryEquip(inst) == false)
                _errorNote = $"파츠 {partId} 착용 실패 (부위 잠김?)";
        }

        private static string ScanBrokenShaders()
        {
            Renderer[] all = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            List<string> broken = new List<string>(4);
            for (int i = 0; i < all.Length; i++)
            {
                Renderer r = all[i];
                if (r == null)
                    continue;
                Material m = r.sharedMaterial;
                if (m == null)
                {
                    broken.Add($"{r.gameObject.name}(머티리얼 없음)");
                    continue;
                }
                if (m.shader == null || m.shader.name.Contains("InternalErrorShader"))
                    broken.Add($"{r.gameObject.name}/{m.name}(셰이더 깨짐)");
            }
            if (broken.Count == 0)
                return $"렌더러 {all.Length}개 — 셰이더 이상 없음";
            return $"렌더러 {all.Length}개 — 깨짐 {broken.Count}: {string.Join(", ", broken)}";
        }

        private static string ScanView()
        {
            Camera cam = Jinhyeong_Character.CameraFollow.GameCamera;
            Transform ground = null;
            Jinhyeong_View.TilemapStreamer gt = Object.FindFirstObjectByType<Jinhyeong_View.TilemapStreamer>();
            if (gt != null)
                ground = gt.transform;

            string camTxt = cam != null
                ? $"pos={cam.transform.position} ortho={cam.orthographic} size={cam.orthographicSize:0.0} rot={cam.transform.eulerAngles}"
                : "없음";
            string plTxt = _player != null ? _player.transform.position.ToString() : "없음";
            string gdTxt = ground != null ? $"{ground.position} scale={ground.localScale}" : "없음";

            int enemies = 0;
            Jinhyeong_AI.Enemy[] all = Object.FindObjectsByType<Jinhyeong_AI.Enemy>(FindObjectsSortMode.None);
            Vector3 sample = Vector3.zero;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i].gameObject.activeInHierarchy == false)
                    continue;
                if (enemies == 0)
                    sample = all[i].transform.position;
                enemies++;
            }

            return "카메라 " + camTxt + " | 플레이어 " + plTxt + " | 바닥 " + gdTxt + " | 활성 적 " + enemies + " (예: " + sample + ")";
        }

        // 최대 밀도 캡처 — 동시 생존 적이 지금까지 최대를 넘을 때 찍는다 (5초 간격). 가독성 판정용
        private const string PeakShotFile = "Temp/runloop_peak.png";
        private static int _peakEnemies;
        private static float _peakShotAt = -100f;

        // 뽕맛 계측 — 라운드 안 최저 HP · 6칸이 다 찬 시각
        private static float _roundMinHp = 100f;
        private static float _slotsFullAt = -1f;

        private static void TrackPacing(float elapsed)
        {
            Damageable hp = _player != null ? _player.Damageable : null;
            if (hp != null && hp.MaxHp > 0f)
                _roundMinHp = Mathf.Min(_roundMinHp, hp.Hp / hp.MaxHp * 100f);
            Jinhyeong_AI.WaveDirector wdNow = Jinhyeong_AI.WaveDirector.Instance;
            if (hp != null && hp.MaxHp > 0f && wdNow != null && Jinhyeong_SkillSystem.RoundRegistry.RoundOf(wdNow.Elapsed) >= 10)
                _lateMinHp = Mathf.Min(_lateMinHp, hp.Hp / hp.MaxHp * 100f);
            Jinhyeong_Parts.PartLoadout pl = _player != null ? _player.GetComponent<Jinhyeong_Parts.PartLoadout>() : null;
            if (_slotsFullAt < 0f && pl != null && pl.EquippedCount >= Jinhyeong_Parts.PartLoadout.SlotCount)
                _slotsFullAt = elapsed;
        }

        private static void CapturePeak(float elapsed)
        {
            Jinhyeong_AI.EnemySpawner sp = Jinhyeong_AI.EnemySpawner.Instance;
            if (sp == null)
                return;
            int n = sp.Active.Count;
            if (n <= _peakEnemies || elapsed - _peakShotAt < 5f)
                return;
            _peakEnemies = n;
            _peakShotAt = elapsed;
            Shot(PeakShotFile, true);
        }

        private static void SampleRound(float elapsed)
        {
            Jinhyeong_AI.WaveDirector wd = Jinhyeong_AI.WaveDirector.Instance;
            if (wd == null)
                return;
            int r = Jinhyeong_SkillSystem.RoundRegistry.RoundOf(wd.Elapsed);
            if (r == _lastSampledRound)
                return;
            _lastSampledRound = r;

            Jinhyeong_AI.EnemySpawner sp = Jinhyeong_AI.EnemySpawner.Instance;
            RunProgress pg = RunProgress.Instance;
            Damageable hp = _player != null ? _player.Damageable : null;
            Jinhyeong_Parts.PartLoadout pl = _player != null
                ? _player.GetComponent<Jinhyeong_Parts.PartLoadout>() : null;
            Jinhyeong_SkillSystem.SkillObject so = _player != null ? _player.Skills : null;

            float hpPct = hp != null && hp.MaxHp > 0f ? hp.Hp / hp.MaxHp * 100f : -1f;
            if (r == 6 && sp != null)
                _killsR5 = sp.KillCount;
            Jinhyeong_Parts.PartInstance basic = pl != null ? pl.UniquePart : null;
            _samples.Add(string.Format(
                "| {0} | {1:0} | {2} | {3} | {4} | {5} | {6:0} | {7:0} | {8} | {9} | {10:0} |",
                r, elapsed,
                sp != null ? sp.KillCount : -1,
                pg != null ? pg.Level : -1,
                sp != null ? sp.Active.Count : -1,
                pl != null ? pl.EquippedCount : -1,
                so != null ? so.AttackPower : -1f,
                hpPct,
                pl != null ? pl.ActiveSynergies.Count : -1,
                basic != null ? basic.Options.Count : -1,
                _roundMinHp));
            _roundMinHp = 100f;
        }

        // E-2 계측: 움직인 스킬 중 잔상을 남긴 비율, 적을 맞힌 스킬 중 버스트를 낸 비율
        private static string TrailLine()
        {
            var moved = Jinhyeong_SkillSystem.Skill.SkillVisual.MovedKeys;
            var ghost = Jinhyeong_SkillSystem.Skill.SkillVisual.GhostKeys;
            int hit = 0;
            foreach (string k in moved)
            {
                if (ghost.Contains(k))
                    hit++;
            }
            return $"[잔상] 이동 {moved.Count}종 · 잔상 {hit}종 ({(moved.Count > 0 ? hit * 100 / moved.Count : 0)}%) · {string.Join(",", moved)}";
        }

        private static string BurstLine()
        {
            var hits = Jinhyeong_SkillSystem.Skill.SkillHit.HitKeys;
            var bursts = Jinhyeong_SkillSystem.Skill.SkillHit.BurstKeys;
            int n = 0;
            foreach (string k in hits)
            {
                if (bursts.Contains(k))
                    n++;
            }
            return $"[버스트] 적중 {hits.Count}종 · 버스트 {n}종 ({(hits.Count > 0 ? n * 100 / hits.Count : 0)}%)";
        }

        // "<계열>:<개수>/<장착 수>" — 고유 공격(첫 칸)도 포함
        private static string TopFamily(Jinhyeong_Parts.PartLoadout pl)
        {
            if (pl == null)
                return "-";
            Dictionary<ESkillFamily, int> count = new Dictionary<ESkillFamily, int>();
            int total = 0;
            for (int i = 0; i < pl.Equipped.Count; i++)
            {
                Jinhyeong_Parts.PartInstance p = pl.Equipped[i];
                if (p == null || p.Data == null)
                    continue;
                total++;
                count.TryGetValue(p.Data.Family, out int n);
                count[p.Data.Family] = n + 1;
            }
            ESkillFamily best = ESkillFamily.None;
            int bestN = 0;
            foreach (KeyValuePair<ESkillFamily, int> kv in count)
            {
                if (kv.Value > bestN)
                {
                    best = kv.Key;
                    bestN = kv.Value;
                }
            }
            return best + ":" + bestN + "/" + total;
        }

        private static void AppendResult(EnemySpawner spawner, RunProgress progress, bool cleared)
        {
            // 사망 판은 Finish 가 두 번 불릴 수 있다 — 한 판에 한 줄
            if (_resultWritten)
                return;
            _resultWritten = true;
            Jinhyeong_Parts.PartLoadout pl = _player != null ? _player.GetComponent<Jinhyeong_Parts.PartLoadout>() : null;
            string row = string.Join("\t", new[]
            {
                Jinhyeong_Common.RunSelection.CharacterIndex.ToString(),
                Jinhyeong_Common.RunSelection.MapIndex.ToString(),
                _seed.ToString() + (_growth > 0 ? "g" + _growth : string.Empty),
                cleared ? "1" : "0",
                (_deathTime >= 0f ? _deathTime : _runSeconds).ToString("0"),
                (spawner != null ? spawner.KillCount : -1).ToString(),
                _killsR5.ToString(),
                (progress != null ? progress.Level : -1).ToString(),
                _lateMinHp.ToString("0"),
                (pl != null ? pl.ActiveSynergies.Count : -1).ToString(),
                TopFamily(pl)
            });
            try
            {
                if (File.Exists(ResultsFile) == false)
                    File.WriteAllText(ResultsFile, ResultsHeader + "\n", Encoding.UTF8);
                File.AppendAllText(ResultsFile, row + "\n", Encoding.UTF8);
            }
            catch (IOException e)
            {
                _errorNote = $"결과 줄 쓰기 실패 — {e.Message} · {row}";
            }
        }

        private static void Finish(string error)
        {
            EditorApplication.update -= Drive;
            if (RunProgress.Instance != null)
                RunProgress.Instance.OnLevelUp -= HandleLevelUp;
            _shots.Clear();
            _forceDraw = 0;
            _drawHold = 0;

            StringBuilder report = new StringBuilder(1024);
            if (string.IsNullOrEmpty(error) == false)
            {
                report.AppendLine($"[중단] {error}");
            }
            else
            {
                EnemySpawner spawner = EnemySpawner.Instance;
                RunProgress progress = RunProgress.Instance;
                if (_mortal && Jinhyeong_UI.GameFlow.IsRunning)
                    Jinhyeong_UI.GameFlow.EndGame();

                report.AppendLine("=== 코어 루프 실측 ===");
                report.AppendLine($"킬={(spawner != null ? spawner.KillCount : -1)}  레벨={(progress != null ? progress.Level : -1)}  잔여XP={(progress != null ? progress.Xp : -1f):0.0}/{(progress != null ? progress.XpToNext : -1f):0.0}");
                Jinhyeong_Parts.PartLoadout pl = _player != null
                    ? _player.GetComponent<Jinhyeong_Parts.PartLoadout>() : null;
                report.AppendLine($"보유 스킬 {(pl != null ? pl.EquippedCount : -1)}/{Jinhyeong_Parts.PartLoadout.SlotCount}");
                report.AppendLine($"파츠 테이블 {Jinhyeong_SkillSystem.PartRegistry.Count}종 / 옵션 {Jinhyeong_SkillSystem.PartOptionRegistry.Count}종 / 스킬 {Jinhyeong_SkillSystem.SkillRegistry.Count}종");
                report.AppendLine($"생존 적={Jinhyeong_SkillSystem.Damageable.All.Count}");
                Jinhyeong_SkillSystem.SkillObject so = _player != null ? _player.Skills : null;
                {
                    Jinhyeong_Character.Player[] players =
                        Object.FindObjectsByType<Jinhyeong_Character.Player>(FindObjectsSortMode.None);
                    System.Text.StringBuilder who = new System.Text.StringBuilder();
                    System.Collections.Generic.IReadOnlyList<Damageable> all = Damageable.All;
                    int shown = 0;
                    for (int i = 0; i < all.Count && shown < 8; i++)
                    {
                        Damageable d = all[i];
                        if (d == null || d.IsAlive == false)
                            continue;
                        if (who.Length > 0)
                            who.Append(", ");
                        who.Append($"{d.name}[{d.Team}]");
                        shown++;
                    }
                    report.AppendLine($"플레이어 컴포넌트={players.Length}개  생존 Damageable: {who}");
                }

                Jinhyeong_Parts.PartLoadout plForReport =
                    _player != null ? _player.GetComponent<Jinhyeong_Parts.PartLoadout>() : null;
                Jinhyeong_SkillSystem.MapData mapForReport = Jinhyeong_SkillSystem.MapRegistry.Current;
                report.AppendLine($"맵={(mapForReport != null ? mapForReport.DisplayName : "-")}(#{Jinhyeong_Common.RunSelection.MapIndex})" +
                    $"  적HP x{(mapForReport != null ? mapForReport.HpMul : 1f):0.00} 피해 x{(mapForReport != null ? mapForReport.DamageMul : 1f):0.00}");
                report.AppendLine($"슈퍼독={Jinhyeong_Common.RunSelection.CurrentHero.Name}" +
                    $"(#{Jinhyeong_Common.RunSelection.CharacterIndex})" +
                    $"  고유스킬={(plForReport != null && plForReport.EquippedCount > 0 ? plForReport.Equipped[0].DisplayName : "-")}");
                if (string.IsNullOrEmpty(Jinhyeong_UI.SurvivorHud.DimNote) == false)
                    report.AppendLine($"[결과딤] {Jinhyeong_UI.SurvivorHud.DimNote}");
                report.AppendLine($"효과음 클립={Jinhyeong_Managers.SfxManager.LoadedCount}종  재생={Jinhyeong_Managers.SfxManager.PlayCount}회");
                report.AppendLine($"자동스킬={(so != null ? so.AutoSkillCount : -1)}개  팀={(so != null ? so.Team.ToString() : "-")}  피해배수={(so != null ? so.OutgoingDamageMultiplier : -1f):0.00}");
                report.AppendLine(ScanBrokenShaders());
                report.AppendLine(ScanView());
                Jinhyeong_Parts.PartLoadout pl2 = _player != null
                    ? _player.GetComponent<Jinhyeong_Parts.PartLoadout>() : null;
                if (pl2 != null)
                {
                    System.Collections.Generic.IReadOnlyList<Jinhyeong_SkillSystem.SynergyData> syn = pl2.ActiveSynergies;
                    System.Text.StringBuilder sb2 = new System.Text.StringBuilder();
                    for (int i = 0; i < syn.Count; i++)
                    {
                        if (sb2.Length > 0)
                            sb2.Append(", ");
                        sb2.Append(syn[i].DisplayName);
                    }
                    report.AppendLine($"시너지 {syn.Count}개: {(sb2.Length > 0 ? sb2.ToString() : "-")}  선호계열={pl2.PreferredFamily}  (테이블 {Jinhyeong_SkillSystem.SynergyRegistry.Count}종)");
                }
                Jinhyeong_AI.WaveDirector wd = Jinhyeong_AI.WaveDirector.Instance;
                Jinhyeong_SkillSystem.RoundData rd = wd != null
                    ? Jinhyeong_SkillSystem.RoundRegistry.At(wd.Elapsed) : null;
                report.AppendLine(rd != null
                    ? $"라운드 {rd.Round}/{Jinhyeong_SkillSystem.RoundRegistry.LastRound}  간격 {rd.SpawnGap:0.00}s x{rd.SpawnBatch}  상한 {rd.MaxAlive}  적HP x{rd.HpMul:0.00} 피해 x{rd.DamageMul:0.00}"
                    : "라운드 테이블 없음");
                if (_samples.Count > 0)
                {
                    report.AppendLine();
                    report.AppendLine("| 라운드 | 초 | 킬 | Lv | 생존적 | 파츠 | 공격력 | HP% | 시너지 | 앰프 | 직전 최저HP% |");
                    report.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
                    for (int i = 0; i < _samples.Count; i++)
                        report.AppendLine(_samples[i]);
                    report.AppendLine();
                }
                Jinhyeong_AI.EnemySpawner sp = Jinhyeong_AI.EnemySpawner.Instance;
                int bossSpawned = sp != null ? sp.BossSpawnCount : -1;
                int bossKilled = sp != null ? sp.BossKillCount : -1;
                bool cleared = Jinhyeong_UI.GameFlow.IsCleared;

                Jinhyeong_AI.Enemy[] live = Object.FindObjectsByType<Jinhyeong_AI.Enemy>(FindObjectsSortMode.None);
                Jinhyeong_SkillSystem.Damageable bossHp = null;
                for (int i = 0; i < live.Length; i++)
                {
                    if (live[i] != null && live[i].isActiveAndEnabled && live[i].IsBoss)
                    {
                        bossHp = live[i].GetComponent<Jinhyeong_SkillSystem.Damageable>();
                        break;
                    }
                }

                if (bossHp != null)
                {
                    report.AppendLine($"[보스] 등장 {bossSpawned} · 생존 중 HP {bossHp.Hp:0}/{bossHp.MaxHp:0} " +
                        $"({bossHp.Hp / Mathf.Max(1f, bossHp.MaxHp) * 100f:0}%)");
                }
                else if (bossKilled > 0)
                {
                    report.AppendLine(cleared
                        ? $"[클리어] 보스 격파 (등장 {bossSpawned} · 처치 {bossKilled})"
                        : $"[보스] 처치 {bossKilled} 인데 클리어가 안 걸렸다 — GameFlow.ClearGame 경로 확인");
                }
                else
                {
                    report.AppendLine($"[보스] 미등장 (등장 카운터 {bossSpawned})");
                }

                report.AppendLine(Damageable.FriendlyFireHits == 0
                    ? "[자해] 없음"
                    : $"[자해] {Damageable.FriendlyFireHits}회 · 최대 {Damageable.FriendlyFireWorst:0.0} · {Damageable.FriendlyFireNote}");
                report.AppendLine($"[최대 밀도] 동시 적 {_peakEnemies} · 캡처 {PeakShotFile}");
                if (_bossShotTaken)
                    report.AppendLine($"[보스 패턴] 장착 {_bossPatternCount} · 캡처 {BossShotFile}");
                if (string.IsNullOrEmpty(_growthNote) == false)
                    report.AppendLine($"[성장] {_growthNote}");
                report.AppendLine(_slotsFullAt >= 0f ? $"[슬롯 완성] {_slotsFullAt:0}초" : "[슬롯 완성] 못 채움");
                report.AppendLine($"[로비] LobbyWindow {Object.FindObjectsByType<Jinhyeong_UI.Lobby.LobbyWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length}개 · StartPanel {Object.FindObjectsByType<Jinhyeong_UI.Lobby.LobbyStartPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length}개 (씬 배치 재사용이면 각 1)");
                report.AppendLine($"[레벨업 3택1] 열린 프레임 {_levelPicks} · 캡처 {LevelShotFile}");
                report.AppendLine(TrailLine());
                report.AppendLine(BurstLine());

                report.AppendLine(_mortal
                    ? (_deathTime >= 0f ? $"[필멸] 생존 {_deathTime:0.0}초" : $"[필멸] {_runSeconds:0}초 완주 (사망 없음)")
                    : "[무적] 밸런스 지표 아님");
                if (string.IsNullOrEmpty(_errorNote) == false)
                    report.AppendLine($"[진단] {_errorNote}");

                Damageable hp = GameEvents.CurrentPlayer != null
                    ? GameEvents.CurrentPlayer.GetComponent<Damageable>() : null;
                report.AppendLine($"플레이어 HP={(hp != null ? hp.Hp : -1f):0.0}/{(hp != null ? hp.MaxHp : -1f):0.0}  버프={(hp != null ? hp.Status.Count : -1)}");
                report.AppendLine($"timeScale={Time.timeScale} (1 이어야 정상 복귀)");

                float real = Time.realtimeSinceStartup - _startReal;
                float game = Time.time - _startGame;
                report.AppendLine($"[속도] 게임 {game:0}초 / 실시간 {real:0}초 = x{game / Mathf.Max(1f, real):0.0} · {(Time.frameCount - _startFrame) / Mathf.Max(1f, real):0} fps");

                if (_mortal)
                    AppendResult(spawner, progress, Jinhyeong_UI.GameFlow.IsCleared);
            }
            Time.captureDeltaTime = 0f;
            if (_saveBackup != null)
                PlayerPrefs.SetString(Jinhyeong_Common.SaveData.Key, _saveBackup);
            else
                PlayerPrefs.DeleteKey(Jinhyeong_Common.SaveData.Key);
            PlayerPrefs.Save();
            Jinhyeong_Common.SaveData.Load();
            Jinhyeong_Constellation.ConstellationManager.Reload();
            if (_mutedCameras != null)
            {
                for (int i = 0; i < _mutedCameras.Length; i++)
                {
                    if (_mutedCameras[i] != null)
                        _mutedCameras[i].enabled = true;
                }
                _mutedCameras = null;
            }

            File.WriteAllText(ReportFile, report.ToString(), Encoding.UTF8);
            if (File.Exists(ActiveFile))
                File.Delete(ActiveFile);

            EditorApplication.isPlaying = false;
        }
    }
}
