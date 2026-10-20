using System;
using System.Collections.Generic;
using Translation;
using Translation.Unity;
using UnityEngine;

namespace Jinhyeong_Common
{
    // 번역 한 곳 (com.translation.unity). 기본 한국어, 설정에서 영어
    // - UI 글자: Loc.T("키", "한국어") — 한국어는 원문이자 번역이 없을 때 쓰는 글자. gen_loc_source.py 가 이 호출을 모아 원문 표를 만든다
    // - 데이터 표: 레지스트리를 읽은 뒤 ApplyTables 가 이름 · 설명을 지금 언어로 덮어쓴다 (원문은 처음 한 번 따로 둔다)
    // - 결과 번들: Resources/Localization (Korean.json · English.json · manifest.json · LocalKey.csv)
    public static class Loc
    {
        public const string Korean = "Korean";
        public const string English = "English";

        private static bool _booted;
        private static readonly Dictionary<object, string[]> _source = new Dictionary<object, string[]>();

        public static event Action OnChanged;

        public static string Language
        {
            get { return TranslationRuntime.IsLoaded ? TranslationRuntime.LanguageId : Korean; }
        }

        public static void Boot()
        {
            if (_booted)
                return;
            _booted = true;
            TranslationRuntime.MissingKeyPolicy = MissingKeyPolicy.ReturnEmpty;
            TranslationBootstrap.StartFromResources("Localization/LocalKey", "Localization");
            TranslationRuntime.OnLanguageChanged += HandleChanged;
        }

        public static void SetLanguage(string id)
        {
            Boot();
            TranslationRuntime.SetLanguage(id);
        }

        private static void HandleChanged(string id)
        {
            ApplyTables();
            if (OnChanged != null)
                OnChanged.Invoke();
        }

        public static string T(string key, string korean)
        {
            if (TranslationRuntime.IsLoaded == false || string.IsNullOrEmpty(key))
                return korean;
            string v = TranslationRuntime.Get(key);
            return string.IsNullOrEmpty(v) ? korean : v;
        }

        public static string F(string key, string korean, params object[] args)
        {
            return string.Format(T(key, korean), args);
        }

        private static string Table(string table, string column, int id, string korean)
        {
            if (TranslationRuntime.IsLoaded == false)
                return korean;
            string v = TranslationRuntime.Get(table, column, id.ToString());
            return string.IsNullOrEmpty(v) ? korean : v;
        }

        // 행마다 원문을 처음 한 번 기억해 두고, 언어가 바뀔 때마다 원문 기준으로 다시 덮는다
        private static string[] SourceOf(object row, params string[] korean)
        {
            if (_source.TryGetValue(row, out string[] s) == false)
            {
                s = korean;
                _source[row] = s;
            }
            return s;
        }

        public static void ApplyTables()
        {
            Boot();
            foreach (Jinhyeong_SkillSystem.PartData p in Jinhyeong_SkillSystem.PartRegistry.All)
            {
                string[] s = SourceOf(p, p.DisplayName, p.Description);
                p.DisplayName = Table("Part", "displayName", p.Id, s[0]);
                p.Description = Table("Part", "desc", p.Id, s[1]);
            }
            foreach (Jinhyeong_SkillSystem.PartOptionData o in Jinhyeong_SkillSystem.PartOptionRegistry.All)
            {
                string[] s = SourceOf(o, o.DisplayName, o.Description);
                o.DisplayName = Table("PartOption", "displayName", o.Id, s[0]);
                o.Description = Table("PartOption", "desc", o.Id, s[1]);
            }
            foreach (Jinhyeong_SkillSystem.SynergyData y in Jinhyeong_SkillSystem.SynergyRegistry.All)
            {
                string[] s = SourceOf(y, y.DisplayName, y.Description);
                y.DisplayName = Table("Synergy", "displayName", y.Id, s[0]);
                y.Description = Table("Synergy", "desc", y.Id, s[1]);
            }
            foreach (Jinhyeong_SkillSystem.RoundData r in Jinhyeong_SkillSystem.RoundRegistry.All)
            {
                string[] s = SourceOf(r, r.Banner);
                r.Banner = string.IsNullOrEmpty(s[0]) ? s[0] : Table("Round", "banner", r.Id, s[0]);
            }
            foreach (Jinhyeong_SkillSystem.WaveEventData w in Jinhyeong_SkillSystem.WaveEventRegistry.All)
            {
                string[] s = SourceOf(w, w.Banner);
                w.Banner = string.IsNullOrEmpty(s[0]) ? s[0] : Table("WaveEvent", "banner", w.Id, s[0]);
            }
            foreach (Jinhyeong_SkillSystem.MapData m in Jinhyeong_SkillSystem.MapRegistry.All)
            {
                string[] s = SourceOf(m, m.DisplayName, m.Boss, m.Description);
                m.DisplayName = Table("Map", "displayName", m.Id, s[0]);
                m.Boss = Table("Map", "boss", m.Id, s[1]);
                m.Description = Table("Map", "desc", m.Id, s[2]);
            }
        }

        // 성좌 그래프는 따로 읽힌다 (ConstellationManager) — 노드 이름 · 설명 (ConstellationText 표, gen_loc_source.py)
        public static void ApplyGraph(IList<Jinhyeong_Constellation.ConstellationNode> nodes)
        {
            Boot();
            for (int i = 0; i < nodes.Count; i++)
            {
                Jinhyeong_Constellation.ConstellationNode n = nodes[i];
                string[] s = SourceOf(n, n.DisplayName, n.Description);
                n.DisplayName = Table("ConstellationText", "name", n.Id, s[0]);
                n.Description = Table("ConstellationText", "desc", n.Id, s[1]);
            }
        }
    }
}
