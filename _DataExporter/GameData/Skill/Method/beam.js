/**
 * 고정 빔. Speed 0 으로 쏘면 이펙트가 총구에 머문 채 전방을 계속 훑는다.
 * HitBeam 은 매 프레임 판정이라 지속시간 동안 DamageInterval 마다 들어간다.
 */
module.exports = {
    desc: '지속 빔 — 전방 직선을 일정 시간 계속 태운다',
    slots: ['onHit'],
    nodes: [
        { slot: 'onHit' },
        { slot: 'onDebuff', optional: true },
        { type: 'DespawnAfterTime', params: { Value: '@stat.Duration' } },
        {
            type: 'LaunchStraight',
            params: {
                Speed: 0,
                MaxDistance: 999,
                Visual: '@visual.Projectile', VisualColor: '@visual.Color', VisualOrient: '@visual.Orient'
            }
        }
    ]
};
