using System.Collections.Generic;
using Jinhyeong_GameData;

namespace Jinhyeong_SkillSystem.Status
{

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
