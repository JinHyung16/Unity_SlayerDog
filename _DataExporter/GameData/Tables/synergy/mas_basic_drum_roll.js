/** 드럼 롤 통달 — 이 기본 공격의 증강을 4개 이상 모으면 자동 발동. */
module.exports = {
    id: 8102,
    name: 'mas_basic_drum_roll',
    desc: '드럼 롤 증강 4개 — 피해 +80%',
    displayName: '드럼 롤 통달',
    family: 'None',
    reqFamilyCount: 0,
    reqSkillId: 2202,
    reqOwnOptions: 4,
    reqDebuff: 'None',
    trait: 'Damage',
    value: 0.8,
    vsDebuff: 'None'
};
