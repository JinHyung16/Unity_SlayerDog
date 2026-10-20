/** 수호 결계 — 최대체력 15% 보호막을 8초간. 10초마다 */
module.exports = {
    id: 2019,
    name: 'Fur Barrier',
    desc: '최대체력 15% 보호막을 8초간. 10초마다',
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
