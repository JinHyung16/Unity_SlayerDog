using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Jinhyeong_GameData.Editor
{

    [InitializeOnLoad]
    public static class EditorTaskRunner
    {
        private const string TaskFile = "Temp/editor.task";
        private const string LogFile = "Temp/editor.task.log";
        private const string DoneFile = "Temp/editor.task.done";

        private const double PollSeconds = 1.0;

        private static double _nextPoll;
        private static StringBuilder _capture;

        static EditorTaskRunner()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            if (EditorApplication.timeSinceStartup < _nextPoll)
            {
                return;
            }
            _nextPoll = EditorApplication.timeSinceStartup + PollSeconds;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }
            if (File.Exists(TaskFile) == false)
            {
                return;
            }

            string task = string.Empty;
            try
            {
                task = File.ReadAllText(TaskFile).Trim().ToLowerInvariant();
            }
            catch
            {
                return;
            }

            SafeDelete(TaskFile);
            SafeDelete(DoneFile);

            Run(task);
        }

        private static void Run(string task)
        {
            _capture = new StringBuilder();
            Application.logMessageReceived += Capture;

            string status = "ok";
            try
            {
                switch (task)
                {
                    case "refresh":
                        AssetDatabase.Refresh();
                        Debug.Log("[EditorTask] 에셋 새로고침");
                        break;

                    case "build":
                        AssetDatabase.Refresh();
                        Art2DBuilder.BuildAll();
                        break;

                    case "validate":
                        AssetDatabase.Refresh();
                        GameDataValidator.Validate();
                        break;

                    case "nodes":
                        SkillNodeCheck.RunBatch();
                        break;

                    case "ui":
                        AssetDatabase.Refresh();
                        Jinhyeong_UI.Editor.UIPrefabBuilder.Build();
                        break;

                    case "sceneui":
                        AssetDatabase.Refresh();
                        Debug.Log(Jinhyeong_UI.Editor.UIPrefabBuilder.PlaceLobbyInScene());
                        break;

                    case "star":
                        AssetDatabase.Refresh();
                        Debug.Log(ConstellationCheck.Run());
                        break;

                    case "starsave":
                        AssetDatabase.Refresh();
                        Debug.Log(ConstellationCheck.SaveDraft());
                        break;

                    case "staredit":
                        Jinhyeong_Constellation.Editor.ConstellationEditorWindow.Open();
                        break;

                    default:
                        status = "unknown";
                        Debug.LogError($"[EditorTask] 모르는 작업 '{task}' (refresh · build · validate · nodes · ui · sceneui · star · starsave · staredit)");
                        break;
                }
            }
            catch (Exception e)
            {
                status = "error";
                Debug.LogError($"[EditorTask] '{task}' 실행 중 예외 — {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                Application.logMessageReceived -= Capture;
            }

            if (EditorUtility.scriptCompilationFailed)
            {
                status = "compile_failed";
                _capture.AppendLine("[EditorTask] 스크립트 컴파일 실패 — 콘솔을 확인하라");
            }

            try
            {
                File.WriteAllText(LogFile, _capture.ToString());
                File.WriteAllText(DoneFile, status);
            }
            catch (Exception e)
            {
                Debug.LogError($"[EditorTask] 결과 파일을 쓰지 못했다 — {e.Message}");
            }

            _capture = null;
        }

        private static void Capture(string message, string stackTrace, LogType type)
        {
            if (_capture == null)
            {
                return;
            }
            if (type == LogType.Error || type == LogType.Exception)
            {
                _capture.Append("[ERROR] ");
            }
            else if (type == LogType.Warning)
            {
                _capture.Append("[WARN] ");
            }
            _capture.AppendLine(message);
        }

        private static void SafeDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }
    }
}
