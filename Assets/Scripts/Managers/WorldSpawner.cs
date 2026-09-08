using Cysharp.Threading.Tasks;
using Jinhyeong_Common;
using UnityEngine;
using Jinhyeong_Character;
using Jinhyeong_Parts;
using Jinhyeong_SkillSystem;
using Jinhyeong_View;

namespace Jinhyeong_Managers
{
    public static class WorldSpawner
    {
        public const string KeyPlayer = "obj_player";
        public const string KeyEnemy  = "obj_enemy";
        public const string KeyGround = "obj_ground";

        private static readonly string[] PreloadKeys =
            { KeyPlayer, KeyEnemy, KeyGround, Jinhyeong_Parts.PartDropper.PickupKey };

        public static async UniTask<Player> SpawnPlayerWorldAsync(SkillLoadout loadout, Vector3 spawnPosition)
        {
            AddressableManager am = AddressableManager.Ensure();
            await am.LoadAllAsync(PreloadKeys);

            EnsureGround();
            EnsurePartDropper();
            PrewarmFx();

            return SpawnPlayer(loadout, spawnPosition);
        }

        private static Player SpawnPlayer(SkillLoadout loadout, Vector3 spawnPosition)
        {
            // [사고] 플레이어가 **두 마리** 스폰돼 있었다. 하네스가 직접 하나를 세우고,
            // LobbyWindow 의 비동기 흐름이 나중에 또 하나를 세운다. 화면엔 개가 둘,
            // 스킬은 두 벌이 나가 그동안의 밸런스 실측이 통째로 부풀려져 있었다.
            //
            // 「누가 먼저 부르나」를 조율하는 대신 스폰 자체를 한 번만 되게 막는다 —
            // 호출 순서에 기대는 규칙은 경로가 늘 때마다 다시 깨진다.
            Player existing = Object.FindFirstObjectByType<Player>();
            if (existing != null)
            {
                Debug.LogWarning("[WorldSpawner] 플레이어가 이미 있어 새로 만들지 않습니다");
                return existing;
            }

            AddressableManager am = AddressableManager.Instance;
            GameObject prefab = am != null ? am.Get(KeyPlayer) : null;
            if (prefab == null)
            {
                Debug.LogError("[WorldSpawner] obj_player 프리팹을 캐시에서 못 찾음 — 플레이어 스폰 실패");
                return null;
            }

            GameObject go = Object.Instantiate(prefab, spawnPosition, Quaternion.identity);
            Player player = go.GetComponent<Player>();
            if (player == null)
            {
                Debug.LogError("[WorldSpawner] obj_player 프리팹에 Player 컴포넌트가 없음");
                return null;
            }

            if (loadout != null && player.Skills != null)
            {
                player.Skills.Loadout = loadout;
                player.Skills.EquipAll();
            }

            // 파츠 착용 시작. 알몸 + 고유 파츠 1개로 출발한다 — 성장은 전부 여기서 시작된다.
            // 어떤 슈퍼독인지는 로비가 정한다. 이걸 안 넘기면 프리팹 기본값(시바)으로 고정된다.
            PartLoadout parts = go.GetComponent<PartLoadout>();
            if (parts != null)
            {
                parts.CharacterIndex = RunSelection.CharacterIndex;
                parts.BeginRun();
            }

            return player;
        }

        /// <summary>
        /// 바닥은 한 판에 하나면 된다. 씬에 미리 두지 않고 런 시작 때 만든다 —
        /// 씬 파일을 건드리지 않아야 다른 씬에서도 그대로 돈다.
        /// </summary>
        private static void EnsureGround()
        {
            if (Object.FindFirstObjectByType<TilemapStreamer>() != null)
                return;

            AddressableManager am = AddressableManager.Instance;
            GameObject prefab = am != null ? am.Get(KeyGround) : null;
            if (prefab == null)
            {
                Debug.LogWarning("[WorldSpawner] obj_ground 프리팹이 없습니다 — 바닥 없이 진행");
                return;
            }
            Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// 연출 풀을 미리 채운다. 에셋 로드가 끝난 **뒤에** 불러야 한다 —
        /// 프리팹이 아직 없으면 조용히 건너뛰고, 결국 전투 중에 만들게 된다.
        /// </summary>
        private static void PrewarmFx()
        {
            PoolManager pm = PoolManager.Instance;
            if (pm == null)
                return;
            pm.Pool_Fx_Prewarm(CommonConfig.Skill.HitFxKey, CommonConfig.Spawner.PrewarmHitFx);
            pm.Pool_Fx_Prewarm(CommonConfig.Skill.DamageTextKey, CommonConfig.Spawner.PrewarmDamageText);
            pm.Pool_Fx_Prewarm(CommonConfig.Skill.XpGemKey, CommonConfig.Spawner.PrewarmXpGem);
            pm.Pool_Fx_Prewarm(PartDropper.PickupKey, CommonConfig.Spawner.PrewarmPickup);
        }

        /// <summary>파츠 상자를 떨어뜨리는 관리자. 에셋이 필요 없어 컴포넌트만 붙인다.</summary>
        private static void EnsurePartDropper()
        {
            if (PartDropper.Instance != null)
                return;
            GameObject go = new GameObject("_PartDropper");
            go.AddComponent<PartDropper>();
        }
    }
}
