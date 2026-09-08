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

    /// <summary>
    /// 뱀서 코어 루프(처치→젬→경험치→레벨업→증강 3택1) 실측 하네스.
    ///
    /// - 자동 발동(OnTick) 스킬만 장착해 입력 없이 적을 잡는다
    /// - 플레이어는 가장 가까운 젬을 향해 걷게 조종한다 (자석 반경 안으로 들어가면 흡수)
    /// - 레벨업 패널이 뜨면(timeScale=0) 잠깐 보여준 뒤 첫 카드를 자동 클릭한다
    /// - 첫 패널은 스크린샷으로 남긴다
    ///
    /// 페이싱은 전부 unscaledTime — timeScale=0 인 패널 대기 중에도 하네스는 돌아야 한다.
    /// </summary>
    [InitializeOnLoad]
    public static class RunLoopCheck
    {
        private const string RequestFile = "Temp/runloop.request";
        private const string ActiveFile = "Temp/runloop.active";
        private const string ReportFile = "Temp/runloop.report";
        private const string PanelShotFile = "Temp/runloop_panel.png";
        private const string EndShotFile = "Temp/runloop_end.png";
        private const string LobbyShotFile = "Temp/runloop_lobby.png";

        /// <summary>
        /// 스크린샷 해상도. **PC 게임**이라 가로 16:9 다.
        /// [사고] 모바일로 오해해 9:16 으로 맞췄었다 — 룩 판정의 전제가 화면비이므로,
        /// 이 값이 틀리면 UI 를 고칠 때마다 다른 곳이 깨진다.
        /// </summary>
        private const int ShotWidth = 1280;
        private const int ShotHeight = 720;

        /// <summary>기본 실행 시간(초). 요청 파일에 'mortal:300' 처럼 적어 덮어쓴다.</summary>
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
        /// <summary>로비 창이 뜰 때까지 기다리는 틱 상한. 폴링 1초 간격이라 30틱 ≈ 30초.</summary>
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

            // [사고] 플레이를 끝내고 편집 모드로 돌아오면 감시자가 붙어 있지 않아
            // 그 뒤로는 요청 파일을 만들어도 아무 일도 안 일어났다. 스크립트를 고쳐
            // 도메인 리로드가 나야만 되살아나서, 원인이 「에디터가 죽었나」로 보였다.
            // 편집 모드 복귀 때마다 다시 건다.
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

            // [사고] Refresh 직후 곧바로 플레이에 들어가면, 컴파일이 **한 박자 늦게** 시작되면서
            // 플레이 진입 중 도메인 리로드가 난다. 하네스 정적 상태가 통째로 날아가 리포트가
            // 영영 안 나오고, 증상은 「에디터가 멈춤」으로 보인다.
            // 한 번 새로 고치고 **다음 폴링까지 기다렸다가** 들어간다.
            if (_refreshed == false)
            {
                _refreshed = true;
                AssetDatabase.Refresh();
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            _refreshed = false;

            // 요청 파일 내용이 모드다. 'mortal' 이면 무적을 끄고 실제 생존을 잰다.
            string mode = "god";
            try { mode = File.ReadAllText(RequestFile).Trim(); } catch { }
            File.Delete(RequestFile);
            File.WriteAllText(ActiveFile, string.IsNullOrEmpty(mode) ? "god" : mode);

            // [사고] 이 초기화를 스폰 루틴 안에 뒀더니, 그 루틴이 스폰될 때까지 매 틱 다시 도는 바람에
            // 「초기화 -> 촬영 -> return」 이 무한 반복돼 **플레이가 영영 시작되지 않았다**.
            // 런당 한 번만 도는 곳은 여기다.
            _lobbyShotDone = false;
            _lobbyWaitTicks = 0;
            _levelShotDone = false;
            // 룩 판정의 전제는 타깃 화면비다. 가로로 찍은 그림을 보고 세로 UI 를 고치면
            // 고칠 때마다 다른 곳이 깨진다. 플레이에 들어가기 전에 맞춘다.
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
                // 적 프리팹이 캐시에 오르기 전엔 시작하지 않는다
                if (am.IsLoaded("obj_enemy") == false)
                    return;

                Setup();
                return;
            }

            float elapsed = Time.realtimeSinceStartup - _startReal;

            // 필멸 모드에선 언제 죽었는지가 곧 밸런스 지표다.
            if (_mortal && _deathTime < 0f && _player != null && _player.Damageable != null
                && _player.Damageable.Hp <= 0f)
            {
                _deathTime = elapsed;

                // [사고] 죽고 나면 씬이 리로드되며 플레이어 참조가 끊겨, 남은 시간을 더 돌려봐야
                // 리포트가 -1 로 채워진다. 필멸 측정은 **사망이 곧 끝**이므로 여기서 마감한다.
                ScreenCapture.CaptureScreenshot(EndShotFile);
                _endShotTaken = true;
                Finish(null);
                return;
            }

            DriveMoveTowardGems();
            HandleAugmentPanel();
            SampleRound(elapsed);

            if (_endShotTaken == false && elapsed >= _runSeconds)
            {
                // [사고] 종료 샷을 런을 닫기 **전에** 찍어서, 결과 화면(CLEAR/GAME OVER)을
                // 한 번도 제대로 본 적이 없었다. 「결과 화면 딤이 안 먹는다」를 스크린샷으로
                // 확인하려 했는데 애초에 결과 화면이 안 찍히고 있었다.
                // 필멸 측정은 여기서 런이 끝나는 것이므로, 닫고 나서 찍는다.
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

            // 파츠 테이블이 아직 안 올라왔으면 지금 올린다 — 스킬 레지스트리 로드가 비동기라 순서가 보장되지 않는다.
            if (Jinhyeong_SkillSystem.PartRegistry.Count == 0)
            {
                Jinhyeong_SkillSystem.PartRegistry.Load();
                Jinhyeong_SkillSystem.PartOptionRegistry.Load();
            }
            if (Jinhyeong_SkillSystem.SkillRegistry.Count == 0)
                Jinhyeong_SkillSystem.SkillRegistry.Load();

            // [사고] 요청 스펙을 BeginRun **뒤에** 읽어서, 'god:60:2'(코기)를 줘도
            // 시바의 고유 스킬이 들어갔다. 고른 캐릭터는 고유 파츠를 끼우기 전에 정해져야 한다.
            ApplyRequestedHero();

            // [사고] LobbyWindow 의 비동기 흐름이 먼저 플레이어를 세우는데 하네스가 또 세워
            // **개가 두 마리** 돌아다녔다. 스킬이 두 벌 나가 그동안의 밸런스 실측이 통째로 부풀려졌다.
            // 화면을 세로로 바꾸기 전까지는 둘째가 화면 밖이라 보이지도 않았다.
            // 스폰 순서를 맞추려 들지 말고, 있으면 그걸 쓴다.
            Jinhyeong_Character.Player already = Object.FindFirstObjectByType<Jinhyeong_Character.Player>();
            GameObject go = already != null
                ? already.gameObject
                : Object.Instantiate(prefab, new Vector3(0f, 0.5f, 0f), Quaternion.identity);
            _player = go.GetComponent<Player>();

            // 파츠 체제라 로드아웃으로 스킬을 주지 않는다. 고유 파츠 + 대표 파츠 2개를 직접 끼워
            // 「파츠 착용 -> 스킬 자동 발동 -> 적 사망」 경로 전체를 실측한다.
            Jinhyeong_Parts.PartLoadout parts = go.GetComponent<Jinhyeong_Parts.PartLoadout>();
            if (parts != null)
            {
                parts.CharacterIndex = Jinhyeong_Common.RunSelection.CharacterIndex;
                parts.BeginRun();
                EquipTestPart(parts, 2003);   // 으르렁 — 주변 광역, 입력 없이 붙은 적을 정리한다
                EquipTestPart(parts, 2021);   // God-사료 폭탄 — 원거리 광역
            }
            else
            {
                _errorNote = "PartLoadout 이 obj_player 에 없습니다";
            }

            // 바닥. 하네스는 WorldSpawner 를 안 타므로 여기서 직접 띄우지 않으면
            // 배경이 카메라 클리어색(민무늬)으로만 나와 룩 판정을 할 수 없다.
            GameObject groundPrefab = UnityEngine.AddressableAssets.Addressables
                .LoadAssetAsync<GameObject>("obj_ground").WaitForCompletion();
            if (groundPrefab != null)
                Object.Instantiate(groundPrefab, Vector3.zero, Quaternion.identity);
            else
                _errorNote = "obj_ground 로드 실패";

            // 파츠 드랍 관리자. 하네스는 WorldSpawner 를 안 타므로 직접 세우지 않으면
            // 상자가 한 개도 안 떨어지고, 그러면 「파츠 3개로 얼마나 버티나」를 재게 된다.
            // 상자 프리팹이 캐시에 없으면 드랍이 조용히 실패한다.
            Cysharp.Threading.Tasks.UniTaskExtensions.Forget(
                AddressableManager.Ensure().LoadAsync(Jinhyeong_Parts.PartDropper.PickupKey));

            if (Jinhyeong_Parts.PartDropper.Instance == null)
            {
                GameObject dropperGo = new GameObject("_PartDropper");
                dropperGo.AddComponent<Jinhyeong_Parts.PartDropper>();
            }

            // 무적 여부는 요청 모드가 정한다.
            //   god    — 피해 경로 검증용. 죽으면 스킬 틱이 멈춰 「피해가 들어가는가」를 못 잰다
            //   mortal — 밸런스 검증용. 실제로 몇 초 버티는지를 잰다
            // 'mortal:300' 처럼 모드와 시간을 같이 받는다.
            string spec = "god";
            try { spec = File.ReadAllText(ActiveFile).Trim(); } catch { }
            string[] parts2 = spec.Split(':');
            _mortal = parts2.Length > 0 && parts2[0] == "mortal";

            _runSeconds = DefaultRunSeconds;
            if (parts2.Length > 1 && float.TryParse(parts2[1], out float secs) && secs > 10f)
                _runSeconds = secs;
            _samples.Clear();
            _lastSampledRound = 0;

            if (_mortal == false && _player != null && _player.Damageable != null)
            {
                _player.Damageable.MaxHp = 1000000f;
                _player.Damageable.Hp = 1000000f;
            }
            _deathTime = -1f;
            Damageable.ResetFriendlyFireWatch();
            Jinhyeong_Managers.SfxManager.ResetPlayCount();

            // 하네스가 이동을 직접 조종하므로 입력 컨트롤러는 끈다 (매 프레임 MoveAxis 를 덮어쓰기 때문)
            PlayerController controller = go.GetComponent<PlayerController>();
            if (controller != null)
                controller.enabled = false;

            // 로비는 하네스가 끄지 않는다. GameFlow.StartGame() 이 OnGameStarted 로 닫고,
            // 아직 로드 중이던 로비는 열리는 순간 IsRunning 을 보고 스스로 사라진다.
            Jinhyeong_UI.Lobby.LobbyWindow lobby = Jinhyeong_UI.Framework.UIManagement.Instance != null
                ? Jinhyeong_UI.Framework.UIManagement.Instance.Find<Jinhyeong_UI.Lobby.LobbyWindow>()
                : null;

            // 로비를 내리기 전에 한 컷 남긴다. 하네스는 로비를 자동으로 지나치므로
            // 이게 없으면 첫 화면이 어떻게 보이는지 확인할 방법이 없다.
            //
            // [사고] 촬영과 닫기를 한 프레임에 했더니 스크린샷에 게임 월드가 찍혔다.
            // CaptureScreenshot 은 **프레임 끝**에 기록하므로, 그 사이에 로비를 내리면 늦다.
            // 촬영한 틱에서는 아무것도 더 하지 않고 다음 틱에 시작한다.
            if (_lobbyShotDone == false)
            {
                bool wantShot = _shotTab >= 0 || _heroRequested;
                // 창은 키로 비동기 로드된다. 찍을 거면 뜰 때까지 조금 기다린다 — 무한정은 아니다.
                if (wantShot && lobby == null && _lobbyWaitTicks++ < LobbyWaitMaxTicks)
                    return;

                _lobbyShotDone = true;
                if (lobby != null)
                {
                    // 캐릭터를 지정한 요청이면 그 화면을 찍는다. 메인 탭만 찍으면
                    // 「캐릭터 탭이 어떻게 보이나」를 확인할 방법이 없다.
                    if (_shotTab >= 0)
                    {
                        lobby.ShowTab((Jinhyeong_UI.Lobby.LobbyWindow.ETab)_shotTab);

                        // 스킬 트리는 「별을 하나 고른 상태」가 기본 화면이 아니다.
                        // 설명 패널은 누른 뒤에만 뜨므로, 찍으려면 하네스가 대신 눌러 줘야 한다.
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

        /// <summary>
        /// 요청 스펙('mortal:820:2')의 세 번째 칸으로 슈퍼독을 정한다.
        /// 스폰보다 먼저 불러야 고유 파츠가 그 캐릭터 것으로 들어간다.
        /// </summary>
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

            // 네 번째 칸은 로비 스크린샷을 찍을 탭 번호다 ('god:40:0:4' -> 도감).
            // 탭마다 눈으로 확인하려면 지정할 방법이 있어야 한다.
            _shotTab = -1;
            if (parts.Length > 3 && int.TryParse(parts[3], out int tabIdx) && tabIdx >= 0)
                _shotTab = tabIdx;
        }

        private static void HandleLevelUp(int level)
        {
            _panelOpenedReal = Time.realtimeSinceStartup;

            // 레벨업 연출은 1.4초만 떠 있다. 끝나고 찍으면 이미 사라진 뒤라
            // 「연출이 있는지」를 확인할 방법이 없다. 첫 레벨업 순간에 한 장 남긴다.
            // (PanelShotFile 은 3택1 패널을 없앨 때 남은 이름이다. 쓰임을 되살렸다)
            if (_levelShotDone)
                return;
            _levelShotDone = true;
            ScreenCapture.CaptureScreenshot(PanelShotFile);
        }

        /// <summary>가장 가까운 젬을 향해 걷는다. 젬이 없으면 제자리.</summary>
        private static void DriveMoveTowardGems()
        {
            if (_player == null || Time.timeScale <= 0f)
                return;

            CharacterMotor motor = _player.GetComponent<CharacterMotor>();
            if (motor == null)
                return;

            // 파츠 상자가 있으면 그쪽을 먼저 간다. 상자를 안 주우면 화력이 안 늘어
            // 「라운드 곡선」이 아니라 「파츠 3개로 얼마나 버티나」를 재게 된다.
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

            // 주울 것이 없어도 서 있으면 안 된다. 뱀서류에서 정지는 곧 죽음이라,
            // 가만히 선 채로 맞는 시간을 재면 곡선이 실제보다 훨씬 나쁘게 나온다.
            Vector2 goal = Vector2.zero;
            if (nearest != null)
            {
                Vector3 dir = nearest.transform.position - pos;
                goal = new Vector2(dir.x, dir.z);
            }
            Steer(motor, goal, pos);
        }

        /// <summary>적에게서 붙는 거리 — 이 안의 적은 밀어내는 힘으로 친다.</summary>
        private const float AvoidRadius = 4.5f;

        /// <summary>회피가 목표를 이기는 정도. 1 보다 크면 붙은 적이 있을 때 도망이 우선한다.</summary>
        private const float AvoidWeight = 2.2f;

        /// <summary>
        /// 목표(젬·상자)로 가되 붙은 적은 피한다.
        ///
        /// [사고] 예전엔 가장 가까운 젬으로 직진만 했다. 젬은 적 시체 위에 떨어지므로
        /// **적 무리 한가운데로 걸어 들어가는** 조작이었고, 그렇게 잰 생존 시간으로
        /// 라운드 곡선을 낮추면 실제 플레이어에겐 한없이 쉬운 게임이 된다.
        /// 회피는 밸런스 실측의 전제다.
        /// </summary>
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

                // 가까울수록 세게 민다. 선형이면 바로 앞의 적과 반경 끝의 적이 비슷해진다.
                push += flat / dist * ((AvoidRadius - dist) / AvoidRadius);
            }

            Vector2 axis = dir + push * AvoidWeight;
            if (axis.sqrMagnitude < 0.01f)
            {
                // 사방이 막혔다. 가장 얕은 쪽으로라도 계속 움직인다.
                axis = push.sqrMagnitude > 0.0001f ? push : Vector2.right;
            }
            motor.MoveAxis = axis.normalized;
        }

        /// <summary>레벨업 패널이 뜨면 1초 보여주고(첫 번은 스크린샷) 첫 카드를 누른다.</summary>
        /// <summary>
        /// 레벨업 3택1 이 사라져서 더 이상 클릭할 패널이 없다.
        /// 성장은 파츠 획득으로만 일어나므로 하네스는 그냥 지나간다.
        /// </summary>
        private static void HandleAugmentPanel()
        {
            // 레벨업 3택1 이 사라져 클릭할 패널이 없다. 성장은 파츠 획득으로만 일어난다.
        }

        /// <summary>테스트용 파츠 강제 착용. 등급은 전설(옵션 3칸)로 줘서 옵션 경로까지 태운다.</summary>
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

        /// <summary>
        /// 셰이더가 깨진 렌더러를 찾는다. Unity 는 이걸 마젠타로 그리지만 로그에는 아무 것도 안 남겨,
        /// 스크린샷을 눈으로 보기 전엔 존재조차 모른다.
        /// </summary>
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

        /// <summary>
        /// 카메라·플레이어·바닥이 어디에 있는지. 화면이 비었을 때 「무엇이 안 보이나」가 아니라
        /// 「무엇이 어디 있나」를 봐야 원인이 한 번에 갈린다.
        /// </summary>
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

        /// <summary>
        /// 라운드가 바뀔 때마다 한 줄씩 남긴다. 마지막 상태만 보면 어느 라운드에서 무너졌는지 알 수 없다.
        /// </summary>
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
            _samples.Add(string.Format(
                "| {0} | {1:0} | {2} | {3} | {4} | {5} | {6:0} | {7:0} |",
                r, elapsed,
                sp != null ? sp.KillCount : -1,
                pg != null ? pg.Level : -1,
                sp != null ? sp.Active.Count : -1,
                pl != null ? pl.EquippedCount : -1,
                so != null ? so.AttackPower : -1f,
                hpPct));
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
                // [사고] 하네스 타이머로 런이 끝나면 GameFlow.EndGame 이 안 불려 기록이 저장되지 않았다.
                // 실제 플레이는 항상 사망/클리어로 끝나므로 게임은 정상인데, 측정만 그 경로를 안 탄다.
                //
                // 리포트를 **쓰기 전에** 닫아야 한다. 중간에 닫으면 결과 화면 상태를 찍는 줄이
                // 이미 지나간 뒤라 항상 비어 나온다.
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
                // [진단] 세로 화면비로 바꾸니 개가 두 마리 보였다. 「몇 마리인가」를 세지 않으면
                // 스크린샷을 눈으로 헤아리게 된다. 살아 있는 Damageable 을 팀별로 이름과 함께 찍는다.
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
                        if (sb2.Length > 0) sb2.Append(", ");
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
                    report.AppendLine("| 라운드 | 초 | 킬 | Lv | 생존적 | 파츠 | 공격력 | HP% |");
                    report.AppendLine("|---|---|---|---|---|---|---|---|");
                    for (int i = 0; i < _samples.Count; i++)
                        report.AppendLine(_samples[i]);
                    report.AppendLine();
                }
                // 보스는 지나간 사건이라 살아 있는 오브젝트로 찾으면 안 된다.
                // 등장/처치 카운터와 클리어 플래그를 함께 찍어 세 경우를 가른다:
                // 안 나옴 / 나왔는데 못 잡음 / 잡았는데 클리어가 안 걸림.
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

                // 아군 오사는 「가끔 순간사」로만 보여 재현이 안 되면 놓친다. 매 런 찍는다.
                report.AppendLine(Damageable.FriendlyFireHits == 0
                    ? "[자해] 없음"
                    : $"[자해] {Damageable.FriendlyFireHits}회 · 최대 {Damageable.FriendlyFireWorst:0.0} · {Damageable.FriendlyFireNote}");

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
