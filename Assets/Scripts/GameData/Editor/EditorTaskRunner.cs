using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Jinhyeong_GameData.Editor
{

    /// <summary>
    /// 켜져 있는 에디터에 일을 시킨다 — 에셋 새로고침 · 아트 빌드 · DB 검증.
    ///
    /// [사고] 그동안 이 일들을 `-batchmode -executeMethod` 로 돌렸다. 배치모드는 프로젝트를
    /// **배타적으로 잠그기 때문에**(Temp/UnityLockfile) GUI 에디터가 떠 있으면 두 번째 인스턴스가
    /// 못 뜬다. 그래서 매번 에디터를 죽이고 배치를 돌리고 다시 띄웠다.
    /// 리로드가 필요해서가 아니라 **잠금 때문에** 껐던 것이고, 그 대가로 부팅을 수십 번 반복했다.
    ///
    /// 배치모드를 쓸 이유가 없다. 에디터는 이미 떠 있고, 오토 리프레시도 켜져 있다.
    /// 요청 파일을 하나 두고 그 안에서 같은 함수를 부르면 된다 — 부팅이 사라진다.
    ///
    /// 사용:
    ///   echo build    &gt; Temp/editor.task     # 아트 프리팹 빌드
    ///   echo validate &gt; Temp/editor.task     # DB 검증
    ///   echo nodes    &gt; Temp/editor.task     # 스킬 노드 어휘 검사
    ///   echo refresh  &gt; Temp/editor.task     # 에셋 새로고침만
    ///   echo ui       &gt; Temp/editor.task     # 로비 골격 프리팹 굽기 + 어드레서블 등록
    ///   echo star     &gt; Temp/editor.task     # 성좌 실측 (노드/간선/값/성립성/왕복)
    ///   echo starsave &gt; Temp/editor.task     # 성좌 초안을 편집기 SSOT 로 저장
    ///   echo staredit &gt; Temp/editor.task     # 성좌 편집기 창 열기
    /// 끝나면 Temp/editor.task.log 에 결과가, Temp/editor.task.done 에 완료 표시가 생긴다.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorTaskRunner
    {
        private const string TaskFile = "Temp/editor.task";
        private const string LogFile = "Temp/editor.task.log";
        private const string DoneFile = "Temp/editor.task.done";

        /// <summary>폴링 간격(초). 사람이 기다리는 일이라 1초면 충분하다.</summary>
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
            // 플레이 중에는 하네스가 에디터를 쓰고 있다. 끼어들면 런이 깨진다.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            if (EditorApplication.timeSinceStartup < _nextPoll)
            {
                return;
            }
            _nextPoll = EditorApplication.timeSinceStartup + PollSeconds;

            // 컴파일·임포트 중에 실행하면 반쯤 갱신된 상태를 재게 된다.
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

            // 요청 파일을 먼저 지운다. 실행 중 예외가 나도 같은 일을 무한히 반복하지 않는다.
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

                    case "star":
                        AssetDatabase.Refresh();
                        Debug.Log(ConstellationCheck.Run());
                        break;

                    case "starsave":
                        AssetDatabase.Refresh();
                        Debug.Log(ConstellationCheck.SaveDraft());
                        break;

                    case "staredit":
                        // 창을 띄우고 한 번 그리게 한다. GUI 는 사람이 눌러야 도는 코드라
                        // 컴파일만으로는 「열자마자 예외」를 못 잡는다 — 열어 봐야 안다.
                        Jinhyeong_Constellation.Editor.ConstellationEditorWindow.Open();
                        break;

                    default:
                        status = "unknown";
                        Debug.LogError($"[EditorTask] 모르는 작업 '{task}' (refresh · build · validate · nodes · ui · star · starsave · staredit)");
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

            // 컴파일 실패는 로그에 안 남을 수도 있다. 상태로 따로 알린다 —
            // 「빌드가 됐는가」를 로그 문자열로 추측하게 두면 다음 사람이 틀린다.
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

        /// <summary>실행 중 올라온 로그를 모은다. 배치모드 로그파일을 대신하는 자리다.</summary>
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
                // 지우기 실패는 다음 폴링에서 다시 시도된다. 여기서 막을 이유가 없다.
            }
        }
    }
}
