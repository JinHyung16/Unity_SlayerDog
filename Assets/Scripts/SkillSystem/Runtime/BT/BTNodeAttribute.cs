using System;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.BT
{

    /// <summary>
    /// 이 클래스가 처리하는 노드 타입을 스스로 신고한다.
    ///
    /// [사고] 예전에는 노드 하나를 붙이려면 세 군데를 고쳐야 했다 —
    /// `_enums.js` 의 ESkillNodeType, `BTBuilder` 의 switch, 노드 클래스.
    /// 셋 중 하나만 빠뜨려도 컴파일은 통과하고 **런타임에 조용히 아무 일도 안 일어난다**.
    /// 조용한 실패는 잡는 데 가장 비싼 종류의 버그다.
    ///
    /// 이제 클래스가 자기 타입을 들고 있고 <see cref="BTNodeRegistry"/> 가 리플렉션으로 집는다.
    /// 고칠 곳은 enum 과 클래스 둘뿐이고, 그나마 빠뜨리면 레지스트리가 이름을 대며 경고한다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class BTNodeAttribute : Attribute
    {
        /// <summary>이 클래스가 받는 노드 타입.</summary>
        public readonly ESkillNodeType[] Types;

        /// <summary>
        /// 받기는 하지만 **아직 효과가 없는** 타입.
        ///
        /// [사고] 「어느 클래스가 받는가」와 「실제로 동작하는가」를 같은 것으로 뭉갰다가
        /// 멀쩡한 안전장치를 지울 뻔했다. 예를 들어 DespawnLeafNode 는 DespawnOnWall 을 받아
        /// ctx.DespawnNode 에 써 두지만, 그 값을 읽는 SkillHit 은 AfterTime/AfterHits 만 분기한다.
        /// 데이터가 통과하고 런타임에 아무 일도 안 일어나는, 가장 찾기 힘든 종류의 실패다.
        ///
        /// 그래서 「받는다」와 별개로 「죽어 있다」를 여기에 적는다. 구현하면 이 목록에서 빼면 되고,
        /// 그 순간 익스포터의 차단도 저절로 풀린다.
        /// </summary>
        public ESkillNodeType[] Inert = System.Array.Empty<ESkillNodeType>();

        public BTNodeAttribute(params ESkillNodeType[] types)
        {
            Types = types;
        }
    }
}
