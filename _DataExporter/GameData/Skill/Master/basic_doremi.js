/** 도레미 스케일 — 도 · 레 · 미 세 음이 부채꼴로 연달아 날아가 각 공격력 72% 피해. 1.1초마다 */
module.exports = {
    id: 2204,
    name: 'Do Re Mi',
    desc: '도 · 레 · 미 세 음이 부채꼴로 연달아 날아가 각 공격력 72% 피해. 1.1초마다',
    maxLevel: 1,
    method: 'projectile/burst',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetNearest',
    params: {
        Range: '@stat.Range'
    }
},

    slots: {
    onHit: {
        type: 'HitSingle',
        params: {
            Damage: '@stat.Damage',
            Radius: '@stat.Radius'
        }
    }
}
};
