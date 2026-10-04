using System;
using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_SkillSystem.BT;

namespace Jinhyeong_SkillSystem
{

    public class SkillObject : BaseBehaviour
    {
        public ESkillTeam Team
        {
            get
            {
                Damageable d = GetComponent<Damageable>();
                return d != null ? d.Team : ESkillTeam.Neutral;
            }
        }

        public float AttackPower = CommonConfig.Player.AttackPower;

        public float OutgoingDamageMultiplier = 1f;
        public float AttackSpeedMultiplier = 1f;
        public bool Stunned = false;

        [Tooltip("스킬이 발사되는 기준 위치. 비우면 캐스터 위치 + MuzzleHeight로 폴백.")]
        public Transform Muzzle;

        public Vector3 MuzzlePosition
        {
            get { return Muzzle != null ? Muzzle.position : transform.position + Vector3.up * CommonConfig.Skill.MuzzleHeight; }
        }

        [Tooltip("장착할 스킬 로드아웃 SO. Start 시점에 컴파일.")]
        public SkillLoadout Loadout;

        private readonly List<CompiledSkill> _autoSkills = new List<CompiledSkill>(4);
        private readonly Dictionary<KeyCode, CompiledSkill> _slotSkills = new Dictionary<KeyCode, CompiledSkill>(8);
        private readonly SkillContext _ctx = new SkillContext();

        private bool _attackPending;
        private bool _ready;

        private async void Start()
        {
            if (SkillRegistry.IsLoaded == false)
            {
                try
                {
                    await SkillRegistry.LoadAsync();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SkillObject] SkillRegistry load failed: {e.Message}");
                    return;
                }
            }

            try
            {
                await SkillRegistry.PreloadVisualsAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SkillObject] VFX 사전로드 실패(폴백으로 진행): {e.Message}");
            }

            EquipAll();
            _ready = true;
        }

        public int AutoSkillCount { get { return _autoSkills.Count; } }

        public void NotifyAttack()
        {
            _attackPending = true;
        }

        public bool TryFireSlot(KeyCode key)
        {
            if (key == KeyCode.None)
                return false;
            if (_slotSkills.TryGetValue(key, out CompiledSkill c) == false)
                return false;
            TickSkill(c, true, true);
            return true;
        }

        public bool AddAutoSkill(int skillId, int level)
        {
            SkillDefinition def = SkillRegistry.Get(skillId);
            if (def == null)
            {
                Debug.LogWarning($"[SkillObject] AddAutoSkill: skill_id={skillId} not in registry");
                return false;
            }

            int lv = Mathf.Clamp(level, 1, Mathf.Max(1, def.Meta != null ? def.Meta.MaxLevel : 1));
            _autoSkills.Add(SkillCompiler.Compile(def, lv));
            return true;
        }

        public bool AddPartSkill(int skillId, SkillModifier modifier)
        {
            if (AddAutoSkill(skillId, 1) == false)
                return false;
            CompiledSkill c = _autoSkills[_autoSkills.Count - 1];
            c.Modifier = modifier != null ? modifier : SkillModifier.None;
            c.FromPart = true;
            return true;
        }

        public bool SetPartModifier(int skillId, SkillModifier modifier)
        {
            for (int i = 0; i < _autoSkills.Count; i++)
            {
                CompiledSkill c = _autoSkills[i];
                if (c.Source == null || c.Source.Meta == null || c.Source.Meta.Id != skillId)
                    continue;
                c.Modifier = modifier != null ? modifier : SkillModifier.None;
                return true;
            }
            return false;
        }

        public bool RemoveAutoSkill(int skillId)
        {
            for (int i = _autoSkills.Count - 1; i >= 0; i--)
            {
                CompiledSkill c = _autoSkills[i];
                if (c.Source != null && c.Source.Meta != null && c.Source.Meta.Id == skillId)
                {
                    _autoSkills.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public bool HasSkill(int skillId)
        {
            for (int i = 0; i < _autoSkills.Count; i++)
            {
                if (_autoSkills[i].Source != null && _autoSkills[i].Source.Meta != null
                    && _autoSkills[i].Source.Meta.Id == skillId)
                    return true;
            }
            foreach (CompiledSkill c in _slotSkills.Values)
            {
                if (c.Source != null && c.Source.Meta != null && c.Source.Meta.Id == skillId)
                    return true;
            }
            return false;
        }

        public void EquipAll()
        {
            for (int i = _autoSkills.Count - 1; i >= 0; i--)
            {
                if (_autoSkills[i] != null && _autoSkills[i].FromPart)
                    continue;
                _autoSkills.RemoveAt(i);
            }
            _slotSkills.Clear();

            if (Loadout == null || Loadout.Entries == null)
                return;

            for (int i = 0; i < Loadout.Entries.Count; i++)
            {
                EquippedSkillEntry setup = Loadout.Entries[i];
                if (setup == null)
                    continue;

                SkillDefinition def = SkillRegistry.Get(setup.SkillId);
                if (def == null)
                {
                    Debug.LogWarning($"[SkillObject] skill_id={setup.SkillId} not in registry — skipping");
                    continue;
                }

                int lv = Mathf.Clamp(setup.Level, 1, Mathf.Max(1, def.Meta != null ? def.Meta.MaxLevel : 1));
                CompiledSkill c = SkillCompiler.Compile(def, lv);

                if (setup.SlotKey != KeyCode.None)
                {
                    if (_slotSkills.ContainsKey(setup.SlotKey))
                    {
                        Debug.LogWarning($"[SkillObject] SlotKey '{setup.SlotKey}' 중복 — skill_id={setup.SkillId} 무시");
                        continue;
                    }
                    _slotSkills.Add(setup.SlotKey, c);
                }
                else
                {
                    _autoSkills.Add(c);
                }
            }
        }

        private void Update()
        {
            if (_ready == false)
                return;

            if (Stunned == false)
            {
                for (int i = 0; i < _autoSkills.Count; i++)
                {
                    TickSkill(_autoSkills[i], _attackPending, false);
                }
            }

            _attackPending = false;
        }

        private void TickSkill(CompiledSkill c, bool attackPending, bool manualCast)
        {
            if (Stunned)
                return;
            if (c == null || c.Root == null)
                return;

            _ctx.Reset(this, c.Level, c.LevelData, 0);
            _ctx.Modifier = c.Modifier != null ? c.Modifier : SkillModifier.None;
            _ctx.AttackPending = attackPending;
            _ctx.ManualCast = manualCast;
            c.Root.Tick(_ctx);
        }
    }
}
