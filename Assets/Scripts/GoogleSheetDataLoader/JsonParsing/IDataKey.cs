namespace Jinhyeong_JsonParsing
{
    /// <summary>딕셔너리 컨테이너의 키를 제공하는 데이터 객체. 자동 생성 데이터 클래스(Jinhyeong_GameData.*)가 구현한다.</summary>
    public interface IDataKey<TKey>
    {
        TKey Key { get; }
    }
}
