/**
 * 유도 투사체. 대상이 죽거나 사라지면 마지막 진행 방향으로 계속 날아간다.
 * TurnRate(도/초)가 낮으면 크게 휘고, 높으면 딱 붙어서 쫓는다.
 */
module.exports = {
    desc: '유도 투사체 — 대상을 계속 추적',
    slots: ['onHit'],
    nodes: [
        { slot: 'onHit' },
        { slot: 'onDebuff', optional: true },
        { type: 'DespawnAfterHits', params: { Value: 1 } },
        {
            type: 'LaunchHoming',
            params: {
                Speed: '@stat.Speed',
                MaxDistance: '@stat.MaxDistance',
                TurnRate: '@stat.TurnRate',
                Visual: '@visual.Projectile', VisualColor: '@visual.Color', VisualOrient: '@visual.Orient'
            }
        }
    ]
};
