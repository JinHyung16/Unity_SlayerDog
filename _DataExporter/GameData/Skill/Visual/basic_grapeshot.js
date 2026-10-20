/** 산탄 포격 — 산탄 5발을 부채꼴로 뿌려 각 공격력 60% 피해. 1.1초마다 */
module.exports = {
    color: '#FFC466',
    desc: "산탄 포격 — 산탄 5발을 부채꼴로 뿌려 각 공격력 60% 피해. 1.1초마다",
    values: {
        "Projectile": "Fx_Pellet",
        "Impact": "Fx_Hit",
        "Cast": "Fx_Muzzle",
        "Orient": "Billboard",
        "Color": "#FFC466",
        "Cooldown": 1.1,
        "Range": 8,
        "Damage": 60,
        "Radius": 0.6,
        "Speed": 16,
        "MaxDistance": 9,
        "BurstCount": 5,
        "BurstInterval": 0.02,
        "BurstSpread": 28
    },
};
