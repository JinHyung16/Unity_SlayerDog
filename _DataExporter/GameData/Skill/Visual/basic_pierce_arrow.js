/** 관통 화살 — 긴 화살이 일직선으로 적 4명을 꿰뚫어 공격력 120% 피해. 0.9초마다 */
module.exports = {
    color: '#8CFF6E',
    desc: "관통 화살 — 긴 화살이 일직선으로 적 4명을 꿰뚫어 공격력 120% 피해. 0.9초마다",
    values: {
        "Projectile": "Fx_LongArrow",
        "Impact": "Fx_Hit",
        "Cast": "Fx_Muzzle",
        "Orient": "Ground",
        "Color": "#8CFF6E",
        "Cooldown": 0.9,
        "Range": 11,
        "Damage": 120,
        "Radius": 0.6,
        "Speed": 24,
        "MaxDistance": 13,
        "MaxPerTarget": 4
    },
};
