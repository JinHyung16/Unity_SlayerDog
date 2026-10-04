namespace Jinhyeong_UI.Framework
{

    public interface IBaseWindowUpdate
    {
        void OnUpdate();
    }

    public interface IBaseWindowUpdatePerSec
    {
        void OnUpdatePerSec();
    }

    public interface IBaseWindowLateUpdate
    {
        void OnLateUpdate();
    }
}
