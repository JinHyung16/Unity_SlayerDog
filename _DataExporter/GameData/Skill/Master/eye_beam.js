/** 개눈빛 레이저 — 전방으로 스포트라이트를 1.5초간 발사, 0.2초마다 공격력 25% 피해. 4초마다 */
module.exports = {
    id: 2013,
    name: 'Eye Beam',
    desc: '전방으로 스포트라이트를 1.5초간 발사, 0.2초마다 공격력 25% 피해. 4초마다',
    maxLevel: 1,
    method: 'beam',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetNearestForward',
    params: {
        Range: '@stat.Range',
        HalfAngle: 45
    }
},

    slots: {
    onHit: {
        type: 'HitBeam',
        params: {
            Damage: '@stat.Damage',
            Length: '@stat.Length',
            Width: '@stat.Width',
            DamageInterval: '@stat.DamageInterval'
        }
    }
}
};
