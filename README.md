# 개많이잡아 — Unity Skill System Module

> **12라운드를 버티며 파츠를 주워 스킬을 조립하는 2D 탑다운 서바이버.**
> 스킬 하나하나는 코드가 아니라 **로컬 DB에 정의한 모듈 조합(행동 트리)** 으로 만들어진다 —
> 데이터만 고쳐 새 스킬을 얼마든지 찍어낼 수 있는 **데이터 주도형 스킬 시스템**이 핵심.

엔진: **Unity 6 (6000.3) / URP**
플레이 한 줄 요약: 키보드로 이동 · 스킬은 전부 자동 발동 · 드랍된 **파츠 6칸**을 갈아끼우며 12라운드 보스까지.

이 저장소는 게임 하나가 아니라 **「기획서를 넣으면 코어 플레이가 도는 것까지 뽑는 파이프라인」** 을
만드는 중이다. 게임은 그 파이프라인의 첫 번째 실증 대상이다.

---

## 1. 프로젝트 한눈에

- **코어 루프**: 적을 처치해 **파츠**를 얻고, 파츠가 스킬과 **시너지**를 만들어 더 강해진다.
  스킬은 슬롯 키가 아니라 파츠 장착으로 붙고, 발동은 전부 자동이다.
- **세션**: 12라운드 × 60초 = 720초. 12라운드 보스(납치선) 처치 = 클리어, 사망 = 종료.
- **장르 / 카메라**: 3D 월드(XZ 평면) 위에 **오쏘그래픽 쿼터뷰** — 스프라이트 빌보드, PPU 32.
  충돌이 원래부터 XZ 2D 라 3D → 2D 전환에서 게임플레이 로직을 고칠 것이 거의 없었다.
- **무엇을 보여주는가**
  1. 로컬 JS → JSON + enum → 컴파일된 BT 노드 → 런타임 발동까지 이어지는 **데이터 파이프라인**
  2. 역할(Trigger / Targeting / Method / Hit / Despawn / Buff)을 **직교**로 분해한 스킬 구조
  3. **한 폴더 = 한 스킬** — 폴더를 복사하면 스킬 하나가 복제된다
  4. 물리 엔진 없이 직접 짠 **원 vs OBB 2D 충돌**과 경량 **Behavior Tree**
  5. 사람이 클릭하지 않아도 도는 **요청 파일 기반 자동 검증 하네스** (실측 리포트 + 스크린샷)

---

## 2. 기술 스택

| 분류 | 사용 기술 |
|---|---|
| Engine | Unity 6000.3 |
| Render | Universal Render Pipeline 17.3 (PC / Mobile RP Asset 분리) |
| Async | UniTask (`Assets/Plugins/UniTask`) |
| Load | Addressables 2.3 — 그룹 `Skill` · `UI` · `Character` · `Default` |
| Data | 로컬 JS 모듈 → Node 익스포터 → JSON + enum (구글 시트는 레거시) |
| Art | Python 절차 생성 (`_DataExporter/tools/build_*.py`) + 외부 팩 반입 |
| Code | C# / .NET Standard 2.1 |
| Platform | PC · 가로 1280×720 기준 |

---

## 3. 설계 원칙

「동작하는 것」이 아니라 **「바뀔 때 덜 깨지는 것」** 을 우선했다.

### 3.1 직교성
스킬을 역할 노드로 쪼개 **한 축이 바뀌어도 다른 축은 무관**하다.
새 Hit 패턴을 추가해도 Method / Targeting / Despawn 코드는 안 건드린다.

### 3.2 콘텐츠는 C# 을 고치지 않는다
스킬·적·라운드·시너지를 늘릴 때 코드를 고치지 않는다. `_DataExporter/GameData/` 에 파일을 더하고 굽는다.
클라이언트에는 **스킬별 분기가 하나도 없다** — 노드 타입별 범용 실행기만 있다.

### 3.3 노드는 자기 타입을 스스로 신고한다
전에는 노드 하나를 붙이려면 세 군데(enum · 빌더 switch · 노드 클래스)를 고쳐야 했고,
**switch 를 빠뜨려도 컴파일은 통과하고 런타임에 조용히 아무 일도 안 했다.**

