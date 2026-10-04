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

            float elapsed = Time.realtimeSinceStartup - _startReal;

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
            DriveMoveTowardGems();
            SampleRound(elapsed);
            CapturePeak(elapsed);
            TrackPacing(elapsed);

            if (_endShotTaken == false && elapsed >= _runSeconds)
            {
                if (_mortal && Jinhyeong_UI.GameFlow.IsRunning)
                    Jinhyeong_UI.GameFlow.EndGame();

                ScreenCapture.CaptureScreenshot(EndShotFile);
                _endShotTaken = true;
                return;
            }
            if (_endShotTaken && elapsed >= _runSeconds + 2f)
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

            GameObject groundPrefab = UnityEngine.AddressableAssets.Addressables
                .LoadAssetAsync<GameObject>("obj_ground").WaitForCompletion();
            if (groundPrefab != null)
                Object.Instantiate(groundPrefab, Vector3.zero, Quaternion.identity);
            else
                _errorNote = "obj_ground 로드 실패";

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
            _slotsFullAt = -1f;
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
            ScreenCapture.CaptureScreenshot(PeakShotFile);
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

        private static void Finish(string error)
        {
            EditorApplication.update -= Drive;
            if (RunProgress.Instance != null)
                RunProgress.Instance.OnLevelUp -= HandleLevelUp;

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
            }

            File.WriteAllText(ReportFile, report.ToString(), Encoding.UTF8);
            if (File.Exists(ActiveFile))
                File.Delete(ActiveFile);

            EditorApplication.isPlaying = false;
        }
    }
}
