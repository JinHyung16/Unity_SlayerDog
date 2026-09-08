using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Jinhyeong_GameData.Editor
{

    /// <summary>
    /// GmarketSans TTF → TMP 폰트 에셋 생성 + 프로젝트 기본 폰트로 지정.
    ///
    /// 한글은 완성형 음절이 11,172자라 정적 아틀라스로 다 굽면 수십 MB 가 된다.
    /// **Dynamic** 모드로 만들면 실제 화면에 나온 글자만 아틀라스에 추가되므로
    /// 어떤 텍스트가 와도 깨지지 않고 용량도 최소다. 뱀서류처럼 텍스트가 가벼운 게임엔 이게 정답이다.
    ///
    /// TMP Settings 의 defaultFontAsset 을 바꾸면 폰트를 지정하지 않은 모든
    /// TextMeshProUGUI(코드 생성 HUD 포함)가 자동으로 이 폰트를 쓴다.
    /// </summary>
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

            // 기본 폰트 교체 — 폰트 미지정 TMP 텍스트 전부가 이걸 쓰게 된다
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

            // 샘플링 90 / 아틀라스 1024², Dynamic — 등장한 글자만 담는다
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

            // Dynamic 아틀라스 텍스처와 머티리얼은 서브에셋으로 같이 저장해야 참조가 살아남는다
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
