namespace Jinhyeong_JsonParsing
{
    /// <summary>DataTable 한 행에서 자신을 채울 수 있는 데이터 객체. 자동 생성 데이터 클래스가 __Parse를 구현한다.</summary>
    public interface IData
    {
        void __Parse(DataTable table, int row);
    }
}
