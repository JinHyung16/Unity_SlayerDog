/**
 * 발사 방식 모듈 — 즉발 + 휘두르기.
 *
 * instant 와 같고, Launch 가 Cast 그림을 시전자 앞에 대상 방향으로 한 번 더 그린다 (판정 없음).
 * 「칼을 뽑아 앞을 벤다」처럼 몸 앞에서 나가는 그림이 정보인 공격용.
 * instant 는 Impact 만 대상 위치에 띄우므로 Cast 를 쓰지 않는다.
 */
module.exports = {
    desc: '즉발 + 휘두르기 — 대상 위치에 바로 적중, 시전자 앞에 Cast 그림. 대상이 없으면 실패',
    slots: ['onHit'],
    nodes: [
        { slot: 'onHit' },
        { slot: 'onDebuff', optional: true },
        { slot: 'onCast', optional: true },
        { type: 'DespawnAfterHits', params: { Value: 1 } },
        { type: 'LaunchInstant', params: { Visual: '@visual.Impact', CastVisual: '@visual.Cast', VisualColor: '@visual.Color', VisualOrient: '@visual.Orient' } }
    ]
};
