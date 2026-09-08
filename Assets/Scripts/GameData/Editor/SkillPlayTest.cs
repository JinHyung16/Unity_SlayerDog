using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Jinhyeong_Common;
using Jinhyeong_Managers;
using Jinhyeong_SkillSystem;
using Jinhyeong_SkillSystem.Skill;
using UnityEditor;
using UnityEngine;

namespace Jinhyeong_GameData.Editor
{

    /// <summary>
    /// 실제 플레이 모드에서 스킬을 하나씩 쏘고 피해량·궤도를 측정한다.
    ///
    /// 전부 Editor 어셈블리라 빌드에는 안 들어간다. 플레이 모드 진입은 도메인 리로드를 동반하므로
    /// 상태는 파일로만 넘긴다 (request -> active -> report).
    ///
    /// 아레나는 실제 씬 원점에서 멀리 떨어뜨린다. 씬의 EnemySpawner 가 뿌린 적이 타게팅에 걸리면
    /// 측정값이 오염되기 때문이다.
    /// </summary>
    [InitializeOnLoad]
    public static class SkillPlayTest
    {
        private const string RequestFile = "Temp/gamedata_playtest.request";
        private const string ActiveFile = "Temp/gamedata_playtest.active";
        private const string ReportFile = "Temp/gamedata_playtest.report";
        private const string ShotFile = "Temp/gamedata_playtest_homing.png";

        // 씬 원점에서 충분히 떨어뜨려 스포너가 뿌린 적과 섞이지 않게 한다
        private static readonly Vector3 ArenaOrigin = new Vector3(0f, 0f, 500f);
        private const float SkillWindow = 2.6f;
        private const int DummyCount = 6;

        private static readonly StringBuilder _report = new StringBuilder(8192);
        private static readonly SkillContext _ctx = new SkillContext();
        private static readonly List<Vector3> _path = new List<Vector3>(64);

        private static GameObject _arena;
        private static SkillObject _caster;
        private static readonly List<Damageable> _dummies = new List<Damageable>(DummyCount);
        private static List<SkillDefinition> _queue;

        private static int _index = -1;
        private static float _phaseStartScaled;
        private static float _phaseStartReal;
        private static int _phaseStartFrame;
        private static float _travelMax;
        private static Vector3 _spawnPos;
        private static bool _running;
        private static bool _shotTaken;
        private static bool _waitingPreload;
        private static float _preloadStart;

        private static double _nextPoll;

        static SkillPlayTest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode == false)
            {
                // 요청 파일을 상시 감시한다. static 생성자는 도메인 리로드 때만 도는데,
                // C# 변경이 없으면 창을 클릭해도 리로드가 안 일어나 요청이 영영 안 잡힌다.
                EditorApplication.update += WatchRequest;
                return;
            }

