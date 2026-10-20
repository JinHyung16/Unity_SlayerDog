/** 보스 패턴 금쇠몽둥이 내려찍기 — 몽둥이를 던져 반경 2.2m 폭발 (맵 snow_sakura) */
module.exports = {
    id: 3001,
    name: 'boss_snow_sakura_3001',
    desc: '금쇠몽둥이 내려찍기 — 몽둥이를 던져 반경 2.2m 폭발',
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
