/** 자장가 하품 통달 — 이 스킬의 증강을 4개 이상 모으면 자동 발동. 전설(3개)을 주운 것만으로는 안 뜬다. */
module.exports = {
    id: 8025,
    name: 'mas_yawn',
    desc: '자장가 하품 증강 4개 — 지속시간 +50%',
    displayName: '자장가 하품 통달',
    family: 'None',
    reqFamilyCount: 0,
    reqSkillId: 2004,
    reqOwnOptions: 4,
    reqDebuff: 'None',
    trait: 'Duration',
    value: 0.5,
    vsDebuff: 'None'
};