```csharp
[BTNode(ESkillNodeType.HitArea)]        // 클래스가 자기 타입을 신고한다
public sealed class HitLeafNode : BTNode
```

`BTNodeRegistry` 가 리플렉션으로 수집한다. **받는 것 ≠ 동작하는 것**이라,
값을 받지만 아무도 안 읽는 노드는 어트리뷰트의 `Inert` 로 따로 표시한다.

### 3.4 보정은 조회 시점에 곱한다
파츠 옵션·시너지가 「이 스킬만」 강화하는데, 적용점은 `SkillNodeData.GetFloat/GetInt` **한 곳뿐**이다.
공유물인 노드 데이터에 값을 굽지 않고 조회할 때 곱해서 내보낸다 —
발사체는 `SkillConfig` 가 자기 보정을 들고 있어 **날아가는 중에 파츠를 바꿔도 그 탄은 안 변한다.**

### 3.5 이벤트 허브
Player / CameraFollow / EnemySpawner 는 서로를 직접 참조하지 않고 `GameEvents` 로만 만난다.
`CurrentPlayer` 캐시 + 구독 즉시 1회 호출 → **Awake/OnEnable 순서 무관**.

> 「생성됨」 이벤트만 구독하고 현재 값을 안 보면, 이미 생성된 뒤에 구독한 쪽은 영영 못 받는다.
> 드랍 관리자가 실제로 이 사고를 냈고 「라운드 2에서 죽는다」로 잘못 읽혔다.

### 3.6 팀 검사는 길목 한 곳에서
`Damageable.TakeDamage` 입구에서 같은 팀 출처를 센다. **무적 판정보다 앞**이라 막힌 시도도 잡히고,
스킬뿐 아니라 자동공격·지속피해 등 모든 경로를 덮는다.
호출처마다 흩어 두면 새 경로가 생길 때마다 아군 오사가 재발한다.

### 3.7 GC 최소화
- `SkillObject._scratchCtx` 하나를 매 발사마다 `Reset()` 재사용 — 발사 1회당 alloc 0
- `Damageable.All` 정적 리스트 자가 등록 + 외부 버퍼 채우기
- 적 200개 · FX 풀 사전 생성. `SpriteAnimator` 는 Animator 없이 배열 인덱싱으로 돈다
  (적 수백 마리에서 컨트롤러 평가 비용을 내지 않으려고 그렇게 만들었다)

---

## 4. 시스템 아키텍처

```
        _DataExporter/GameData/*.js          SSOT. 사람이 고치는 유일한 곳
                    │  node export.js  (검증 통과 못 하면 아무것도 안 쓴다)
                    ▼
        Assets/Resources/GameData/*.json  +  *.Generated.cs (enum · 데이터 클래스)
                    │  Registry.LoadAsync
                    ▼
     ┌───────────────────────────────────────┐
     │  SkillRegistry · PartRegistry ·        │
     │  SynergyRegistry · RoundRegistry ·     │
     │  EnemyTypeRegistry · WaveEventRegistry │
     └───────────────┬───────────────────────┘
                     │ SkillCompiler.Compile(def, level)
                     ▼
               [CompiledSkill] ◀── SkillModifier ◀── PartLoadout (파츠 옵션 + 시너지)
                     │
   ┌─────────────────┼──────────────────┐
   ▼                 ▼                  ▼
[SkillObject]  [TargetingResolver]  [LaunchExecutor]
 자동 발동 컨테이너   → ctx.Targets       풀에서 VFX 스폰
                                         │
                                         ▼
                                    [SkillEffect]   이동 · Hit · Despawn
                                         │
                                         ▼
                                    [Damageable]    Hp / Shield / Stunned / OnDied
                                         │
                         ┌───────────────┴───────────────┐
                         ▼                               ▼
                  [EnemySpawner] 풀 반환          [PartDropper] 파츠 상자

      ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

  [GameFlow]  Bootstrap → OpenLobby → BeginRun → (사망 | ClearGame) → EndGame
       │
       ├─ [WaveDirector]   라운드 12개 · 웨이브 이벤트 · 보스
       ├─ [UIManagement]   Window / Panel / Component + 밴드 9종
       └─ [GameEvents]     Player ↔ Camera ↔ Spawner 디커플링
```

---

## 5. 주요 모듈

### 5.1 Skill System (`Assets/Scripts/SkillSystem/`, 62 파일)

