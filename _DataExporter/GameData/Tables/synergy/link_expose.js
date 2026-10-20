/** 약점 포착 — DamageReceiveIncrease 를 거는 스킬을 보유하면 발동. 그 상태의 적에게만 붙는다. */
module.exports = {
    id: 8021,
    name: 'link_expose',
    desc: '취약 걸린 적에게 피해 +40%',
    displayName: '약점 간파',
    family: 'None',
    reqFamilyCount: 0,
    reqSkillId: 0,
    reqOwnOptions: 0,
    reqDebuff: 'DamageReceiveIncrease',
    trait: 'None',
    value: 0.4,
    vsDebuff: 'DamageReceiveIncrease'
};
