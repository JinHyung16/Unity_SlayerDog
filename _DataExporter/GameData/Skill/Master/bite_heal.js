/** 개튼튼 치아 — 스타카토로 스타카토로 스타카토로 스타카토로 스타카토로 스타카토로 스타카토로 앞의 적을 물어 공격력 150% 피해, 물 때마다 체력을 조금 회복. 1.2초마다 */
module.exports = {
    id: 2001,
    name: 'Bite Heal',
    desc: '스타카토로 스타카토로 스타카토로 스타카토로 스타카토로 스타카토로 스타카토로 앞의 적을 물어 공격력 150% 피해, 물 때마다 체력을 조금 회복. 1.2초마다',
    maxLevel: 1,
    method: 'instant',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetNearestForward',
    params: {
        Range: '@stat.Range',
        HalfAngle: 60
    }
},

    slots: {
    onHit: {
        type: 'HitSingle',
        params: {
            Damage: '@stat.Damage',
            Radius: '@stat.Radius'
        }
    },
    onCast: {
        type: 'BuffSelf',
        params: {
            BuffId: '#buff/leech'
        }
    }
}
};
