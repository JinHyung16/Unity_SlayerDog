/** 뼈다귀 매설 — 발밑에 베이스 드럼을 깐다. 적이 밟으면 반경 1.5m 공격력 200% 폭발. 4초마다 */
module.exports = {
    id: 2008,
    name: 'Bone Mine',
    desc: '발밑에 베이스 드럼을 깐다. 적이 밟으면 반경 1.5m 공격력 200% 폭발. 4초마다',
    maxLevel: 1,
    method: 'instant',

    trigger: 'TriggerOnTick',
    triggerParams: { Cooldown: '@stat.Cooldown' },
    targeting: {
    type: 'TargetSelf'
},

    slots: {
    onHit: {
        type: 'HitDeathBurst',
        params: {
            Damage: '@stat.Damage',
            Radius: '@stat.Radius'
        }
    }
}
};
