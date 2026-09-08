using System.Collections.Generic;

namespace Jinhyeong_JsonParsing
{
    /// <summary>테이블 한 개를 키→데이터 딕셔너리로 적재하는 컨테이너 베이스. 자동 생성된 &lt;Table&gt;DictionaryContainer가 Name/Parse를 구현하고,
    /// 사용자 클래스(&lt;Table&gt;Container)가 그걸 상속해 쓴다. Load로 DataManager에서 채운다.</summary>
    public abstract class DictionaryContainer<TKey, TValue> where TValue : class, IData, IDataKey<TKey>
    {
        private readonly Dictionary<TKey, TValue> _map = new Dictionary<TKey, TValue>();

        /// <summary>적재할 테이블 이름(시트 탭명).</summary>
        public abstract string Name { get; }

        public int Count => _map.Count;
        public IReadOnlyDictionary<TKey, TValue> All => _map;

        /// <summary>한 행을 데이터로 변환. 무효 행이면 null 반환(생성 코드가 키<=0 등 검사).</summary>
        protected abstract TValue Parse(DataTable table, int row);

        public TValue Get(TKey key)
        {
            return _map.TryGetValue(key, out TValue v) ? v : null;
        }

        public bool TryGet(TKey key, out TValue value)
        {
            return _map.TryGetValue(key, out value);
        }

        /// <summary>DataManager에서 Name 테이블을 읽어 전 행을 적재(키 중복 시 마지막 행이 이김).</summary>
        public void Load(DataManager dataManager)
        {
            _map.Clear();
            if (dataManager == null)
                return;
            DataTable table = dataManager.GetTable(Name);
            if (table == null)
                return;
            for (int row = 0; row < table.RowCount; row++)
            {
                TValue value = Parse(table, row);
                if (value != null)
                    _map[value.Key] = value;
            }
        }
    }
}
