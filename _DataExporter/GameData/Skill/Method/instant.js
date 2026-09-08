/**
 * 발사 방식 모듈.
 *
 * nodes = **형제 노드 목록**이고 배열 순서가 곧 실행 순서다.
 * Hit/Debuff/Despawn 은 ctx 에 값을 써두는 리프라 반드시 Launch 앞에 와야 하고,
 * Launch 는 그 ctx 를 읽어 이펙트를 스폰하므로 항상 마지막이다.
 * (Launch 노드는 자식을 실행하지 않는다 — 자식으로 매달면 영영 안 돈다)
 */
module.exports = {
    desc: '즉발 — 선정된 대상 위치에 바로 생성. 대상이 없으면 실패',
    slots: ['onHit'],
    nodes: [
        { slot: 'onHit' },
        { slot: 'onDebuff', optional: true },
        { slot: 'onCast', optional: true },
        { type: 'DespawnAfterHits', params: { Value: 1 } },
        { type: 'LaunchInstant', params: { Visual: '@visual.Impact', VisualColor: '@visual.Color', VisualOrient: '@visual.Orient' } }
    ]
};
