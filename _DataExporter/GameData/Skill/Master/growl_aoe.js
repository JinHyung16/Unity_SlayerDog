/** 으르렁 — 반경 3m 적에게 공격력 80% 피해 + 2초간 둔화. 3초마다 */
module.exports = {
    id: 2003,
    name: 'Growl',
    desc: '반경 3m 적에게 공격력 80% 피해 + 2초간 둔화. 3초마다',
    maxLevel: 1,
    method: 'instant',

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
            Radius: '@stat.Radius'
        }
    },
    onDebuff: {
        type: 'DebuffOnHit',
        params: {
            DebuffId: '#debuff/chill'
        }
    }
}
};
