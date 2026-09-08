module.exports = {
    desc: '포물선 낙하 — 발사 시점의 목표 지점에 떨어진다 (착탄지 고정)',
    slots: ['onHit'],
    nodes: [
        { slot: 'onHit' },
        { slot: 'onDebuff', optional: true },
        { type: 'DespawnAfterHits', params: { Value: 1 } },
        {
            type: 'LaunchArc',
            params: {
                Speed: '@stat.Speed',
                ArcHeight: '@stat.ArcHeight',
                Visual: '@visual.Projectile', VisualColor: '@visual.Color', VisualOrient: '@visual.Orient'
            }
        }
    ]
};
