using UnityEngine;
using UnityEngine.SceneManagement;
using Jinhyeong_AI;
using Jinhyeong_SkillSystem;
using Jinhyeong_Common;

namespace Jinhyeong_Managers
{

    public class GameInitializer : BaseBehaviour
    {
        public static GameInitializer Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
                return;
            GameObject go = new GameObject("_GameInitializer");
            go.AddComponent<GameInitializer>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Init()
        {
            AddressableManager.Ensure();
            PoolManager.Ensure();
            EnemySpawner.Ensure();

            // 대량 스폰용. 컬링이 없으면 화면 밖 적의 AI·애니메이터가 그대로 돌아 프레임이 죽는다.
            EnemyCullingSystem.Ensure();
            WaveDirector.Ensure();
            Jinhyeong_UI.SurvivorHud.Ensure();
            Jinhyeong_SkillSystem.RunProgress.Ensure();
        }

        public void ClearAll()
        {
            if (EnemySpawner.Instance != null)
                EnemySpawner.Instance.Clear();
            if (PoolManager.Instance != null)
                PoolManager.Instance.Clear();
            if (AddressableManager.Instance != null)
                AddressableManager.Instance.Clear();

            SkillRegistry.Clear();
            SkillBuffRegistry.Clear();
            PartRegistry.Clear();
            PartOptionRegistry.Clear();
            RoundRegistry.Clear();
            SynergyRegistry.Clear();
            EnemyTypeRegistry.Clear();
            WaveEventRegistry.Clear();
            if (Jinhyeong_SkillSystem.RunProgress.Instance != null)
                Jinhyeong_SkillSystem.RunProgress.Instance.ResetRun();
        }

        public void Restart()
        {
            ClearAll();
            Jinhyeong_UI.GameFlow.ResetForReload();
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }

        private void OnApplicationQuit()
        {
            ClearAll();
        }
    }
}
