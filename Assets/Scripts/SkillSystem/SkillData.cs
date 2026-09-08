namespace Jinhyeong_SkillSystem
{

    public class SkillData
    {
        public int Id;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public int MaxLevel = 1;

        /// <summary>조립에 쓰인 DB 모듈 경로. 런타임 동작에는 안 쓰고 툴링/로그 추적용이다.</summary>
        public string VisualPath = string.Empty;
        /// <summary>발사 방식 모듈 경로. 로그·검사용 표시 이름이다.</summary>
        public string MethodPath = string.Empty;
    }
}
