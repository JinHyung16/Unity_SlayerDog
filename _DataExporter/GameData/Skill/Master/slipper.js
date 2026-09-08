/** 슬리퍼 투척 — 슬리퍼를 던져 공격력 300% 피해. 5초마다 */
module.exports = {
    id: 2024,
    name: 'Slipper Throw',
    desc: '슬리퍼를 던져 공격력 300% 피해. 5초마다',
    maxLevel: 1,
    method: 'projectile/straight',

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
