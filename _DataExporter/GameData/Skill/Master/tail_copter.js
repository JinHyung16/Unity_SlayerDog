/** 회오리 베기 — 칼날을 돌려 반경 2m 에 0.4초마다 공격력 35% 피해. 8초마다 */
module.exports = {
    id: 2012,
    name: 'Tail Copter',
    desc: '칼날을 돌려 반경 2m 에 0.4초마다 공격력 35% 피해. 8초마다',
    maxLevel: 1,
    method: 'ground_field',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetSelf'
},

    slots: {
    onHit: {
        type: 'HitArea',
        params: {
            Damage: '@stat.Damage',
            Radius: '@stat.Radius',
            DamageInterval: '@stat.DamageInterval'
        }
    },
    onCast: {
        type: 'BuffSelf',
        params: {
            BuffId: '#buff/sprint'
        }
    }
}
};
