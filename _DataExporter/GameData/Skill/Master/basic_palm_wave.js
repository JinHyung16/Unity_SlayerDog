/** 장풍 — 손바닥에서 기를 내뿜어 반경 2.6m 적에게 공격력 85% 피해 + 둔화. 1.2초마다 */
module.exports = {
    id: 2204,
    name: 'Palm Wave',
    desc: '손바닥에서 기를 내뿜어 반경 2.6m 적에게 공격력 85% 피해 + 둔화. 1.2초마다',
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