스킬 한 개 = **행동 트리** 하나. 노드를 역할별로 조합해 정의한다.

| 역할 | 노드 타입 |
|---|---|
| 흐름 제어 | `Sequence` `Selector` `Parallel` `Inverter` `Cooldown` `Chance` |
| 발동 조건 | `TriggerOnTick` `TriggerOnAttack` |
| 대상 선정 | `TargetSelf` `TargetNearest` `TargetFarthest` `TargetRandom` `TargetAll` `TargetNearestForward` `TargetCone` |
| 발사 방식 | `LaunchInstant` `LaunchStraight` `LaunchArc` `LaunchCurve` `LaunchHoming` |
| 소멸 조건 | `DespawnAfterTime` `DespawnAfterHits` |
| 타격 | `HitSingle` `HitArea` `HitBeam` `HitChain` `HitDeathBurst` |
| 상태 변화 | `BuffSelf` `DebuffOnHit` |

미구현 3종(`TriggerOnOreBreak` · `DespawnAfterBounces` · `DespawnOnWall`)은 **문서가 아니라
`_DataExporter/config.json` 이 관리**하고, 에디터의 「스킬 노드 어휘 검사」가 실제 코드를 스캔해 갱신한다.

**리프는 ctx 에 쓰기만 하고, 마지막 Method 노드가 읽어서 실행한다.**
Targeting 이 `ctx.Targets` 를, Hit 이 히트 규칙을, Despawn 이 소멸 규칙을 채워 두면
`LaunchLeafNode` 가 그것을 모아 한 번에 발사한다. Launch/Trigger 는 자식을 실행하지 않는다.

**핵심 파일**
- `SkillRegistry.cs` — 멱등 `LoadAsync`, 동시 호출 시 같은 Task 공유
- `SkillCompiler.cs` — 노드 리스트를 역할 슬롯으로 평탄화
- `Runtime/SkillObject.cs` — 시전자 컨테이너. 파츠가 준 스킬은 `CompiledSkill.FromPart` 로 출처를 남긴다
- `Runtime/SkillModifier.cs` — 옵션·시너지가 같은 문(`AddTrait`)으로 들어온다. 문이 둘이면 한쪽만 고치는 실수가 난다
- `Runtime/BT/BTNodeRegistry.cs` — 어트리뷰트 리플렉션 수집

### 5.2 Parts & Synergy (`Assets/Scripts/Parts/`)

- **슬롯 6칸, 부위 제약 없음.** 부위(6칸)와 계열(6종)이 충돌하면 계열 시너지 대부분이 도달 불가능해진다
  — 시선 계열 3종이 전부 「머리」라 아예 불가능했다
- 같은 스킬을 또 주우면 옵션을 **합친다**(상한 6). 교체만 하면 「깊이」 축이 사라진다
- `PartLoadout.RebuildSynergies()` — 구성이 바뀌면 **착용 중인 모든 스킬**의 보정을 다시 만든다.
  계열 시너지는 이미 낀 스킬에도 걸리므로 새로 낀 것만 갱신하면 틀린다
- 시너지 3축: **넓게**(계열 2/4) · **깊게**(한 스킬 증강 4+) · **연계**(거는 스킬 + 때리는 스킬)

### 5.3 Collision (`Assets/Scripts/Collision/`)

Unity 물리를 쓰지 않는다. **XZ 평면 2D 자작 충돌** — 이동체는 원, 벽은 `OBBCollider`.
판정은 `OBBPhysics` 의 원 vs OBB 하나뿐이고, 콜라이더를 가진 것은 벽뿐이다.
이 선택 덕에 3D 퍼스펙티브 → 2D 오쏘 전환에서 게임플레이 코드를 고칠 일이 없었다.

### 5.4 AI / Behavior Tree (`Assets/Scripts/AI/`)

외부 패키지 없이 경량 BT 직접 구현. 모든 액션이 1프레임 `Success` 를 반환해
Selector 가 매 프레임 위에서 재평가한다 → HP 가 떨어지면 추격 중에도 즉시 전환된다.

- `WaveDirector` — 라운드 12개를 데이터로 진행. 스폰 간격·묶음·동시 상한·적 배수를 **라운드가** 정한다
- `EnemyCullingSystem` — 화면 밖 개체의 갱신 등급을 낮춘다
- `Enemy` 는 Spawner 를 **모른다.** `Damageable.OnDied` 이벤트로만 통지된다

