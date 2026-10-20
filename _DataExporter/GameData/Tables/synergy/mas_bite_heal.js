/** 흡혈 송곳니 통달 — 이 스킬의 증강을 4개 이상 모으면 자동 발동. 전설(3개)을 주운 것만으로는 안 뜬다. */
module.exports = {
    id: 8022,
    name: 'mas_bite_heal',
    desc: '흡혈 송곳니 증강 4개 — 피해 +80%',
    displayName: '흡혈 송곳니 통달',
    family: 'None',
    reqFamilyCount: 0,
    reqSkillId: 2001,
    reqOwnOptions: 4,
    reqDebuff: 'None',
    trait: 'Damage',
    value: 0.8,
    vsDebuff: 'None'
};
