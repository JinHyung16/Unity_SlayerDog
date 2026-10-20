/** 잠의 주문 — 전방 90° 적을 3초간 크게 느리게 만든다 (피해 없음). 6초마다 */
module.exports = {
    id: 2004,
    name: 'Yawn',
    desc: '전방 90° 적을 3초간 크게 느리게 만든다 (피해 없음). 6초마다',
    maxLevel: 1,
    method: 'instant',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetCone',
    params: {
        Range: '@stat.Range',
        HalfAngle: 45
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