            if (File.Exists(ActiveFile) == false)
            {
                return;
            }
            EditorApplication.update += Drive;
        }

        private static void WatchRequest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            // 매 프레임 파일을 두드릴 필요는 없다
            if (EditorApplication.timeSinceStartup < _nextPoll)
                return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;

            // 컴파일/임포트 중에는 건드리지 않는다
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            if (File.Exists(RequestFile) == false)
                return;

            // Unity 는 창이 포커스를 받을 때만 에셋을 임포트한다. 포커스 없이 돌리려면 직접 부른다.
            // 스크립트가 바뀌었으면 여기서 리컴파일이 시작되고, 도메인 리로드 후 다음 틱에 이어서 처리된다.
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
            {
                return;
            }

            if (_running == false)
            {
                Setup();
                return;
            }

            // Skill_Root 프리팹이 로드되기 전에 쏘면 전부 스폰 실패로 0 이 찍힌다.
            if (_waitingPreload)
            {
                AddressableManager am = AddressableManager.Instance;
                if (am != null && am.IsLoaded(CommonConfig.Skill.RootPrefabKey))
                {
                    _waitingPreload = false;
                    _index = -1;
                    Advance();
                }
                else if (Time.realtimeSinceStartup - _preloadStart > 30f)
                {
                    Finish($"Addressable 프리로드 실패 — '{CommonConfig.Skill.RootPrefabKey}' 를 못 불러왔습니다");
                }
                return;
            }

            SamplePath();

            // 게임 시간 기준으로 진행한다. timeScale 이 0 이면 영원히 안 끝나므로 실시간 워치독을 건다 —
            // 이게 없으면 "아무것도 안 움직였는데 피해 0" 을 정상 결과처럼 보고하게 된다.
            if (Time.realtimeSinceStartup - _phaseStartReal > SkillWindow * 4f)
            {
                Finish($"게임 시간이 흐르지 않습니다 (timeScale={Time.timeScale}, 경과 프레임={Time.frameCount - _phaseStartFrame})");
                return;
            }

            if (Time.time - _phaseStartScaled < SkillWindow)
            {
                return;
            }

            RecordCurrent();
            Advance();
        }

        private static void Setup()
        {
            // LobbyWindow 게이트가 timeScale=0 으로 잡고 있다. 시작 버튼을 코드로 누른다.
            Jinhyeong_UI.GameFlow.StartGame();
            Application.runInBackground = true;

            // UI 캔버스가 화면을 덮어 스크린샷에 아레나가 안 보인다
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].enabled = false;
            }

            SkillRegistry.Load();
            if (SkillRegistry.IsLoaded == false)
            {
                Finish("SkillRegistry 로드 실패");
                return;
            }

            _arena = new GameObject("_SkillPlayTestArena");
            _arena.transform.position = ArenaOrigin;

            // 캐스터는 +Z 를 본다. 더미는 +X 쪽에 세워서 "직선으로 쏘면 반드시 빗나가는" 배치를 만든다.
            // 이래야 유도(homing)와 직선(straight)이 결과로 구분된다.
            GameObject casterGo = new GameObject("Caster");
            casterGo.transform.SetParent(_arena.transform, false);
            casterGo.transform.forward = Vector3.forward;
            Damageable casterHp = casterGo.AddComponent<Damageable>();
            casterHp.Team = ESkillTeam.Friend;
            casterHp.DestroyOnDeath = false;
            _caster = casterGo.AddComponent<SkillObject>();

            _dummies.Clear();
            for (int i = 0; i < DummyCount; i++)
            {
                GameObject dummy = new GameObject($"Dummy{i}");
                dummy.transform.SetParent(_arena.transform, false);
                dummy.transform.localPosition = new Vector3(6f + i * 1.6f, 0f, 0f);
                Damageable d = dummy.AddComponent<Damageable>();
                d.Team = ESkillTeam.Enemy;
                d.MaxHp = 100000f;   // 죽어서 사라지면 누적 피해를 못 센다
                d.Hp = 100000f;
                d.DestroyOnDeath = false;
                _dummies.Add(d);
            }

            GameObject camGo = new GameObject("TestCamera");
            camGo.transform.SetParent(_arena.transform, false);
            camGo.transform.localPosition = new Vector3(8f, 16f, -6f);
            camGo.transform.localEulerAngles = new Vector3(55f, 0f, 0f);
            Camera cam = camGo.AddComponent<Camera>();
            cam.depth = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.1f);

            _queue = new List<SkillDefinition>(SkillRegistry.All);
            _queue.Sort((a, b) => a.Meta.Id.CompareTo(b.Meta.Id));

            _report.AppendLine("=== 스킬 플레이 검증 ===");
            _report.AppendLine($"캐스터 forward=+Z, 더미 {DummyCount}기는 +X 6~14 (직선 발사는 구조적으로 빗나감)");
            _report.AppendLine();
            _report.AppendLine($"timeScale={Time.timeScale}");
            _report.AppendLine();
            _report.AppendLine("id    이름               launch                 피해   프레임  이동  상태이상  궤도(시작→끝)");

            _running = true;
            _waitingPreload = true;
            _preloadStart = Time.realtimeSinceStartup;
            PreloadAsync();
        }

        private static async void PreloadAsync()
        {
            try
            {
                await SkillRegistry.PreloadVisualsAsync();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SkillPlayTest] Addressable 프리로드 실패: {e.Message}");
            }
        }

        private static void Advance()
        {
            _index++;
            if (_queue == null || _index >= _queue.Count)
            {
                Finish(null);
                return;
            }

            for (int i = 0; i < _dummies.Count; i++)
            {
                Damageable d = _dummies[i];
                if (d == null)
                    continue;

                d.Hp = d.MaxHp;

                // HP만 되돌리면 앞 스킬이 건 DoT가 다음 스킬 창에서 계속 틱해 피해량에 섞인다.
                // (실제로 독탄의 잔여 틱이 빔/자버프 결과로 잡혔다)
                d.Status.Clear();
                d.IncomingDamageMultiplier = 1f;
                d.SpeedMultiplier = 1f;
                d.Shield = 0f;
                d.Stunned = false;
            }

            Damageable casterHp = _caster.GetComponent<Damageable>();
            if (casterHp != null)
                casterHp.Status.Clear();
            _caster.OutgoingDamageMultiplier = 1f;
            _caster.AttackSpeedMultiplier = 1f;

            // 남은 이펙트가 다음 스킬 측정에 섞이지 않게 정리
            SkillConfig[] leftover = Object.FindObjectsByType<SkillConfig>(FindObjectsSortMode.None);
            for (int i = 0; i < leftover.Length; i++)
            {
                if (leftover[i] != null)
                    Object.Destroy(leftover[i].gameObject);
            }

            _path.Clear();
            _travelMax = 0f;
            _spawnPos = Vector3.positiveInfinity;
            _phaseStartScaled = Time.time;
            _phaseStartReal = Time.realtimeSinceStartup;
            _phaseStartFrame = Time.frameCount;

            SkillDefinition def = _queue[_index];
            CompiledSkill compiled = SkillCompiler.Compile(def, 1);
            if (compiled.Root == null)
            {
                return;
            }

            _ctx.Reset(_caster, compiled.Level, compiled.LevelData, 0);
            _ctx.ManualCast = true;   // 트리거/쿨다운 우회 — 발사 자체를 결정적으로 만든다
            compiled.Root.Tick(_ctx);
        }

        private static void SamplePath()
        {
            SkillConfig[] live = Object.FindObjectsByType<SkillConfig>(FindObjectsSortMode.None);
            if (live.Length == 0)
            {
                return;
            }

            Vector3 local = live[0].transform.position - ArenaOrigin;
            if (float.IsInfinity(_spawnPos.x))
            {
                _spawnPos = local;
            }
            _travelMax = Mathf.Max(_travelMax, Vector3.Distance(local, _spawnPos));

            if (_path.Count >= 64)
            {
                return;
            }
            _path.Add(local);

            if (_shotTaken == false && _queue != null && _index >= 0 && _queue[_index].Meta.Id == 1002 && _path.Count == 12)
            {
                _shotTaken = true;
                ScreenCapture.CaptureScreenshot(ShotFile);
            }
        }

        private static void RecordCurrent()
        {
            SkillDefinition def = _queue[_index];

            float damage = 0f;
            Damageable casterDmg = _caster.GetComponent<Damageable>();
            int statusCount = casterDmg != null ? casterDmg.Status.Count : 0;
            for (int i = 0; i < _dummies.Count; i++)
            {
                if (_dummies[i] == null)
                    continue;
                damage += _dummies[i].MaxHp - _dummies[i].Hp;
                statusCount += _dummies[i].Status.Count;
            }

            string trail = "-";
            if (_path.Count > 0)
            {
                Vector3 first = _path[0];
                Vector3 last = _path[_path.Count - 1];
                trail = $"({first.x:0.0},{first.z:0.0})→({last.x:0.0},{last.z:0.0})";
            }

            _report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0,-5} {1,-18} {2,-22} {3,6:0.0} {4,6} {5,6:0.0} {6,5}  {7}",
                def.Meta.Id,
                def.Meta.Name,
                def.Meta.MethodPath.Replace("Skill/Method/", ""),
                damage,
                Time.frameCount - _phaseStartFrame,
                _travelMax,
                statusCount,
                trail));
        }

        private static void Finish(string error)
        {
            EditorApplication.update -= Drive;
            _running = false;

            if (string.IsNullOrEmpty(error) == false)
            {
                _report.AppendLine($"[중단] {error}");
            }

            try
            {
                File.WriteAllText(ReportFile, _report.ToString(), Encoding.UTF8);
            }
            catch (IOException e)
            {
                Debug.LogError($"[SkillPlayTest] 리포트 기록 실패: {e.Message}");
            }

            if (File.Exists(ActiveFile))
            {
                File.Delete(ActiveFile);
            }

            if (_arena != null)
            {
                Object.Destroy(_arena);
            }

            Debug.Log($"[SkillPlayTest] 완료 — {ReportFile}");
            EditorApplication.isPlaying = false;
        }
    }
}
