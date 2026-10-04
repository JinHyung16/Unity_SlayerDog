/** 라운드 5 — 240~300초. 스폰 간격/한 번에/동시 상한이 난이도의 본체다. */
// LV1 (10-04): 레벨업 자동 체력(레벨당 +4%, 20레벨이면 ×2.2)이 빠져 라운드 5~12 피해 ×0.8
module.exports = {
    id: 7005,
    name: 'Round5',
    desc: '라운드 5',
    round: 5,
    spawnGap: 0.9,
    spawnBatch: 2,
    maxAlive: 52,
    hpMul: 1.5,
    damageMul: 1.12,
    banner: ''
};
