/**
 * 연발 직선 투사체. 한 번 발동에 BurstCount 발을 BurstInterval 간격 · BurstSpread 각도로 쏜다.
 * 개수 증강(CountAdd)은 BurstCount 에 더해진다 (LaunchExecutor).
 */
module.exports = {
    desc: '연발 투사체 — N 발을 간격 · 각도로 나눠 쏜다',
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
                BurstCount: '@stat.BurstCount',
                BurstInterval: '@stat.BurstInterval',
                BurstSpread: '@stat.BurstSpread',
                Visual: '@visual.Projectile', VisualColor: '@visual.Color', VisualOrient: '@visual.Orient'
            }
        }
    ]
};
