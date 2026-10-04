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

            PartLoadout parts = go.GetComponent<PartLoadout>();
            if (parts != null)
            {
                parts.CharacterIndex = RunSelection.CharacterIndex;
                parts.BeginRun();
            }

            return player;
        }

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

        private static void PrewarmFx()
        {
            PoolManager pm = PoolManager.Instance;
            if (pm == null)
                return;
            pm.Pool_Fx_Prewarm(CommonConfig.Skill.HitFxKey, CommonConfig.Spawner.PrewarmHitFx);
            pm.Pool_Fx_Prewarm(CommonConfig.Skill.DamageTextKey, CommonConfig.Spawner.PrewarmDamageText);
            pm.Pool_Fx_Prewarm(CommonConfig.Skill.XpGemKey, CommonConfig.Spawner.PrewarmXpGem);
            pm.Pool_Fx_Prewarm(PartDropper.PickupKey, CommonConfig.Spawner.PrewarmPickup);
            pm.Pool_Fx_Prewarm(CommonConfig.Skill.HitBurstKey, CommonConfig.Spawner.PrewarmHitBurst);
            pm.Pool_Fx_Prewarm(CommonConfig.Skill.GhostKey, CommonConfig.Spawner.PrewarmGhost);
        }

        private static void EnsurePartDropper()
        {
            if (PartDropper.Instance != null)
                return;
            GameObject go = new GameObject("_PartDropper");
            go.AddComponent<PartDropper>();
        }
    }
}
