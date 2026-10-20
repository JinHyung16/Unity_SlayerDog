/** 파츠 = 스킬 1개 + 옵션 슬롯. 착용하면 skillId 스킬이 자동 발동한다. */
module.exports = {
    id: 2021,
    name: 'Kibble Bomb',
    desc: '화염 단지를 던져 반경 2m 공격력 90% 피해 + 화상. 3초마다',
    displayName: '화염 단지',
    slot: 'Back',
    family: 'Throw',
    appliesDebuff: 'DamageOverTime',
    skillId: 2021,
    weight: 10,
    owner: 0
};
