/** 질풍 — 6초간 이동속도 +40%. 12초마다 */
module.exports = {
    id: 2007,
    name: 'Zoomies',
    desc: '6초간 이동속도 +40%. 12초마다',
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
            BuffId: '#buff/sprint'
        }
    }
}
};
