/** 현의 선율 — 오선지 레이저를 1초간 켜서 0.15초마다 공격력 34% 피해. 1.3초마다 */
module.exports = {
    id: 2203,
    name: 'String Beam',
    desc: '오선지 레이저를 1초간 켜서 0.15초마다 공격력 34% 피해. 1.3초마다',
    maxLevel: 1,
    method: 'beam',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetNearestForward',
    params: {
        Range: '@stat.Range',
        HalfAngle: 60
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
