/** 회전 도끼 — 도끼를 1.2초간 휘돌려 반경 1.8m 에 0.3초마다 공격력 38% 피해. 1.6초마다 */
module.exports = {
    id: 2208,
    name: 'Axe Spin',
    desc: '도끼를 1.2초간 휘돌려 반경 1.8m 에 0.3초마다 공격력 38% 피해. 1.6초마다',
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
