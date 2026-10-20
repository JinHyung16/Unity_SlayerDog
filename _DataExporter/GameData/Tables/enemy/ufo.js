/** 적 타입. weight 로 추첨되고 minTime(초) 이후에만 등장한다. sprite 가 2D 비주얼 키. */
module.exports = {
    id: 5006,
    name: 'Boss',
    desc: '맵 보스 — 그림 · 이름 · 패턴은 지금 맵이 정한다 (Tables/map boss · bossSkills, build_bosses.py)',
    sprite: 'boss',
    hp: 240,
    moveSpeed: 2.6,
    attackDamage: 16,
    scale: 1,
    color: '#FFFFFF',
    xp: 200,
    weight: 0,
    minTime: 99999
};
