/** 편전 — 짧은 화살 4발을 부채꼴로 쏘아 각 공격력 75% 피해. 1초마다 */
module.exports = {
    color: '#FF7D66',
    desc: "편전 — 짧은 화살 4발을 부채꼴로 쏘아 각 공격력 75% 피해. 1초마다",
    values: {
        "Projectile": "Fx_Arrow",
        "Impact": "Fx_Hit",
        "Cast": "Fx_Muzzle",
        "Orient": "Ground",
        "Color": "#FF7D66",
        "Cooldown": 1,
        "Range": 9,
        "Damage": 75,
        "Radius": 0.5,
        "Speed": 18,
        "MaxDistance": 11,
        "BurstCount": 4,
        "BurstInterval": 0.05,
        "BurstSpread": 14
    },
};
