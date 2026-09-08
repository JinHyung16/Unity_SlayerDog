namespace Jinhyeong_UI.Framework
{

    /// <summary>
    /// 대역 → sortingOrder. 표는 여기 한 곳에만 있다 (UIFramework.md 「밴드」).
    /// 창은 자기 숫자를 정하지 않는다 — <see cref="UIManagement"/> 가 최소값에 여는 순서×간격을 더한다.
    /// </summary>
    public static class WindowDepth
    {
        public static int Min(EWindowType type)
        {
            switch (type)
            {
                case EWindowType.Passive: return 0;
                case EWindowType.PassiveClosable: return 50;
                case EWindowType.Normal: return 100;
                case EWindowType.LowDepth: return 300;
                case EWindowType.Popup: return 400;
                case EWindowType.Global: return 600;
                case EWindowType.Tutorial: return 700;
                case EWindowType.Top: return 800;
                case EWindowType.SystemPopup: return 900;
                default: return 100;
            }
        }

        /// <summary>같은 대역 안에서 창 하나가 차지하는 층 수. Normal 은 안에 팝업 자리를 하나 둔다.</summary>
        public static int Interval(EWindowType type)
        {
            return type == EWindowType.Normal ? 2 : 1;
        }

        /// <summary>전체 크기로 열리면 아래 창의 캔버스를 꺼도 되는 대역.</summary>
        public static bool CoversBelow(EWindowType type)
        {
            return type == EWindowType.Normal || type == EWindowType.Global;
        }
    }
}
