namespace Jinhyeong_UI.Framework
{

    public class WindowKey
    {
        public string Path { get; private set; }

        public WindowKey(string path)
        {
            Path = path;
        }
    }

    public sealed class WindowKey<T> : WindowKey where T : BaseWindow
    {
        public WindowKey(string path) : base(path)
        {
        }
    }
}
