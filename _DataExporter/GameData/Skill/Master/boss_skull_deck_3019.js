/** 보스 패턴 유령 산탄 — 산탄 7발 부채꼴 (맵 skull_deck) */
module.exports = {
    id: 3019,
    name: 'boss_skull_deck_3019',
    desc: '유령 산탄 — 산탄 7발 부채꼴',
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
