/** 꼬리 채찍 — 꼬리를 후려쳐 직선상 적에게 공격력 140% 피해. 2초마다 */
module.exports = {
    id: 2011,
    name: 'Tail Whip',
    desc: '꼬리를 후려쳐 직선상 적에게 공격력 140% 피해. 2초마다',
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
