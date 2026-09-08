/** 냄새 추적 — 가장 먼 적을 유도 추적해 공격력 130% 피해 + 받는 피해 증가. 2초마다 */
module.exports = {
    id: 2014,
    name: 'Scent Track',
    desc: '가장 먼 적을 유도 추적해 공격력 130% 피해 + 받는 피해 증가. 2초마다',
    maxLevel: 1,
    method: 'projectile/homing',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetFarthest',
    params: {
        Range: '@stat.Range'
    }
},

    slots: {
    onHit: {
        type: 'HitSingle',
        params: {
            Damage: '@stat.Damage',
            Radius: '@stat.Radius'
        }
    },
    onDebuff: {
        type: 'DebuffOnHit',
        params: {
            DebuffId: '#debuff/expose'
        }
    }
}
};
