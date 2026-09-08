@echo off
chcp 65001 >nul
setlocal

echo.
echo ====================================
echo    Jinhyeong Skill DB Exporter
echo ====================================
echo.

set "SCRIPT_DIR=%~dp0"

:: 이 폴더는 리포 루트(Assets\ 바깥)에 있어 Unity 가 통째로 무시한다.
::   입력 : _DataExporter\GameData\              (DB 원본, git 추적)
::   출력 : Assets\Resources\GameData\           (JSON)
::          Assets\Scripts\GameData\Generated\   (C# enum)
set "SOURCE_ROOT=%SCRIPT_DIR%GameData"
set "JSON_OUTPUT_PATH=%SCRIPT_DIR%..\Assets\Resources\GameData"
set "CS_OUTPUT_PATH=%SCRIPT_DIR%..\Assets\Scripts\GameData\Generated"
set "CONFIG_PATH=%SCRIPT_DIR%config.json"

where node >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERROR] Node.js 가 설치되어 있지 않습니다. https://nodejs.org
    pause
    exit /b 1
)

set "VERBOSE="
if /i "%~1"=="-v" set "VERBOSE=--verbose"
if /i "%~1"=="--verbose" set "VERBOSE=--verbose"

cd /d "%SCRIPT_DIR%"
node "%SCRIPT_DIR%export.js" all %VERBOSE%
set "RESULT=%errorlevel%"

echo.
if %RESULT% equ 0 (
    echo    [SUCCESS] Export 완료.
) else (
    echo    [ERROR] Export 실패. 위 로그를 확인하세요.
)
echo.
pause >nul
exit /b %RESULT%
