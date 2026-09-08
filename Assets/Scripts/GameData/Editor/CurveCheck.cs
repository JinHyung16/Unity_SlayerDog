using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Jinhyeong_AI;
using Jinhyeong_Character;
using Jinhyeong_Input;
using Jinhyeong_Managers;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_GameData.Editor
{

    /// <summary>
    /// 성장/스폰 곡선 실측 하네스. 5분 게임시간을 돌며 30초마다 표본을 찍는다.
    ///
    /// - 플레이어는 무적(매 프레임 HP 복구) — 사망으로 곡선이 끊기면 후반 표본이 안 나온다
    /// - 시작 로드아웃은 LobbyWindow 기본(1001~1004)과 같게 둔다
    /// - 레벨업 패널은 즉시 첫 카드를 누른다 (표본은 게임시간 Time.time 기준이라 패널 대기는 제외된다)
    /// - 표본: 게임시간 / 레벨 / 누적XP / 킬 / 생존 적 / 정예 / 직전 구간 평균 FPS / 젬 수
    ///
    /// 결과는 Temp/curve.report 에 표로 남긴다.
    /// </summary>
    [InitializeOnLoad]
    public static class CurveCheck
    {
        private const string RequestFile = "Temp/curve.request";
        private const string ActiveFile = "Temp/curve.active";
        private const string ReportFile = "Temp/curve.report";

        private const float RunSeconds = 300f;
        private const float SampleInterval = 30f;

        private static double _nextPoll;
        private static bool _spawned;
        private static float _startGame;
        private static float _nextSample;
        private static bool _panelOpen;
        private static Player _player;
        private static Damageable _hp;
        private static int _picks;

        private static int _frames;
        private static float _frameTime;

        private static readonly HarnessInput _input = new HarnessInput();
        private static readonly List<string> _rows = new List<string>(16);
        private static readonly List<string> _pickedNames = new List<string>(16);

        /// <summary>
        /// 하네스 이동 입력. PlayerController 를 끄면 자동공격(OnAttack 트리거의 원천)까지 죽으므로
        /// 컨트롤러는 살려두고 입력원만 바꿔 끼운다.
        /// </summary>
        private sealed class HarnessInput : IInputProvider
        {
            public Vector2 Axis;
            public Vector2 MoveAxis { get { return Axis; } }
            public int FacingSign { get { return 1; } }
            public bool GetAttackDown() { return false; }
            public bool GetSkillSlotDown(KeyCode slotKey) { return false; }
        }

        static CurveCheck()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode == false)
            {
                EditorApplication.update += WatchRequest;
                return;
            }
            if (File.Exists(ActiveFile) == false)
                return;
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

            if (_spawned == false)
            {
                Setup();
                return;
            }

            if (_hp != null)
            {
                _hp.Hp = _hp.MaxHp;
            }

            DriveMoveTowardGems();
            HandleAugmentPanel();

            if (Time.timeScale > 0f)
            {
                _frames++;
                _frameTime += Time.unscaledDeltaTime;
            }

            float elapsed = Time.time - _startGame;
            if (elapsed >= _nextSample)
            {
                Sample(elapsed);
                _nextSample += SampleInterval;
            }

            if (elapsed >= RunSeconds)
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

            Cysharp.Threading.Tasks.UniTaskExtensions.Forget(
                AddressableManager.Ensure().LoadAsync("obj_enemy"));

            GameObject go = Object.Instantiate(prefab, new Vector3(0f, 0.5f, 0f), Quaternion.identity);
            _player = go.GetComponent<Player>();
            _hp = go.GetComponent<Damageable>();

            SkillLoadout lo = ScriptableObject.CreateInstance<SkillLoadout>();
            lo.Entries = new List<EquippedSkillEntry>
            {
                new EquippedSkillEntry { SkillId = 1001, Level = 1, SlotKey = KeyCode.None },
                new EquippedSkillEntry { SkillId = 1002, Level = 1, SlotKey = KeyCode.None },
                new EquippedSkillEntry { SkillId = 1003, Level = 1, SlotKey = KeyCode.None },
                new EquippedSkillEntry { SkillId = 1004, Level = 1, SlotKey = KeyCode.None },
            };
            if (_player != null && _player.Skills != null)
            {
                _player.Skills.Loadout = lo;
                _player.Skills.EquipAll();
            }

            PlayerController controller = go.GetComponent<PlayerController>();
            if (controller != null)
            {
                System.Reflection.FieldInfo field = typeof(PlayerController).GetField("_input",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                    field.SetValue(controller, _input);
            }

            Jinhyeong_UI.GameFlow.StartGame();

            if (RunProgress.Instance != null)
                RunProgress.Instance.OnLevelUp += HandleLevelUp;

            _startGame = Time.time;
            _nextSample = SampleInterval;
            _rows.Add("t(s) | Lv | 누적XP | 킬 | 생존 | 정예 | 젬 | FPS");
            _rows.Add("---|---|---|---|---|---|---|---");
            _spawned = true;
        }

        private static void HandleLevelUp(int level)
        {
            _panelOpen = true;
        }

        private static void Sample(float elapsed)
        {
            EnemySpawner spawner = EnemySpawner.Instance;
            RunProgress progress = RunProgress.Instance;

            int alive = spawner != null ? spawner.Active.Count : -1;
            int elites = 0;
            if (spawner != null)
            {
                for (int i = 0; i < spawner.Active.Count; i++)
                {
                    if (spawner.Active[i] != null && spawner.Active[i].IsElite)
                        elites++;
                }
            }

            int gems = 0;
            XpGem[] all = Object.FindObjectsByType<XpGem>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].gameObject.activeSelf)
                    gems++;
            }

            // 누적 XP = 지난 레벨들의 XpToNext 합 + 현재 잔여
            float total = 0f;
            int lv = progress != null ? progress.Level : 1;
            for (int l = 1; l < lv; l++)
                total += RunProgress.XpRequired(l);
            total += progress != null ? progress.Xp : 0f;

            float fps = _frameTime > 0f ? _frames / _frameTime : 0f;
            _frames = 0;
            _frameTime = 0f;

            _rows.Add($"{elapsed:0} | {lv} | {total:0} | {(spawner != null ? spawner.KillCount : -1)} | {alive} | {elites} | {gems} | {fps:0}");
        }

        private static void DriveMoveTowardGems()
        {
            if (_player == null || Time.timeScale <= 0f)
                return;

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

            if (nearest == null)
            {
                _input.Axis = Vector2.zero;
                return;
            }

            // 카메라 yaw 0 이므로 월드 XZ 가 곧 입력 축이다
            Vector3 dir = nearest.transform.position - pos;
            Vector2 axis = new Vector2(dir.x, dir.z);
            _input.Axis = axis.sqrMagnitude > 0.01f ? axis.normalized : Vector2.zero;
        }

        /// <summary>
        /// 레벨업 3택1 이 사라져서 더 이상 클릭할 패널이 없다.
        /// 성장은 파츠 획득으로만 일어나므로 하네스는 그냥 지나간다.
        /// </summary>
        private static void HandleAugmentPanel()
        {
            _panelOpen = false;
        }

        private static void Finish(string error)
        {
            EditorApplication.update -= Drive;
            if (RunProgress.Instance != null)
                RunProgress.Instance.OnLevelUp -= HandleLevelUp;

            StringBuilder report = new StringBuilder(2048);
            if (string.IsNullOrEmpty(error) == false)
            {
                report.AppendLine($"[중단] {error}");
            }
            else
            {
                report.AppendLine("=== 성장/스폰 곡선 실측 (무적, 시작 스킬 1001~1004) ===");
                for (int i = 0; i < _rows.Count; i++)
                    report.AppendLine(_rows[i]);
                report.AppendLine($"증강 선택 {_picks}회: {string.Join(", ", _pickedNames)}");
            }

            File.WriteAllText(ReportFile, report.ToString(), Encoding.UTF8);
            if (File.Exists(ActiveFile))
                File.Delete(ActiveFile);

            EditorApplication.isPlaying = false;
        }
    }
}
