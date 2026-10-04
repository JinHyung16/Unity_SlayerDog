/** 메트로놈 — 1초 박자마다 발밑에서 반경 2.4m 공격력 35% 피해 */
module.exports = {
    id: 2026,
    name: 'Metronome',
    desc: '1초 박자마다 발밑에서 반경 2.4m 공격력 35% 피해',
    maxLevel: 1,
    method: 'instant',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetSelf'
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
