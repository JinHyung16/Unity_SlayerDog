/**
 * 발사체가 없는 스킬용. BuffSelf 같이 즉시 효과를 내는 리프를 슬롯에 꽂아 쓴다.
 * Launch 는 Cast 그림을 시전자 자리에 한 번 띄우는 연출뿐이다 — Hit 노드가 없어 판정 없이 한 바퀴 돌고 사라진다.
 */
module.exports = {
    desc: '자기 자신에게 즉시 적용 — 발사체 없음, 시전자 자리에 Cast 그림',
    slots: ['onCast'],
    nodes: [
        { slot: 'onCast' },
        { type: 'LaunchInstant', params: { Visual: '@visual.Cast', VisualColor: '@visual.Color', VisualOrient: '@visual.Orient' } }
    ]
};
