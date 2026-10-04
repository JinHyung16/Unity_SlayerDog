# 개많이잡아 — Unity Skill System Module

2D 탑다운 서바이버. 스킬은 코드가 아니라 데이터에 적은 모듈 조합(행동 트리)으로 만든다.

이 저장소의 목표는 게임 하나가 아니라, 기획서를 넣으면 코어 플레이까지 만드는 파이프라인이다. 이 게임은 그 첫 실증 대상이다.

## 개요

| 항목 | 내용 |
|---|---|
| 코어 루프 | 적 처치, 파츠 획득, 스킬과 시너지로 강화 |
| 세션 | 12라운드 × 60초. 보스 처치 시 클리어, 사망 시 종료 |
| 뷰 | XZ 평면 3D 월드 + 오쏘그래픽 쿼터뷰, 스프라이트 빌보드 |
| 조작 | 키보드 이동. 스킬 자동 발동, 자동 조준 |
| 플랫폼 | PC 1280×720 |

## 기술 스택

| 분류 | 사용 |
|---|---|
| 엔진 | Unity 6000.3, URP 17.3 |
| 비동기 | UniTask |
| 로드 | Addressables 2.3 |
| 데이터 | 로컬 JS, Node 익스포터, JSON + 생성 enum |
| 아트 | Python 절차 생성 + 외부 에셋 팩 |
| 프레임워크 | `com.jinhyung.gameframework`, `com.translation.unity` (UPM). 이관 예정, `TASKS.md` W-1 |

## 스킬 데이터 흐름

```
_DataExporter/GameData/*.js
  node export.js            검증에 실패하면 아무것도 쓰지 않는다
Assets/Resources/GameData/*.json + *.Generated.cs
  Registry.LoadAsync
SkillCompiler -> CompiledSkill      SkillModifier 가 파츠 옵션과 시너지를 곱한다
SkillObject -> TargetingResolver -> LaunchExecutor -> SkillEffect -> Damageable
```

콘텐츠를 늘릴 때 C# 을 고치지 않는다. 노드는 `[BTNode]` 어트리뷰트로 자기 타입을 등록한다.

## 실행

1. Unity 6000.3 으로 연다
2. 데이터를 굽는다: `cd _DataExporter && node export.js`
3. `Assets/Scenes/Main.unity` 에서 Play. 로비가 열린다
4. 이동은 WASD 또는 방향키. 스킬은 자동으로 나간다

아트를 다시 구웠으면 `python _DataExporter/tools/build_meta.py` 를 반드시 이어서 돌린다. 안 돌리면 GUID 가 새로 잡혀 참조가 끊긴다.

## 폴더

```
CLAUDE.md                         작업 규칙 (볼트로 보내는 라우터)
TASKS.md                          작업 큐 (미완료만)
.claude/spec.md                   이 게임의 확정값과 실측값
.claude/history/YYYY-MM-DD.md     완료 기록
.claude/skills/unitygamedev/      스킬 스텁 (발화용, 본문은 볼트)
.claude/tools/                    문서 감사 · 코드 감사
_DataExporter/                    데이터 원본, 익스포터, 아트 생성기
Assets/Scripts/<시스템>/           namespace Jinhyeong_<시스템>
```

개발 방법론(게임과 무관)은 저장소 밖 옵시디언 볼트 `C:\jinhyeong_project\Jinhyeong_Obsidian\Dev_Unity\` 에 있다. 시작 노트는 `00 시작/Dev_Unity 지도.md`.

## 문서

| 알고 싶은 것 | 어디 |
|---|---|
| 작업 규칙 | [CLAUDE.md](CLAUDE.md) |
| 다음 할 일 | [TASKS.md](TASKS.md) |
| 현재 수치 | [.claude/spec.md](.claude/spec.md) |
| 스킬 데이터 작성 | [_DataExporter/README.md](_DataExporter/README.md) |
| 코드 컨벤션 | 볼트 `Dev_Unity/40 규약/규약 - 코드 컨벤션.md` |
| 개발 절차 | 볼트 `Dev_Unity/00 시작/개발 러너.md` |

## 라이선스

MIT. `LICENSE` 참고.
