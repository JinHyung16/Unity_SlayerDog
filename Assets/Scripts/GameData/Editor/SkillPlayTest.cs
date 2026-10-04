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

    [InitializeOnLoad]
    public static class SkillPlayTest
    {
        private const string RequestFile = "Temp/gamedata_playtest.request";
        private const string ActiveFile = "Temp/gamedata_playtest.active";
        private const string ReportFile = "Temp/gamedata_playtest.report";
        private const string ShotFile = "Temp/gamedata_playtest_homing.png";

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
            {
                return;
            }

            if (_running == false)
            {
                Setup();
                return;
            }

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
            Jinhyeong_UI.GameFlow.StartGame();
            Application.runInBackground = true;

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
                d.MaxHp = 100000f;
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
            _ctx.ManualCast = true;
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
