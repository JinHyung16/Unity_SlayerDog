/** 화염구 — 화염구가 날아가 터지며 반경 1.5m 공격력 120% 피해 + 화상. 1.1초마다 */
module.exports = {
    id: 2207,
    name: 'Fireball',
    desc: '화염구가 날아가 터지며 반경 1.5m 공격력 120% 피해 + 화상. 1.1초마다',
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
    },
    onDebuff: {
        type: 'DebuffOnHit',
        params: {
            DebuffId: '#debuff/burn'
        }
    }
}
};
