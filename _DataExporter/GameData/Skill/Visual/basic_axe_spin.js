/** 회전 도끼 — 도끼를 1.2초간 휘돌려 반경 1.8m 에 0.3초마다 공격력 38% 피해. 1.6초마다 */
module.exports = {
    color: '#8AD8FF',
    desc: "회전 도끼 — 도끼를 1.2초간 휘돌려 반경 1.8m 에 0.3초마다 공격력 38% 피해. 1.6초마다",
    values: {
        "Projectile": "Fx_AxeWhirl",
        "Impact": "Fx_AxeWhirl",
        "Cast": "Fx_AxeWhirl",
        "Orient": "Ground",
        "Color": "#8AD8FF",
        "Cooldown": 1.6,
        "Range": 1.8,
        "Damage": 38,
        "Radius": 1.8,
        "Duration": 1.2,
        "DamageInterval": 0.3
    },
};
