/** 라운드 6 — 300~360초. 스폰 간격/한 번에/동시 상한이 난이도의 본체다. */
// LV1 (10-04): 레벨업 자동 체력(레벨당 +4%, 20레벨이면 ×2.2)이 빠져 라운드 5~12 피해 ×0.8
module.exports = {
    id: 7006,
    name: 'Round6',
    desc: '라운드 6',
    round: 6,
    spawnGap: 0.8,
    spawnBatch: 2,
    maxAlive: 64,
    hpMul: 1.7,
    damageMul: 1.36,
    banner: ''
};
