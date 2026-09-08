namespace Jinhyeong_UI.Framework
{

    /// <summary>
    /// 중앙 틱 옵트인. 창마다 <c>Update()</c> 를 갖지 않는다 —
    /// <see cref="UIManagement"/> 가 **열린 창만** 돌며 부른다. 닫힌 창은 목록에 없다.
    /// </summary>
    public interface IBaseWindowUpdate
    {
        void OnUpdate();
    }

    /// <summary>초당 1회. 남은 시간 표시처럼 매 프레임이 필요 없는 것.</summary>
    public interface IBaseWindowUpdatePerSec
    {
        void OnUpdatePerSec();
    }

    /// <summary>후처리. 월드 추종 등.</summary>
    public interface IBaseWindowLateUpdate
    {
        void OnLateUpdate();
    }
}
