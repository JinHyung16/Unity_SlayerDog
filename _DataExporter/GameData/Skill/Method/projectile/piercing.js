module.exports = {
    desc: '관통 투사체 — 여러 명을 뚫고 지나간다 (Value 만큼 타격 후 소멸)',
    slots: ['onHit'],
    nodes: [
        { slot: 'onHit' },
        { slot: 'onDebuff', optional: true },
        { type: 'DespawnAfterHits', params: { Value: '@stat.MaxPerTarget' } },
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
