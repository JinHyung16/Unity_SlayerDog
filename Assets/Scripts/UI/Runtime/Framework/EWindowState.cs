namespace Jinhyeong_UI.Framework
{

    public enum EWindowState
    {
        Closed,
        Opening,
        Opened,
    }

    /// <summary>
    /// Close() 가 <c>HandleCanClose()</c> 를 묻는가.
    /// 입력 중처럼 조건부로 닫힘을 막아야 하는 창만 Handle 을 쓴다.
    /// </summary>
    public enum ECloseType
    {
        Close,
        Handle,
    }
}
