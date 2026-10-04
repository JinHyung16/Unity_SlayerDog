/** 베이스 드롭 통달 — 이 기본 공격의 증강을 4개 이상 모으면 자동 발동. */
module.exports = {
    id: 8101,
    name: 'mas_basic_bass_drop',
    desc: '베이스 드롭 증강 4개 — 피해 +80%',
    displayName: '베이스 드롭 통달',
    family: 'None',
    reqFamilyCount: 0,
    reqSkillId: 2201,
    reqOwnOptions: 4,
    reqDebuff: 'None',
    trait: 'Damage',
    value: 0.8,
    vsDebuff: 'None'
};
