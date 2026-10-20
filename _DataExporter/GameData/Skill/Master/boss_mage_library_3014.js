/** 보스 패턴 저주 마탄 — 쫓아오는 마탄 (맵 mage_library) */
module.exports = {
    id: 3014,
    name: 'boss_mage_library_3014',
    desc: '저주 마탄 — 쫓아오는 마탄',
    maxLevel: 1,
    method: 'projectile/homing',

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
