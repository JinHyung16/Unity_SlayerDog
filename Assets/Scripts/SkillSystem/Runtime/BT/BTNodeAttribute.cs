using System;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.BT
{

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class BTNodeAttribute : Attribute
    {
        public readonly ESkillNodeType[] Types;

        public ESkillNodeType[] Inert = System.Array.Empty<ESkillNodeType>();

        public BTNodeAttribute(params ESkillNodeType[] types)
        {
            Types = types;
        }
    }
}
