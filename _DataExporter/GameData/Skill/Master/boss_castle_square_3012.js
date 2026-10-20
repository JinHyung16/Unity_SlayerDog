/** 보스 패턴 검은 회오리 — 주위 반경 3m 1.5초 칼바람 (맵 castle_square) */
module.exports = {
    id: 3012,
    name: 'boss_castle_square_3012',
    desc: '검은 회오리 — 주위 반경 3m 1.5초 칼바람',
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
