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
        // 맵이 없을 때 쓰는 바닥 — 맵 1
        public const string KeyGroundFallback = "obj_ground_snow_sakura";

        // 고른 맵의 바닥 프리팹 주소 (Art2DBuilder 가 맵마다 obj_ground_<key> 로 굽는다)
        public static string GroundKey
        {
            get
            {
                MapData map = MapRegistry.Current;
                return map != null && string.IsNullOrEmpty(map.GroundKey) == false ? map.GroundKey : KeyGroundFallback;
            }
        }

        public static async UniTask<Player> SpawnPlayerWorldAsync(SkillLoadout loadout, Vector3 spawnPosition)
        {
            AddressableManager am = AddressableManager.Ensure();
            string ground = GroundKey;
            await am.LoadAllAsync(new[] { KeyPlayer, KeyEnemy, ground, Jinhyeong_Parts.PartDropper.PickupKey });

            EnsureGround(ground);
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

        private static void EnsureGround(string key)
        {
            if (Object.FindFirstObjectByType<TilemapStreamer>() != null)
                return;

            AddressableManager am = AddressableManager.Instance;
            GameObject prefab = am != null ? am.Get(key) : null;
            if (prefab == null)
            {
                Debug.LogWarning($"[WorldSpawner] {key} 프리팹이 없습니다 — 바닥 없이 진행");
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
            pm.Pool_Fx_Prewarm(CommonConfig.Skill.MeatKey, CommonConfig.Spawner.PrewarmFieldPickup);
            pm.Pool_Fx_Prewarm(CommonConfig.Skill.MagnetKey, CommonConfig.Spawner.PrewarmFieldPickup);
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
