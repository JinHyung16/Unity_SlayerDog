module.exports = {
    desc: '사행 투사체 — 좌우로 흔들리며 목표 지점으로 (ArcHeight = 흔들림 폭)',
    slots: ['onHit'],
    nodes: [
        { slot: 'onHit' },
        { slot: 'onDebuff', optional: true },
        { type: 'DespawnAfterHits', params: { Value: '@stat.MaxPerTarget' } },
        {
            type: 'LaunchCurve',
            params: {
                Speed: '@stat.Speed',
                ArcHeight: '@stat.ArcHeight',
                Visual: '@visual.Projectile', VisualColor: '@visual.Color', VisualOrient: '@visual.Orient'
            }
        }
    ]
};
