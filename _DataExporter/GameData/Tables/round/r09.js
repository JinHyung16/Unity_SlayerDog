/** 라운드 9 — 480~540초. 스폰 간격/한 번에/동시 상한이 난이도의 본체다. */
// QA3 (10-04): 피해 배수 ×0.9 — QA2 에서 5명 중 4명이 라운드 7~11 에 급락 사망
// LV1 (10-04): 레벨업 자동 체력(레벨당 +4%, 20레벨이면 ×2.2)이 빠져 라운드 5~12 피해 ×0.8
module.exports = {
    id: 7009,
    name: 'Round9',
    desc: '라운드 9',
    round: 9,
    spawnGap: 0.6,
    spawnBatch: 3,
    maxAlive: 98,
    hpMul: 2.4,
    damageMul: 2.0,
    banner: ''
};
