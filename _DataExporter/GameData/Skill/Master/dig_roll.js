/** 뒹굴기 — 춤추는 동안 0.5초마다 반경 1.5m 공격력 30% 피해 (상시 장판) */
module.exports = {
    id: 2005,
    name: 'Dig Roll',
    desc: '춤추는 동안 0.5초마다 반경 1.5m 공격력 30% 피해 (상시 장판)',
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
