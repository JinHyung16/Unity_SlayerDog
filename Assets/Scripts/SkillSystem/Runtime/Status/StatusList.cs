using System.Collections.Generic;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.Status
{

    /// <summary>
    /// 대상 하나가 들고 있는 버프/디버프 목록. Damageable 이 소유하고 매 프레임 Tick 한다.
    ///
    /// 컴포넌트가 아니라서 프리팹을 고칠 필요도, 런타임 AddComponent 도 없다.
    /// 같은 효과가 다시 걸렸을 때의 처리(갱신/중첩/무시)는 DB 의 stackMode 가 정한다 —
    /// 화상은 갱신, 중독은 3중첩 같은 걸 C# 수정 없이 바꾸기 위한 것.
    /// </summary>
    public sealed class StatusList
    {
        private readonly List<ActiveStatus> _active = new List<ActiveStatus>(8);
        private readonly Damageable _damageable;
        private readonly SkillObject _owner;

        public int Count { get { return _active.Count; } }
        public IReadOnlyList<ActiveStatus> Active { get { return _active; } }

        public StatusList(Damageable damageable, SkillObject owner)
        {
            _damageable = damageable;
            _owner = owner;
        }

        public int CountOf(int effectId)
        {
            int count = 0;
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].EffectId == effectId)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 이 종류의 디버프가 지금 걸려 있는지. 상태이상 연계 시너지가 매 타격마다 묻는다 —
        /// id 가 아니라 **종류**로 물어야 「둔화면 무엇이든」 같은 조건이 데이터로 표현된다.
        /// </summary>
        public bool HasDebuffKind(Jinhyeong_GameData.EDebuffType kind)
        {
            if (kind == Jinhyeong_GameData.EDebuffType.None)
                return false;
            for (int i = 0; i < _active.Count; i++)
            {
                ActiveStatus a = _active[i];
                if (a != null && a.EffectKind == ActiveStatus.Kind.Debuff && a.DebuffKind == kind)
                    return true;
            }
            return false;
        }

        public void ApplyBuff(SkillBuffData data, SkillObject source)
        {
            if (data == null)
                return;
            if (TryStack(data.Id, data.StackMode, data.MaxStacks) == false)
                return;

            ActiveStatus status = new ActiveStatus();
            status.Setup(data, source, _damageable, _owner);
            _active.Add(status);
        }

        public void ApplyDebuff(SkillDebuffData data, SkillObject source)
        {
            if (data == null)
                return;
            if (TryStack(data.Id, data.StackMode, data.MaxStacks) == false)
                return;

            ActiveStatus status = new ActiveStatus();
            status.Setup(data, source, _damageable, _owner);
            _active.Add(status);
        }

        /// <summary>새 인스턴스를 추가해야 하면 true. 갱신·무시로 처리했으면 false.</summary>
        private bool TryStack(int effectId, EStatusStackMode mode, int maxStacks)
        {
            int existing = CountOf(effectId);
            if (existing == 0)
                return true;

            switch (mode)
            {
                case EStatusStackMode.Stack:
                    if (existing >= UnityEngine.Mathf.Max(1, maxStacks))
                    {
                        // 상한에 걸렸으면 가장 오래된 것의 시간만 되감는다
                        RefreshOldest(effectId);
                        return false;
                    }
                    return true;

                case EStatusStackMode.Ignore:
                    return false;

                default:
                    RefreshOldest(effectId);
                    return false;
            }
        }

        private void RefreshOldest(int effectId)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].EffectId != effectId)
                    continue;
                _active[i].RefreshDuration();
                return;
            }
        }

        public void Tick()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ActiveStatus status = _active[i];
                if (status.IsExpired)
                {
                    status.End();
                    _active.RemoveAt(i);
                    continue;
                }
                status.Tick();
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _active.Count; i++)
            {
                _active[i].End();
            }
            _active.Clear();
        }
    }
}
