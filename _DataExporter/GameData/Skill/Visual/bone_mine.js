/** 폭뢰 부적 — 발밑에 폭뢰 부적을 붙인다. 적이 밟으면 반경 1.5m 공격력 200% 폭발. 4초마다 */
module.exports = {
    color: '#A66BFF',
    desc: "폭뢰 부적 — 발밑에 폭뢰 부적을 붙인다. 적이 밟으면 반경 1.5m 공격력 200% 폭발. 4초마다",
    values: {
        "Projectile": "Fx_Seal",
        "Impact": "Fx_Blast",
        "Cast": "Fx_Seal",
        "Orient": "Billboard",
        "Color": "#A66BFF",
        "Cooldown": 4,
        "Range": 2,
        "Damage": 200,
        "Radius": 1.5
    },
};
