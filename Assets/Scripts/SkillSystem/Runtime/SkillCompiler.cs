using Jinhyeong_SkillSystem.BT;

namespace Jinhyeong_SkillSystem
{

    public static class SkillCompiler
    {
        public static CompiledSkill Compile(SkillDefinition def, int level)
        {
            CompiledSkill c = new CompiledSkill
            {
                Source = def,
                Level = level,
                LevelData = def != null ? def.GetLevel(level) : null,
            };

            if (def == null)
                return c;

            c.Root = BTBuilder.Build(def.Root);
            return c;
        }
    }
}
