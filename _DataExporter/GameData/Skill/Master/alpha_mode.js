/** 광폭화 — 8초간 공격속도 +40%. 16초마다 */
module.exports = {
    id: 2018,
    name: 'Alpha Mode',
    desc: '8초간 공격속도 +40%. 16초마다',
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
            BuffId: '#buff/haste'
        }
    }
}
};
