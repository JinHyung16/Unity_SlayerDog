/** 사료 유성우 — 랜덤한 적 위치로 음표가 쏟아져 반경 1.5m 공격력 80% 피해. 5초마다 */
module.exports = {
    id: 2020,
    name: 'Kibble Rain',
    desc: '랜덤한 적 위치로 음표가 쏟아져 반경 1.5m 공격력 80% 피해. 5초마다',
    maxLevel: 1,
    method: 'projectile/arc',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetRandom',
    params: {
        Range: '@stat.Range',
        Index: 3
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
