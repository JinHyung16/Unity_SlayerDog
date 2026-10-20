using UnityEngine;

namespace Jinhyeong_Common
{
    // 스팀 연동 한 곳 (TASKS C-4). Steamworks.NET 패키지와 스크립팅 심볼 STEAMWORKS_NET 이 있을 때만 실제로 부른다
    // 없으면 전부 아무 일도 안 한다 — 에디터 · 스팀 밖 빌드가 그대로 돈다
    // 업적 ID 는 Steamworks 관리 화면에 같은 이름으로 만든다: CLEAR_MAP_01 ~ CLEAR_MAP_10, ALL_HEROES
    public static class SteamService
    {
        public static bool Ready { get; private set; }

        public static void Init()
        {
#if STEAMWORKS_NET
            if (Ready)
                return;
            try
            {
                Ready = Steamworks.SteamAPI.Init();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[SteamService] SteamAPI.Init 실패 — " + e.Message);
                Ready = false;
            }
#endif
        }

        public static void Tick()
        {
#if STEAMWORKS_NET
            if (Ready)
                Steamworks.SteamAPI.RunCallbacks();
#endif
        }

        public static void Shutdown()
        {
#if STEAMWORKS_NET
            if (Ready)
                Steamworks.SteamAPI.Shutdown();
            Ready = false;
#endif
        }

        public static void Unlock(string achievementId)
        {
#if STEAMWORKS_NET
            if (Ready == false || string.IsNullOrEmpty(achievementId))
                return;
            Steamworks.SteamUserStats.SetAchievement(achievementId);
            Steamworks.SteamUserStats.StoreStats();
#endif
        }

        // 챕터 첫 클리어 · 10명 모두 합류
        public static void OnMapCleared(int mapOrder, bool allHeroes)
        {
            Unlock("CLEAR_MAP_" + mapOrder.ToString("00"));
            if (allHeroes)
                Unlock("ALL_HEROES");
        }

        // 클라우드 세이브: 세이브 문자열 한 파일. 스팀 자동 클라우드를 안 쓰고 원격 저장소에 직접 쓴다
        public static void WriteCloud(string json)
        {
#if STEAMWORKS_NET
            if (Ready == false || string.IsNullOrEmpty(json))
                return;
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            Steamworks.SteamRemoteStorage.FileWrite(CloudFile, bytes, bytes.Length);
#endif
        }

        public static string ReadCloud()
        {
#if STEAMWORKS_NET
            if (Ready == false || Steamworks.SteamRemoteStorage.FileExists(CloudFile) == false)
                return null;
            int size = Steamworks.SteamRemoteStorage.GetFileSize(CloudFile);
            byte[] bytes = new byte[size];
            Steamworks.SteamRemoteStorage.FileRead(CloudFile, bytes, size);
            return System.Text.Encoding.UTF8.GetString(bytes);
#else
            return null;
#endif
        }

        private const string CloudFile = "slayerdog_save.json";
    }
}
