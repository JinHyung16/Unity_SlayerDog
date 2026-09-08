/** 왈왈 — 전방 부채꼴 적에게 공격력 100% 피해 + 둔화. 2.5초마다 */
module.exports = {
    id: 2002,
    name: 'Bark Cone',
    desc: '전방 부채꼴 적에게 공격력 100% 피해 + 둔화. 2.5초마다',
    maxLevel: 1,
    method: 'instant',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetCone',
    params: {
        Range: '@stat.Range',
        HalfAngle: 30
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
            DebuffId: '#debuff/chill'
        }
    }
}
};
