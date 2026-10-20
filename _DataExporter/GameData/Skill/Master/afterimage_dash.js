/** 분신 질주 — 4초간 빨라지고 지나간 자리에 분신이 남아 피해를 준다. 10초마다 */
module.exports = {
    id: 2103,
    name: 'Afterimage',
    desc: '4초간 빨라지고 지나간 자리에 분신이 남아 피해를 준다. 10초마다',
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
