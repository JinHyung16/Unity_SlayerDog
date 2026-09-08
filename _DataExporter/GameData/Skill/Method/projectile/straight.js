module.exports = {
    desc: '직선 투사체 — 최대 사거리까지 직진',
    slots: ['onHit'],
    nodes: [
        { slot: 'onHit' },
        { slot: 'onDebuff', optional: true },
        { type: 'DespawnAfterHits', params: { Value: 1 } },
        {
            type: 'LaunchStraight',
            params: {
                Speed: '@stat.Speed',
                MaxDistance: '@stat.MaxDistance',
                Visual: '@visual.Projectile', VisualColor: '@visual.Color', VisualOrient: '@visual.Orient'
            }
        }
    ]
};
