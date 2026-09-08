/**
 * 근접 계열의 부채꼴. 성좌 한 갈래가 이 한 행에서 통째로 만들어진다.
 *
 * centerAngle 은 화면 기준 각도(도)다 — 90 이 위, 0 이 오른쪽.
 * span 은 그 갈래가 벌어지는 폭. 계열 6개가 60도 간격이므로 50 이면 갈래 사이가 붙지 않는다.
 *
 * 단 화면이 16:9 라 성좌는 마지막에 세로로 눌린다(CommonConfig.Constellation.AspectY).
 * 위·아래로 뻗는 갈래(근접 90도 · 장판 -90도)는 그 눌림을 그대로 받아 부채꼴이 좁아지므로,
 * 폭을 76 으로 키워 «눌린 뒤에» 다른 갈래와 같은 폭이 되게 한다. 좌우로 뻗는 갈래는 눌림을 거의 안 받는다.
 * clusterCount 는 성단 수이고, 그 계열 스킬이 성단의 중심 자리를 하나씩 채운다 —
 * 스킬보다 성단이 많으면 남는 자리는 계열 보너스가 된다.
 */
module.exports = {
    id: 9001,
    name: 'region_melee',
    desc: '근접 — 물고 할퀴는 갈래. 맞을 각오를 하는 만큼 체력이 붙는다',
    displayName: '근접',
    family: 'Melee',
    centerAngle: 90,
    span: 76,
    clusterCount: 7,
    color: '#FF7A4D',
    traitA: 'Damage',
    valueA: 0.02,
    traitB: 'MaxHp',
    valueB: 0.02,
    keystoneName: '송곳니 각인',
    keystoneDesc: '근접 계열 피해 +25%',
    keystoneTrait: 'Damage',
    keystoneValue: 0.25
};
