/** 식빵 썰기 — 건반을 내리쳐 사방 반경 3m 에 공격력 80% 피해. 2.5초마다 */
module.exports = {
    id: 2102,
    name: 'Bread Slice',
    desc: '건반을 내리쳐 사방 반경 3m 에 공격력 80% 피해. 2.5초마다',
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
