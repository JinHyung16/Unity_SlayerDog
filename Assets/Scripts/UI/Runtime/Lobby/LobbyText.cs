using Jinhyeong_Common;
using Jinhyeong_GameData;

namespace Jinhyeong_UI.Lobby
{

    public static class LobbyText
    {
        public static string FamilyName(ESkillFamily f)
        {
            switch (f)
            {
                case ESkillFamily.Wave: return Loc.T("Lobby_FamilyWave", "파동");
                case ESkillFamily.Throw: return Loc.T("Lobby_FamilyThrow", "투척");
                case ESkillFamily.Field: return Loc.T("Lobby_FamilyField", "장판");
                case ESkillFamily.Melee: return Loc.T("Lobby_FamilyMelee", "근접");
                case ESkillFamily.Gaze: return Loc.T("Lobby_FamilyGaze", "시선");
                case ESkillFamily.Aura: return Loc.T("Lobby_FamilyAura", "각성");
                default: return "-";
            }
        }
    }
}
