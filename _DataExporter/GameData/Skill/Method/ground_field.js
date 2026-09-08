/**
 * 지속 장판. 시전자 발밑에 머물며 DamageInterval 마다 반복 판정한다.
 * Speed 가 없어 제자리에 남고, DespawnAfterTime 이 수명을 끊는다.
 */
module.exports = {
    desc: '지속 장판 — 발밑에 남아 주기적으로 때린다',
    slots: ['onHit'],
    nodes: [
        { slot: 'onHit' },
        { slot: 'onDebuff', optional: true },
        { slot: 'onCast', optional: true },
        { type: 'DespawnAfterTime', params: { Value: '@stat.Duration' } },
        { type: 'LaunchInstant', params: { Visual: '@visual.Cast', VisualColor: '@visual.Color', VisualOrient: '@visual.Orient' } }
    ]
};
