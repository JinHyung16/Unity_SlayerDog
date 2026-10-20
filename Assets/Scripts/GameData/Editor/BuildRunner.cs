using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Jinhyeong_GameData.Editor
{
    // 윈도우 빌드 한 번에 (TASKS R-2). 어드레서블 콘텐츠를 먼저 굽고 Builds/SlayerDog_<버전>/SlayerDog.exe 로 낸다
    // 버전 = Player Settings 의 bundleVersion. 에디터 태스크 buildwin 이나 메뉴로 부른다
    public static class BuildRunner
    {
        [MenuItem("Tools/SlayerDog/윈도우 빌드", priority = 30)]
        public static void BuildWindows()
        {
            string version = PlayerSettings.bundleVersion;
            string dir = Path.Combine("Builds", "SlayerDog_" + version);
            Directory.CreateDirectory(dir);

            // 어드레서블 콘텐츠 먼저 (실패하면 콘솔에 오류가 남고 플레이어 빌드에서 번들이 빠진다)
            EnsureGroupsBuildable();
            AddressableAssetSettings.BuildPlayerContent();

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = Path.Combine(dir, "SlayerDog.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(opt);
            BuildSummary s = report.summary;
            // 도스고딕 MIT 라이선스 — 배포본에 고지문을 같이 둔다
            if (s.result == BuildResult.Succeeded)
            {
                File.Copy("Assets/_Art/Fonts/DOSGothic/LICENSE.txt", Path.Combine(dir, "LICENSE_Font_DOSGothic.txt"), true);
                // 배포본에 넣지 않는 Burst 디버그 정보 — 스팀에 올릴 폴더를 그대로 쓸 수 있게 지운다
                string burstDebug = Path.Combine(dir, "SlayerDog_BurstDebugInformation_DoNotShip");
                if (Directory.Exists(burstDebug))
                    Directory.Delete(burstDebug, true);
            }
            Debug.Log($"[BuildRunner] {s.result} — {opt.locationPathName} · {s.totalSize / (1024 * 1024)}MB · {s.totalTime.TotalSeconds:0}초 · 오류 {s.totalErrors}");
        }

        // 스키마가 없는 그룹(Art2DBuilder 가 만든 Skill · Character)은 에디터에서는 읽혀도 플레이어 카탈로그에서 빠진다
        // — 번들 · 업데이트 스키마를 로컬 경로로 채운다
        private static void EnsureGroupsBuildable()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return;
            foreach (AddressableAssetGroup g in settings.groups)
            {
                if (g == null || g.ReadOnly)
                    continue;
                if (g.HasSchema<BundledAssetGroupSchema>() == false)
                {
                    BundledAssetGroupSchema b = g.AddSchema<BundledAssetGroupSchema>();
                    b.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                    b.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
                    b.IncludeInBuild = true;
                    Debug.Log($"[BuildRunner] '{g.Name}' 그룹에 번들 스키마를 붙였다");
                }
                if (g.HasSchema<ContentUpdateGroupSchema>() == false)
                    g.AddSchema<ContentUpdateGroupSchema>();
            }
            AssetDatabase.SaveAssets();
        }
    }
}
