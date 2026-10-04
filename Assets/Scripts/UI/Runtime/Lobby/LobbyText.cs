using Jinhyeong_GameData;

namespace Jinhyeong_UI.Lobby
{

    public static class LobbyText
    {
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
