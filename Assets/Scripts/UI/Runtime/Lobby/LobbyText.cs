using Jinhyeong_GameData;

namespace Jinhyeong_UI.Lobby
{

    /// <summary>로비 화면들이 함께 쓰는 표기. UI 는 «표기»만 한다 — 규칙 계산은 여기 없다.</summary>
    public static class LobbyText
    {
        /// <summary>계열의 노출명. 6종뿐이고 표시에만 쓰여 여기 둔다 — 늘어나면 데이터로 옮긴다.</summary>
        public static string FamilyName(ESkillFamily f)
        {
            switch (f)
            {
                case ESkillFamily.Wave: return "파동";
                case ESkillFamily.Throw: return "투척";
                case ESkillFamily.Field: return "장판";
                case ESkillFamily.Melee: return "근접";
                case ESkillFamily.Gaze: return "시선";
                case ESkillFamily.Aura: return "각성";
                default: return "-";
            }
        }
    }
}
