/** 보스 패턴 아홉 꼬리 불꽃 — 사방 12발 원형 탄막 (맵 palace_range) */
module.exports = {
    id: 3004,
    name: 'boss_palace_range_3004',
    desc: '아홉 꼬리 불꽃 — 사방 12발 원형 탄막',
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
