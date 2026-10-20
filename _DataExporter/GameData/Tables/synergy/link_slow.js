/** 사냥 본능 — SlowReduce 를 거는 스킬을 보유하면 발동. 그 상태의 적에게만 붙는다. */
module.exports = {
    id: 8019,
    name: 'link_slow',
    desc: '둔화된 적에게 피해 +35%',
    displayName: '빙결 족쇄',
    family: 'None',
    reqFamilyCount: 0,
    reqSkillId: 0,
    reqOwnOptions: 0,
    reqDebuff: 'SlowReduce',
    trait: 'None',
    value: 0.35,
    vsDebuff: 'SlowReduce'
};