### 5.5 UI Framework (`Assets/Scripts/UI/Runtime/Framework/`)

**규약은 문서가 아니라 상속으로 강제된다.** 문서에 규약만 있고 베이스 클래스가 없던 시절,
로비는 `Awake` 에서 화면 전부를 코드로 그렸고 프리팹은 빈 껍데기였다.

| 계층 | 계약 |
|---|---|
| `BaseWindow` | `await UIManagement.OpenAsync(Key)` 로 세우고 호출자가 `Open()`. **Awake·OnEnable 에 논리 없음 — 열 때 채우고 닫을 때 비운다** |
| `BasePanel` | 탭이 처음 눌릴 때 `PrefabAuto.CreateForUI` 로 게으르게 생성 |
| `BaseComponent` | `PrefabAuto<T>` 로 꺼내고 `UILoader.Restore` 로 풀에 반환. `Clear()` 가 값·리스너를 지운다 |
| `UIManagement` | 씬 수명. 깊이 배정 · 전체 화면 창 아래 Canvas 끄기 · 중앙 틱 · 씬 이탈 시 `ClearAll` |

밴드: Passive 0 · PassiveClosable 50 · Normal 100 · LowDepth 300 · Popup 400 · Global 600 · Tutorial 700 · Top 800 · SystemPopup 900.

**프리팹은 손으로 만들지 않는다** — `UIPrefabBuilder` 가 굽고 SerializeField 를 묶는다.
같은 경로에 덮어써 GUID 가 유지된다.

### 5.6 Constellation (`Assets/Scripts/Constellation/`)

아웃게임 성장. **노드를 데이터로 들고 있지 않는다** — 계열 표 6행과 상수 한 벌에서 시드로 매번 똑같이 만든다.
노드 200개를 표에 적으면 「갈래를 조금 벌려 보자」가 200줄 수정이 되고,
값을 한 번 조정할 때마다 이름 200개가 거짓말이 된다.

손으로 고칠 자리가 필요해 편집기를 따로 뒀다. **모델은 방향 없는 그래프 하나**이고,
간선이 곧 「살 수 있는 순서」다. 도구는 최종 형식을 직접 쓰지 않고 `_raw/` 중간 형식에만 쓴다 —
굽는 곳이 `export.js` 한 곳이어야 「도구로 만든 것」과 「손으로 고친 것」의 결과가 갈리지 않는다.

노드 207 · 간선 237 · 도달 207/207 · 겹침 0.

### 5.7 자동 검증 하네스 (`Assets/Scripts/GameData/Editor/`)

**사람이 클릭하지 않아도 돈다.** 에디터가 떠 있는 상태에서 요청 파일을 쓰면 결과가 파일로 나온다.

| 요청 | 하는 일 | 결과 |
|---|---|---|
| `Temp/runloop.request` ← `god:60` / `mortal:820` | 무적/필멸 플레이 실측 | `runloop.report` + `runloop_*.png` |
| `Temp/editor.task` ← `build` `validate` `nodes` `ui` `star` … | 에셋 빌드 · DB 검증 · 노드 어휘 검사 | `editor.task.log` |

- 배치모드는 프로젝트를 **배타적으로 잠그고**, 헤드리스라 프레임 진행이 달라
  같은 하네스가 배치 5킬 / GUI 55킬을 낸다. **밸런스 측정에 쓸 수 없다** — 로직 회귀 전용이다
- 하네스는 회피 조작을 한다. 「가장 가까운 아이템으로 직진」만 하던 시절엔 젬이 적 시체 위에 떨어져
  매번 적 무리 한가운데로 걸어 들어갔고, **실측 전체의 전제가 틀려 있었다**

---

## 6. 폴더 구조

