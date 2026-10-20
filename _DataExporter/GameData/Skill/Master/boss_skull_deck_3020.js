/** 보스 패턴 유령 포격 — 포탄을 던져 반경 2.2m 폭발 (맵 skull_deck) */
module.exports = {
    id: 3020,
    name: 'boss_skull_deck_3020',
    desc: '유령 포격 — 포탄을 던져 반경 2.2m 폭발',
    maxLevel: 1,
    method: 'projectile/arc',

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
        type: 'HitArea',
        params: {
            Damage: '@stat.Damage',
            Radius: '@stat.Radius'
        }
    }
}
};
