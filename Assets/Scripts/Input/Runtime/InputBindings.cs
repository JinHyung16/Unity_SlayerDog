using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_Input
{

    [CreateAssetMenu(menuName = "Jinhyeong/Input/Input Bindings", fileName = "InputBindings")]
    public class InputBindings : ScriptableObject
    {
        /// <summary>
        /// 이동키의 **기본값**. 실제로 무엇이 눌리는지는 <see cref="InputSettings"/> 가 정한다 —
        /// 사용자가 방향키를 골랐는데 여기 값이 그대로 먹으면 설정이 없는 것과 같다.
        /// 여기 값은 프리셋을 아직 안 고른 첫 실행의 모양을 설명할 뿐이다.
        /// </summary>
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
