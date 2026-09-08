/** 뼈다귀 매설 — 발밑에 뼈다귀를 묻는다. 적이 밟으면 반경 1.5m 공격력 200% 폭발. 4초마다 */
module.exports = {
    color: '#FFF6E0',
    desc: "뼈다귀 매설 — 발밑에 뼈다귀를 묻는다. 적이 밟으면 반경 1.5m 공격력 200% 폭발. 4초마다",
    values: {
        "Projectile": "Fx_Bone",
        "Impact": "Fx_Burst",
        "Cast": "Fx_Bone",
        "Orient": "Billboard",
        "Color": "#FFF6E0",
        "Cooldown": 4,
        "Range": 2,
        "Damage": 200,
        "Radius": 1.5
    },
};
