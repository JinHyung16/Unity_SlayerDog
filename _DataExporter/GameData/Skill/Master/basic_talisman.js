/** 추혼부 — 부적이 적을 쫓아가 터지며 반경 1.2m 공격력 130% 피해 + 받는 피해 증가. 0.9초마다 */
module.exports = {
    id: 2205,
    name: 'Soul Talisman',
    desc: '부적이 적을 쫓아가 터지며 반경 1.2m 공격력 130% 피해 + 받는 피해 증가. 0.9초마다',
    maxLevel: 1,
    method: 'projectile/homing',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetNearest',
    params: {
        Range: '@stat.Range'
    }
},

    slots: {
    onHit: {
        type: 'HitArea',
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
