/** 추적 마탄 — 가장 먼 적을 유도 추적해 공격력 130% 피해 + 받는 피해 증가. 2초마다 */
module.exports = {
    color: '#669BFF',
    desc: "추적 마탄 — 가장 먼 적을 유도 추적해 공격력 130% 피해 + 받는 피해 증가. 2초마다",
    values: {
        "Projectile": "Fx_MagicShot",
        "Impact": "Fx_HitBurst",
        "Cast": "Fx_MagicShot",
        "Orient": "Billboard",
        "Color": "#4D8BFF",
        "Cooldown": 2,
        "Range": 13,
        "Damage": 130,
        "Radius": 0.7,
        "Speed": 11,
        "MaxDistance": 16,
        "TurnRate": 200
    },
};
