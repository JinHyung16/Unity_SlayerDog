/** 보스 패턴 타락의 검기 — 검기 3발 부채꼴 (맵 castle_square) */
module.exports = {
    id: 3011,
    name: 'boss_castle_square_3011',
    desc: '타락의 검기 — 검기 3발 부채꼴',
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
