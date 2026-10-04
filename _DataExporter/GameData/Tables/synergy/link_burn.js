/** 불붙은 사료 — DamageOverTime 를 거는 스킬을 보유하면 발동. 그 상태의 적에게만 붙는다. */
module.exports = {
    id: 8020,
    name: 'link_burn',
    desc: '화상·중독 걸린 적에게 피해 +30%',
    displayName: '뜨거운 무대',
    family: 'None',
    reqFamilyCount: 0,
    reqSkillId: 0,
    reqOwnOptions: 0,
    reqDebuff: 'DamageOverTime',
    trait: 'None',
    value: 0.3,
    vsDebuff: 'DamageOverTime'
};
