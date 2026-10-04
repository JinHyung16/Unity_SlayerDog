using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_View
{
    // 주인공 3종의 월드 스프라이트 묶음. 프리팹은 하나(OBJ_Player)이고, 판이 시작될 때 고른 주인공으로 갈아 끼운다.
    // 묶음은 Art2DBuilder 가 dog_<키>_* 로 채운다. 순서는 RunSelection.Heroes 와 같다 (Index 1 = 0번 칸).
    public class HeroSkin : BaseBehaviour
    {
        [System.Serializable]
        public class Set
        {
            public string Key = string.Empty;
            public Sprite[] Idle;
            public Sprite[] Walk;
            public Sprite[] IdleUp;
            public Sprite[] WalkUp;
            public Sprite[] IdleSide;
            public Sprite[] WalkSide;
        }

        [SerializeField] private CharacterView _view;
        [SerializeField] private Set[] _sets;

        public void Apply(int heroIndex)
        {
            if (_view == null || _sets == null || _sets.Length == 0)
                return;

            Set s = _sets[Mathf.Clamp(heroIndex - 1, 0, _sets.Length - 1)];
            _view.SetFrameSet(s.Idle, s.Walk, s.IdleUp, s.WalkUp, s.IdleSide, s.WalkSide);
        }
    }
}
