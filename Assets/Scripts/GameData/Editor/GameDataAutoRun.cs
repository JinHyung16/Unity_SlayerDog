using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Jinhyeong_GameData.Editor
{

    /// <summary>
    /// 에디터를 열어둔 채로 DB 파이프라인을 돌리기 위한 원샷 훅.
    ///
    /// Unity 는 -batchmode -executeMethod 에 프로젝트 배타 락이 필요해서 에디터가 열려 있으면 못 쓴다.
    /// 대신 요청 파일(Temp/gamedata_autorun.request)이 있을 때만 리컴파일 직후 한 번 실행하고,
    /// 콘솔 출력을 Temp/gamedata_autorun.report 로 떨군다. 요청 파일이 없으면 아무 일도 하지 않는다.
    ///
    /// 사용: 요청 파일을 만들면 1초 안에 감지해서 실행한다. 에디터가 떠 있기만 하면 되고 클릭은 필요 없다.
    /// </summary>
    [InitializeOnLoad]
    public static class GameDataAutoRun
    {
        private const string RequestFile = "Temp/gamedata_autorun.request";
        private const string ReportFile = "Temp/gamedata_autorun.report";

        private static double _nextPoll;

        static GameDataAutoRun()
        {
            // 요청 파일을 상시 감시한다. static 생성자는 도메인 리로드 때만 도는데,
            // C# 변경이 없으면 창을 클릭해도 리로드가 안 일어나 요청이 영영 안 잡힌다.
            EditorApplication.update += WatchRequest;
        }

        private static void WatchRequest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (EditorApplication.timeSinceStartup < _nextPoll)
                return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;

            // 컴파일/임포트 중에는 건드리지 않는다
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            if (File.Exists(RequestFile) == false)
                return;

            // Unity 는 창이 포커스를 받을 때만 에셋을 임포트한다. 포커스 없이 돌리려면 직접 부른다.
            // 스크립트가 바뀌었으면 여기서 리컴파일이 시작되고, 도메인 리로드 후 다음 틱에 이어서 처리된다.
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            EditorApplication.delayCall += Run;
        }

        private static void Run()
        {
            if (File.Exists(RequestFile) == false)
            {
                return;
            }

            // 재실행 루프를 막으려면 무슨 일이 있어도 먼저 지운다
            try
            {
                File.Delete(RequestFile);
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[GameDataAutoRun] 요청 파일 삭제 실패 — 중단: {e.Message}");
                return;
            }

            StringBuilder log = new StringBuilder(4096);
            Application.LogCallback handler = (condition, stackTrace, type) =>
            {
                if (type == LogType.Log || type == LogType.Warning || type == LogType.Error || type == LogType.Exception)
                {
                    log.AppendLine($"[{type}] {condition}");
                }
            };

            Application.logMessageReceived += handler;
            try
            {
                Art2DBuilder.BuildAll();
                GameDataValidator.Validate();
            }
            catch (Exception e)
            {
                log.AppendLine($"[Exception] {e}");
            }
            finally
            {
                Application.logMessageReceived -= handler;
            }

            try
            {
                File.WriteAllText(ReportFile, log.ToString(), Encoding.UTF8);
                Debug.Log($"[GameDataAutoRun] 완료 — 리포트: {ReportFile}");
            }
            catch (IOException e)
            {
                Debug.LogError($"[GameDataAutoRun] 리포트 기록 실패: {e.Message}");
            }
        }
    }
}
