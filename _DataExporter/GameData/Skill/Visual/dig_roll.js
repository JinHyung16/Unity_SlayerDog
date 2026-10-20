/** 회전 검무 — 검무를 추는 동안 0.5초마다 반경 1.5m 공격력 30% 피해 (상시 장판) */
module.exports = {
    color: '#A66BFF',
    desc: "회전 검무 — 검무를 추는 동안 0.5초마다 반경 1.5m 공격력 30% 피해 (상시 장판)",
    values: {
        "Projectile": "Fx_BladeSwirl",
        "Impact": "Fx_BladeSwirl",
        "Cast": "Fx_BladeSwirl",
        "Orient": "Ground",
        "Color": "#A66BFF",
        "Cooldown": 4,
        "Range": 2,
        "Damage": 30,
        "Radius": 1.5,
        "Duration": 4,
        "DamageInterval": 0.5
    },
};
