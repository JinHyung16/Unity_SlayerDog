/** 화염 단지 — 화염 단지를 던져 반경 2m 공격력 90% 피해 + 화상. 3초마다 */
module.exports = {
    id: 2021,
    name: 'Kibble Bomb',
    desc: '화염 단지를 던져 반경 2m 공격력 90% 피해 + 화상. 3초마다',
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
