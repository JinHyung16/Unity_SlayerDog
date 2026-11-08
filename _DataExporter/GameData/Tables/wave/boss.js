module.exports = {
    id: 6002,
    name: 'Boss',
    desc: '맵 보스 — 라운드12 시작. 배너는 맵 보스 이름으로 코드가 채운다',
    time: 660,
    repeat: 0,
    count: 1,
    hpMul: 4.5,
    damageMul: 1,
    scaleMul: 1.7,
    xpMul: 1,
    banner: '',
    boss: true,
    enemyTypeId: 5006,
    speedMul: 1.6,
    // 뱀서 HP_x_Level — 보스 HP × clamp(출현 시 레벨 / hpLevelRef, Min, Max). 기준 레벨이면 지금 그대로, 강한 빌드엔 단단하게 · 약한 빌드엔 무르게
    hpLevelRef: 28,
    hpLevelMin: 0.8,
    hpLevelMax: 1.4
};
