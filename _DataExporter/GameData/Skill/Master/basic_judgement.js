/** 심판 — 발밑에 성광을 2초간 깔아 반경 2.2m 에 0.4초마다 공격력 40% 피해. 2초마다 */
module.exports = {
    id: 2206,
    name: 'Judgement',
    desc: '발밑에 성광을 2초간 깔아 반경 2.2m 에 0.4초마다 공격력 40% 피해. 2초마다',
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
