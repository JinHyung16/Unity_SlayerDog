/** 보스 패턴 찢긴 책장 — 사방 12발 원형 탄막 (맵 mage_library) */
module.exports = {
    id: 3013,
    name: 'boss_mage_library_3013',
    desc: '찢긴 책장 — 사방 12발 원형 탄막',
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
