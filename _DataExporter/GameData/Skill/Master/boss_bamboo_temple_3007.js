/** 보스 패턴 나한 장풍 — 주위 반경 3.5m 1.5초 기운 (맵 bamboo_temple) */
module.exports = {
    id: 3007,
    name: 'boss_bamboo_temple_3007',
    desc: '나한 장풍 — 주위 반경 3.5m 1.5초 기운',
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
