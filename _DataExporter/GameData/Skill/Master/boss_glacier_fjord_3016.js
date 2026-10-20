/** 보스 패턴 서리 장막 — 주위 반경 3.5m 2초 냉기 (맵 glacier_fjord) */
module.exports = {
    id: 3016,
    name: 'boss_glacier_fjord_3016',
    desc: '서리 장막 — 주위 반경 3.5m 2초 냉기',
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
