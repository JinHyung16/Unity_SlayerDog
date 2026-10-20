/** 보스 패턴 그림자 베기 — 주위 반경 3m 2초 칼바람 (맵 moon_roof) */
module.exports = {
    id: 3006,
    name: 'boss_moon_roof_3006',
    desc: '그림자 베기 — 주위 반경 3m 2초 칼바람',
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
    }
}
};
