using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Jinhyeong_GameData.Editor
{

    public static class FontAssetBuilder
    {
        private const string RequestFile = "Temp/fontbuild.request";
        private const string ReportFile = "Temp/fontbuild.report";

        private const string SourceMedium = "Assets/_Art/Fonts/GmarketSansTTFMedium.ttf";
        private const string SourceBold = "Assets/_Art/Fonts/GmarketSansTTFBold.ttf";
        private const string OutMedium = "Assets/_Art/Fonts/GmarketSans-Medium SDF.asset";
        private const string OutBold = "Assets/_Art/Fonts/GmarketSans-Bold SDF.asset";

        private static double _nextPoll;

        [InitializeOnLoadMethod]
        private static void Hook()
        {
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

            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            File.Delete(RequestFile);
            string report = Build();
            File.WriteAllText(ReportFile, report, Encoding.UTF8);
        }

        [MenuItem("Jinhyeong/GameData/한글 폰트 에셋 생성 + 기본 지정", false, 50)]
        private static void RunMenu()
        {
            Debug.Log(Build());
        }

        public static string Build()
        {
            StringBuilder log = new StringBuilder(512);

            TMP_FontAsset medium = CreateDynamic(SourceMedium, OutMedium, log);
            TMP_FontAsset bold = CreateDynamic(SourceBold, OutBold, log);

            if (medium == null)
                return log.Append("[실패] Medium 폰트 에셋 생성 실패").ToString();

            TMP_Settings settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings == null)
            {
                log.AppendLine("[실패] TMP Settings 를 찾을 수 없습니다");
                return log.ToString();
            }

            SerializedObject so = new SerializedObject(settings);
            SerializedProperty prop = so.FindProperty("m_defaultFontAsset");
            if (prop == null)
            {
                log.AppendLine("[실패] m_defaultFontAsset 프로퍼티 없음 (TMP 버전 확인 필요)");
                return log.ToString();
            }
            prop.objectReferenceValue = medium;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            log.AppendLine($"기본 폰트 = {medium.name}");
            log.AppendLine($"Bold = {(bold != null ? bold.name : "생성 실패")}");
            return log.ToString();
        }

        private static TMP_FontAsset CreateDynamic(string sourcePath, string outPath, StringBuilder log)
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (source == null)
            {
                log.AppendLine($"[실패] 소스 폰트 없음: {sourcePath}");
                return null;
            }

            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath);
            if (existing != null)
            {
                log.AppendLine($"이미 있음: {outPath}");
                return existing;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                source, 90, 9,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                1024, 1024,
                AtlasPopulationMode.Dynamic);

            if (asset == null)
            {
                log.AppendLine($"[실패] CreateFontAsset 실패: {sourcePath}");
                return null;
            }

            asset.name = Path.GetFileNameWithoutExtension(outPath);
            AssetDatabase.CreateAsset(asset, outPath);

            asset.atlasTextures[0].name = asset.name + " Atlas";
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            AssetDatabase.SaveAssets();
            log.AppendLine($"생성: {outPath}");
            return asset;
        }
    }
}
