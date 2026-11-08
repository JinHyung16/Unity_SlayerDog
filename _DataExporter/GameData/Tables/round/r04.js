/** 라운드 4 — 180~240초. 스폰 간격/한 번에/동시 상한이 난이도의 본체다. */
// 뱀서 웨이브 리듬 (11-08): 탱커 라운드 — 동시 상한 ×0.7 · 체력 ×1.4 (적은 수, 단단한 적. 숨 고르기 + 빌드 점검). 이전 maxAlive 42 · hpMul 1.35 — 필드 총 체력(상한×체력)은 ±6% 안
module.exports = {
    id: 7004,
    name: 'Round4',
    desc: '라운드 4',
    round: 4,
    spawnGap: 1,
    spawnBatch: 2,
    maxAlive: 30,
    hpMul: 1.9,
    damageMul: 1.1,
    banner: '정예 부대 출현!'
};
