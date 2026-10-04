using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Jinhyeong_GameData.Editor
{

    [InitializeOnLoad]
    public static class GameDataAutoRun
    {
        private const string RequestFile = "Temp/gamedata_autorun.request";
        private const string ReportFile = "Temp/gamedata_autorun.report";

        private static double _nextPoll;

        static GameDataAutoRun()
        {
            EditorApplication.update += WatchRequest;
        }

        private static void WatchRequest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (EditorApplication.timeSinceStartup < _nextPoll)
                return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            if (File.Exists(RequestFile) == false)
                return;

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
