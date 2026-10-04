namespace Jinhyeong_UI.Framework
{

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

        public static int Interval(EWindowType type)
        {
            return type == EWindowType.Normal ? 2 : 1;
        }

        public static bool CoversBelow(EWindowType type)
        {
            return type == EWindowType.Normal || type == EWindowType.Global;
        }
    }
}
