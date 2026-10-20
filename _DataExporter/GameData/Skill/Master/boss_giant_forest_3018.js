/** 보스 패턴 뿌리 휘감기 — 주위 반경 3m 1.5초 가시 뿌리 (맵 giant_forest) */
module.exports = {
    id: 3018,
    name: 'boss_giant_forest_3018',
    desc: '뿌리 휘감기 — 주위 반경 3m 1.5초 가시 뿌리',
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
