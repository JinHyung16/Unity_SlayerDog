using System.IO;
using Translation.Unity.Editor;
using UnityEditor;
using UnityEngine;

namespace Jinhyeong_GameData.Editor
{
    // 번역 파이프라인 (com.translation.unity). 원문 = Resources/GameData (gen_loc_source.py 가 LocalizationData · ConstellationText 를 쓴다)
    // 시트 = Assets/Localize/*.csv (영어 열을 채운다) · 결과 = Resources/Localization
    public static class LocRunner
    {
        private const string SettingsPath = "Assets/Localize/TranslationSettings.asset";
        private const string GameDataDir = "Assets/Resources/GameData";
        private const string OutputDir = "Assets/Resources/Localization";

        public static TranslationSettings EnsureSettings()
        {
            TranslationSettings s = AssetDatabase.LoadAssetAtPath<TranslationSettings>(SettingsPath);
            if (s != null)
                return s;
            Directory.CreateDirectory("Assets/Localize");
            Directory.CreateDirectory(OutputDir);
            AssetDatabase.Refresh();

            s = ScriptableObject.CreateInstance<TranslationSettings>();
            s.SourceLanguage = "Korean";
            s.Languages = new System.Collections.Generic.List<TranslationSettings.Language>
            {
                new TranslationSettings.Language { Id = "Korean", DisplayName = "한국어", Bcp47 = "ko" },
                new TranslationSettings.Language { Id = "English", DisplayName = "English", Bcp47 = "en", FallbackId = "Korean" }
            };
            s.GameDataFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(GameDataDir);
            s.GameDataFormat = TranslationSettings.GameDataType.Json;
            s.OutputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(OutputDir);
            s.Targets = new System.Collections.Generic.List<TranslationSettings.Target>
            {
                new TranslationSettings.Target { Table = "", IdColumn = "Key", SourceTable = "LocalizationData", SourceColumn = "Korean" },
                new TranslationSettings.Target { Table = "Part", IdColumn = "id", Columns = new[] { "displayName", "desc" } },
                new TranslationSettings.Target { Table = "PartOption", IdColumn = "id", Columns = new[] { "displayName", "desc" } },
                new TranslationSettings.Target { Table = "Synergy", IdColumn = "id", Columns = new[] { "displayName", "desc" } },
                new TranslationSettings.Target { Table = "Map", IdColumn = "id", Columns = new[] { "displayName", "boss", "desc" } },
                new TranslationSettings.Target { Table = "ConstellationText", IdColumn = "id", Columns = new[] { "name", "desc" } },
                new TranslationSettings.Target { Table = "Round", IdColumn = "id", Columns = new[] { "banner" } },
                new TranslationSettings.Target { Table = "WaveEvent", IdColumn = "id", Columns = new[] { "banner" } }
            };
            AssetDatabase.CreateAsset(s, SettingsPath);
            AssetDatabase.SaveAssets();
            return s;
        }

        public static void Sync()
        {
            TranslationSettings s = EnsureSettings();
            var r = TranslationEditorRunner.Sync(s, prune: true);
            Debug.Log("[LocRunner] Sync — " + r);
        }

        public static void Export()
        {
            TranslationSettings s = EnsureSettings();
            var r = TranslationEditorRunner.Export(s);
            AssetDatabase.Refresh();
            Debug.Log("[LocRunner] Export — " + r);
        }
    }
}
