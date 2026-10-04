using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_Input
{

    [CreateAssetMenu(menuName = "Jinhyeong/Input/Input Bindings", fileName = "InputBindings")]
    public class InputBindings : ScriptableObject
    {
        public KeyCode MoveUp = KeyCode.W;
        public KeyCode MoveDown = KeyCode.S;
        public KeyCode MoveLeft = KeyCode.A;
        public KeyCode MoveRight = KeyCode.D;

        public KeyCode Attack = KeyCode.Mouse0;

        [Serializable]
        public class SkillSlotBinding
        {
            public KeyCode Key = KeyCode.Q;
        }

        public List<SkillSlotBinding> SkillSlots = new List<SkillSlotBinding>
        {
            new SkillSlotBinding { Key = KeyCode.Q },
            new SkillSlotBinding { Key = KeyCode.E },
            new SkillSlotBinding { Key = KeyCode.R },
            new SkillSlotBinding { Key = KeyCode.F },
            new SkillSlotBinding { Key = KeyCode.Z },
            new SkillSlotBinding { Key = KeyCode.X },
            new SkillSlotBinding { Key = KeyCode.C },
            new SkillSlotBinding { Key = KeyCode.V },
        };
    }
}