```
TASKS.md                        작업 큐 — 미완료만. 다음에 뭘 할지
docs/
├─ spec/                        현재 상태 — 확정표 · 데이터구조 · 밸런스 실측치
└─ history/YYYY-MM-DD.md        과거 — 큐를 비운 날 봉인 (확정/산출/실측/발견)

_DataExporter/                  git 추적 밖. 데이터와 생성기 전부가 여기 있다
├─ GameData/
│  ├─ Skill/{Master,Method,Visual,Part,Option}/   한 폴더 = 한 스킬
│  ├─ Shared/{buff,debuff}/
│  ├─ Tables/{constellation,enemy,round,synergy,wave}/
│  ├─ _raw/                     편집기 산출물 (성좌 그래프)
│  └─ _enums.js                 `_` 접두 = 헬퍼. 먼저 읽힌다
├─ tools/build_*.py             아트 절차 생성 (후처리로 반드시 build_meta.py)
└─ export.js                    굽기 — 검증이 본체다

Assets/
├─ Resources/
│  ├─ GameData/                 익스포터 결과 JSON (런타임이 읽는 것)
│  ├─ GoogleSheetData/          레거시 시트 sync 결과물 (건들지 않는다)
│  └─ UI/  Lobby/  Sfx/
├─ _Art/{Sprites,FX,Tiles,UI,Prefabs,Materials,Fonts}
└─ Scripts/
   ├─ AI/               Jinhyeong_AI            Enemy · EnemyAI · Spawner · WaveDirector · Culling
   │  └─ BehaviorTree/  Jinhyeong_AI.BehaviorTree
   ├─ Character/Runtime Jinhyeong_Character     Player · Motor · Facing · AutoAim · CameraFollow
   ├─ Collision/        Jinhyeong_Collision     OBBCollider · OBBPhysics
   ├─ Common/           Jinhyeong_Common        CommonConfig 전역 상수
   ├─ Constellation/    Jinhyeong_Constellation 생성기 · 검증기 · 편집기
   ├─ GameData/         Jinhyeong_GameData      Registry 생성물 + Editor 하네스
   ├─ GoogleSheetDataLoader/                    레거시. 사용자 관리 영역
   ├─ Input/Runtime     Jinhyeong_Input         IInputProvider 추상화
   ├─ Managers/         Jinhyeong_Managers      GameEvents · Pool · Addressable · Sfx
   ├─ Parts/Runtime     Jinhyeong_Parts         PartLoadout · PartInstance · Dropper
   ├─ SkillSystem/      Jinhyeong_SkillSystem   스킬 데이터 + 런타임 + BT
   ├─ UI/               Jinhyeong_UI            Framework · Lobby · HUD
   └─ View/Runtime      Jinhyeong_View          SpriteAnimator · TilemapStreamer · Billboard
```

**컨벤션**: 폴더 = 네임스페이스 1:1. 모든 namespace 에 `Jinhyeong_` 접두어.

---

## 7. 빌드 / 실행

```bash
cd _DataExporter && node export.js
```

1. Unity Hub 에서 **Unity 6000.3** 으로 연다.
2. `Assets/Scenes/Main.unity` 오픈 → `Play`. `GameFlow.Bootstrap` 이 로비를 연다.
3. 로비 4탭 — **시작** · **캐릭터**(슈퍼독 3종 + 사료 강화) · **스킬 트리**(성좌) · **설정**.
4. 조작
   - **WASD / 방향키** 이동 (설정에서 프리셋 교체)
   - **스킬은 전부 자동 발동.** 조준은 `AutoAim` 이 걷는 방향과 분리해 굴린다
   - 해상도 1280×720(기본) · 1600×900 · 1920×1080 + 전체화면

> 아트를 새로 구우려면 `python _DataExporter/tools/build_<대상>.py` 후 **반드시** `build_meta.py`.
> `.meta` 를 같이 만들지 않으면 GUID 가 새로 잡혀 참조가 끊긴다.

---

## 8. 스킬 제작 가이드

목적 한 줄: **Ctrl+C / Ctrl+V 로 스킬 하나가 뚝딱 나오는 것.**

### 8.1 한 폴더 = 한 스킬

```
_DataExporter/GameData/Skill/
  Master/bark_cone.js     「이 스킬 = Method + Visual」 조합 + 트리거 + 슬롯
  Visual/bark_cone.js     이미지 키 · 색 · 수치 · 레벨 곡선
  Part/bark_cone.js       착용 파츠 (계열 · 표시명 · 거는 상태이상)
  Option/bark_cone_*.js   파츠 옵션
  Method/instant.js       발사 방식 — 여기만 공용 (9종을 27개가 나눠 쓴다)
```

