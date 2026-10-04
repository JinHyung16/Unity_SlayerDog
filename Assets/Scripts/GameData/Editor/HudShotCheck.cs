using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using Jinhyeong_AI;
using Jinhyeong_Character;
using Jinhyeong_Managers;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_GameData.Editor
{

    [InitializeOnLoad]
    public static class HudShotCheck
    {
        private const string RequestFile = "Temp/hud_shot.request";
        private const string ActiveFile = "Temp/hud_shot.active";
        private const string ReportFile = "Temp/hud_shot.report";
        private const string ShotFile = "Temp/hud_shot.png";

        private const float WarmupSeconds = 12f;

        private static double _nextPoll;
        private static bool _spawned;
        private static bool _shotRequested;
        private static float _startTime;

        static HudShotCheck()
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

            if (_shotRequested == false && Time.time - _startTime >= WarmupSeconds)
            {
                ScreenCapture.CaptureScreenshot(ShotFile);
                _shotRequested = true;
                return;
            }

            if (_shotRequested && Time.time - _startTime >= WarmupSeconds + 2f)
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
            Player player = go.GetComponent<Player>();

            SkillLoadout lo = ScriptableObject.CreateInstance<SkillLoadout>();
            lo.Entries = new System.Collections.Generic.List<EquippedSkillEntry>
            {
                new EquippedSkillEntry { SkillId = 1001, Level = 1, SlotKey = KeyCode.Q },
                new EquippedSkillEntry { SkillId = 1004, Level = 1, SlotKey = KeyCode.W },
            };
            if (player != null && player.Skills != null)
            {
                player.Skills.Loadout = lo;
                player.Skills.EquipAll();
            }

            Jinhyeong_UI.GameFlow.StartGame();

            _startTime = Time.time;
            _spawned = true;
        }

        private static void Finish(string error)
        {
            EditorApplication.update -= Drive;

            StringBuilder report = new StringBuilder(512);
            if (string.IsNullOrEmpty(error) == false)
            {
                report.AppendLine($"[중단] {error}");
            }
            else
            {
                EnemySpawner spawner = EnemySpawner.Instance;
                WaveDirector wave = WaveDirector.Instance;
                report.AppendLine("=== HUD 스크린샷 ===");
                report.AppendLine($"경과={(wave != null ? wave.Elapsed : 0f):0.0}s 적={(spawner != null ? spawner.Active.Count : -1)} 킬={(spawner != null ? spawner.KillCount : -1)}");

                Jinhyeong_UI.SurvivorHud hud = Jinhyeong_UI.SurvivorHud.Instance;
                report.AppendLine($"HUD 인스턴스={(hud != null ? "OK" : "NULL")}");

                TMP_Text[] texts = hud != null ? hud.GetComponentsInChildren<TMP_Text>(true) : new TMP_Text[0];
                for (int i = 0; i < texts.Length; i++)
                    report.AppendLine($"  텍스트 '{texts[i].name}' = \"{texts[i].text}\"");
            }

            File.WriteAllText(ReportFile, report.ToString(), Encoding.UTF8);
            if (File.Exists(ActiveFile))
                File.Delete(ActiveFile);

            EditorApplication.isPlaying = false;
        }
    }
}
