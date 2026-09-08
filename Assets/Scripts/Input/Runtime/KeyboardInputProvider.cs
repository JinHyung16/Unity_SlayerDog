using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_Input
{

    public class KeyboardInputProvider : BaseBehaviour, IInputProvider
    {
        public InputBindings Bindings;

        public Vector2 MoveAxis { get; private set; }
        public int FacingSign { get; private set; }

        private void Update()
        {
            if (Bindings == null)
                return;

            // 이동키만 설정에서 읽는다. 공격·스킬 슬롯은 아직 고를 수 있는 대상이 아니라 에셋 값 그대로다.
            InputSettings.Scheme move = InputSettings.Current;

            float x = 0f;
            float y = 0f;
            if (Input.GetKey(move.Left))
                x -= 1f;
            if (Input.GetKey(move.Right))
                x += 1f;
            if (Input.GetKey(move.Down))
                y -= 1f;
            if (Input.GetKey(move.Up))
                y += 1f;

            Vector2 axis = new Vector2(x, y);
            if (axis.sqrMagnitude > 1f)
                axis.Normalize();

            MoveAxis = axis;

            if (axis.x > 0f)
                FacingSign = 1;
            else if (axis.x < 0f)
                FacingSign = -1;
        }

        public bool GetAttackDown()
        {
            if (Bindings == null)
                return false;
            return Input.GetKeyDown(Bindings.Attack);
        }

        public bool GetSkillSlotDown(KeyCode slotKey)
        {
            return Input.GetKeyDown(slotKey);
        }
    }
}
