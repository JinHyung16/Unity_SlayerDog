/** 꼬리 흔들기 — 현의 파동을 날려 적을 꿰뚫으며 공격력 120% 피해. 1.5초마다 */
module.exports = {
    id: 2009,
    name: 'Tail Swipe',
    desc: '현의 파동을 날려 적을 꿰뚫으며 공격력 120% 피해. 1.5초마다',
    maxLevel: 1,
    method: 'projectile/piercing',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetNearestForward',
    params: {
        Range: '@stat.Range',
        HalfAngle: 70
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
