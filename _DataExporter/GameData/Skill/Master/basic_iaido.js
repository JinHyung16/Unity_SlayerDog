/** 발도 일섬 — 칼을 뽑아 전방 반달 반경 1.9m 에 공격력 200% 피해. 0.9초마다 */
module.exports = {
    id: 2201,
    name: 'Iaido',
    desc: '칼을 뽑아 전방 반달 반경 1.9m 에 공격력 200% 피해. 0.9초마다',
    maxLevel: 1,
    method: 'instant',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetCone',
    params: {
        Range: '@stat.Range',
        HalfAngle: 60
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
