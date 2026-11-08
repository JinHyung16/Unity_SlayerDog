using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
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
            ApplyPlayerSettings();

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

        private const string IconPath = "Assets/Resources/Lobby/portrait_shiba.png";

        // 출시용 Player Settings 를 코드로 맞춘다 (볼트 스펙 확정표 · 빌드 설정). 빌드마다 먼저 부른다
        // 창 = 런타임 DisplaySettings 기본(1280×720 창 모드)과 같게 — 다르면 첫 실행에 전체 화면으로 떴다가 창으로 바뀐다
        [MenuItem("Tools/SlayerDog/빌드 설정 맞추기", priority = 31)]
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "JinHyung";
            PlayerSettings.productName = "SlayerDog";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.jinhyung.slayerdog");
            // IL2CPP 모듈이 설치돼 있지 않다 — Mono 로 고정
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.defaultIsNativeResolution = false;
            // 해상도는 설정 화면 프리셋으로만 바꾼다
            PlayerSettings.resizableWindow = false;
            PlayerSettings.allowFullscreenSwitch = true;
            // 스팀 실행 두 번 눌러도 창 하나
            PlayerSettings.forceSingleInstance = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.usePlayerLog = true;

            // Unity 6 부터 Personal 도 시작 로고를 끌 수 있다
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null)
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            else
                Debug.LogWarning($"[BuildRunner] 아이콘 '{IconPath}' 가 없다 — 기본 Unity 아이콘으로 나간다");

            SetVSyncAllQualityLevels(1);
            AssetDatabase.SaveAssets();
            Debug.Log("[BuildRunner] Player Settings 맞춤 — 1280×720 창 · Mono · 단일 실행 · 로고 없음 · 아이콘 · VSync");
        }

        // VSync 가 꺼져 있으면 프레임 상한이 없어 GPU 가 끝까지 돈다 (팬 소음 · 찢어짐). 품질 단계 전부 켠다
        private static void SetVSyncAllQualityLevels(int count)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (assets == null || assets.Length == 0)
                return;
            SerializedObject so = new SerializedObject(assets[0]);
            SerializedProperty levels = so.FindProperty("m_QualitySettings");
            for (int i = 0; i < levels.arraySize; i++)
                levels.GetArrayElementAtIndex(i).FindPropertyRelative("vSyncCount").intValue = count;
            so.ApplyModifiedPropertiesWithoutUndo();
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
