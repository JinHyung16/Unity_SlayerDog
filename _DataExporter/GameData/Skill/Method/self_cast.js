/**
 * 발사체가 없는 스킬용. Launch 노드가 아예 없다 (0개도 허용).
 * BuffSelf 같이 즉시 효과를 내는 리프를 슬롯에 꽂아 쓴다.
 */
module.exports = {
    desc: '자기 자신에게 즉시 적용 — 발사체 없음',
    slots: ['onCast'],
    nodes: [
        { slot: 'onCast' }
    ]
};
