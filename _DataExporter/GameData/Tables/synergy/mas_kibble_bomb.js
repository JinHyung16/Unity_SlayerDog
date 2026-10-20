/** 화염 단지 통달 — 이 스킬의 증강을 4개 이상 모으면 자동 발동. 전설(3개)을 주운 것만으로는 안 뜬다. */
module.exports = {
    id: 8042,
    name: 'mas_kibble_bomb',
    desc: '화염 단지 증강 4개 — 피해 +80%',
    displayName: '화염 단지 통달',
    family: 'None',
    reqFamilyCount: 0,
    reqSkillId: 2021,
    reqOwnOptions: 4,
    reqDebuff: 'None',
    trait: 'Damage',
    value: 0.8,
    vsDebuff: 'None'
};