- **Master 는 Visual 을 이름으로 가리키지 않는다. 파일 이름이 곧 연결이다.**
  적게 두면 어긋날 수 있고, 어긋나면 조용히 남의 그림이 나간다
- **Method 만 이름으로 가리킨다.** 발사 방식은 재사용이 본질이라
  여기까지 복제하면 「투사체 속도를 고치려면 27군데」가 된다
- 그림·수치는 **공유하지 않는다.** 공유는 남의 스킬을 건드릴까 봐 못 고치게 만든다

### 8.2 트리 표준형

```
Sequence
├─ Trigger      TriggerOnTick / TriggerOnAttack      발동 게이트
├─ Targeting    TargetNearest 등                     → ctx.Targets · Direction
├─ Hit          HitArea 등                           → 히트 규칙 등록
├─ Despawn      DespawnAfterHits 등                  → 소멸 규칙 등록
├─ (선택)       DebuffOnHit / BuffSelf
└─ Method       LaunchInstant 등                     → VFX 스폰 + 위 규칙 적용
```

Sequence 는 자식이 하나라도 `Failure` 면 멈춘다 → **Trigger 가 실패하면 그 프레임은 발동 안 함.**

### 8.3 레벨 스케일

노드의 값을 **비워두면** 그 param 은 레벨 곡선을 따라간다(레벨업 강화). 값을 넣으면 고정값.
파츠 옵션·시너지 보정은 그 위에 `SkillModifier` 로 곱해진다.

적용 순서 고정: `기본값 × 카드레벨 배율 → 파츠 옵션 → 시너지`.

### 8.4 반영 절차

```bash
cd _DataExporter && node export.js
```

굽기는 **검증이 본체다** — 통과 못 하면 아무것도 안 쓴다.
그다음 `Temp/editor.task` 에 `validate` 로 DB 검증, `build` 로 프리팹·Addressable 등록.

새 `Visual` 키를 썼다면 그림을 먼저 굽는다 — `build_signfx.py`(간판형) 또는 `build_skillfx.py`.
**흰색으로만 그린다.** 색은 DB(`SkillVisual.color`)가 런타임에 틴트한다.

---

## 9. 아트 규격

절차 생성이라 규격이 코드에 있다. 이 값들이 없으면 그림을 다시 그리게 된다.

| 항목 | 값 |
|---|---|
| PPU | 32 (64px = 2유닛) |
| 외곽선 | 그리지 않는다. **실루엣을 1px 팽창** — 두께가 항상 일정 |
| 부위 | 레이어로 나눠 각각 팽창. 하나로 씌우면 팔다리가 묻힌다 |
| 접지 그림자 | 필수. 없으면 탑다운에서 떠 보인다 |
| 명암 | 3단 (1.16 / 1.0 / 0.82) · 잉크 비중 32~37% |
| 이펙트 크기 | 반경 있는 것은 판정 반경 그대로. **반경 없는 장식만** 1.45배 |
| 이펙트 방향 | 데이터가 정한다 (`VisualOrient`) — 코드가 추측하지 않는다 |
| 걷기 | 몸통 상하 이동 필수. 4프레임이면 포즈가 4개여야 한다 |

이펙트는 **외곽만 따서 실루엣으로 승부한다** — 스킬 이름이 그림 한 장으로 읽혀야 한다.
글로우 → 어두운 외곽 → 밝은 안쪽선 세 겹이라 밝은 바닥과 어두운 바닥 양쪽에서 형태가 남는다.

---

## 10. 현황

- 밸런스: 필멸 3회 중 **클리어 2 / 라운드 11 사망 1.** 100% 를 유지하는 판이 없다
- 남은 편차는 수치가 아니라 **파츠 뽑기**에서 온다
- 아직 안 잡힌 것: 키보드 실조작 확인(하네스는 입력을 우회한다) · 빌드 FPS 실측(전부 에디터 값) ·
  `SurvivorHud` 만 UI 프레임워크 밖 · 성좌 환불 없음

다음 할 일은 [TASKS.md](TASKS.md), 현재 상태는 [docs/spec/](docs/spec/),
지나온 것은 [docs/history/](docs/history/INDEX.md) 에 있다.

---

## 11. 라이선스

개인 학습용 프로젝트.
