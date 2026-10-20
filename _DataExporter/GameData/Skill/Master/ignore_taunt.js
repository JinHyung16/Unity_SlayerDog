/** 부동심 — 3초간 단단해지며 주변 적을 끌어당긴다. 12초마다 */
module.exports = {
    id: 2101,
    name: 'Ignore',
    desc: '3초간 단단해지며 주변 적을 끌어당긴다. 12초마다',
    maxLevel: 1,
    method: 'self_cast',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetSelf'
},

    slots: {
    onCast: {
        type: 'BuffSelf',
        params: {
            BuffId: '#buff/barrier'
        }
    }
}
};
