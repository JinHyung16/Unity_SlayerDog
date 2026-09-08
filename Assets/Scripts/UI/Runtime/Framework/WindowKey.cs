namespace Jinhyeong_UI.Framework
{

    /// <summary>
    /// 창을 여는 열쇠. **여는 쪽은 경로를 모른다** — 이 키만 쓴다.
    /// 경로 상수는 창 클래스가 하나만 들고 있고(<c>PrefabPath</c>), 같은 경로가 두 곳에 있으면 하나는 반드시 썩는다.
    /// </summary>
    public class WindowKey
    {
        /// <summary>Addressable 주소. 빌더가 프리팹을 이 주소로 등록한다.</summary>
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
