/** 파츠 = 스킬 1개 + 옵션 슬롯. 착용하면 skillId 스킬이 자동 발동한다. */
module.exports = {
    id: 2021,
    name: 'Kibble Bomb',
    desc: '심벌을 던져 반경 2m 공격력 90% 피해 + 화상. 3초마다',
    displayName: '심벌 크래시',
    slot: 'Back',
    family: 'Throw',
    appliesDebuff: 'DamageOverTime',
    skillId: 2021,
    weight: 10,
    owner: 0
};
