/** 자장가 온음표 — 느린 온음표가 적을 쫓아가 터지며 반경 1.4m 공격력 125% 피해 + 졸음. 1.4초마다 */
module.exports = {
    id: 2205,
    name: 'Lullaby',
    desc: '느린 온음표가 적을 쫓아가 터지며 반경 1.4m 공격력 125% 피해 + 졸음. 1.4초마다',
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
            DebuffId: '#debuff/drowsy'
        }
    }
}
};
