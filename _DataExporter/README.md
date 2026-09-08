# Skill DB Exporter

경로 기반 모듈 DB(트리형) → Unity JSON + C# enum.

리포 루트(`Assets/` 바깥)에 있어서 Unity 가 이 폴더를 통째로 무시한다. `.meta` 안 생기고 빌드에도 안 딸려간다.

## 실행

```bash
node export.js all          # 또는 run_win.bat 더블클릭
node export.js all -v       # 상세 로그
```

외부 npm 의존성 없음. Node 14+ 만 있으면 된다.

| | 경로 |
|---|---|
| 입력 | `_DataExporter/GameData/` |
| 출력 (JSON) | `Assets/Resources/GameData/` |
| 출력 (C#) | `Assets/Scripts/GameData/Runtime/GameEnum.Generated.cs` |

경로는 `config.json` 또는 환경변수(`SOURCE_ROOT` / `JSON_OUTPUT_PATH` / `CS_OUTPUT_PATH` / `CONFIG_PATH`)로 덮어쓸 수 있다.

## DB 구조

```
GameData/
  _enums.js                        enum SSOT ('_' 접두사 = 스캔 제외, 익스포터가 직접 집는다)

  Skill/                           ── 스킬에 관한 것은 전부 여기 ──
    Master/    bark_cone.js        「이 스킬 = Method + Visual」 조합 + 트리거 + 슬롯
    Method/    instant.js          발사 방식 (공용, 9종)
               beam.js  ground_field.js  self_cast.js
               projectile/{straight,arc,homing,piercing,wave}.js
    Visual/    bark_cone.js        이미지 키 · 색 · 데미지/쿨/사거리 · 레벨 곡선
    Part/      bark_cone.js        착용 파츠
    Option/    bark_cone_area.js   파츠 옵션 (파츠당 5~7개)

  Shared/                          ── 여러 스킬이 이름으로 가리키는 것 ──
    buff/    rage.js               idTable — 런타임이 숫자 id 로 조회
    debuff/  burn.js  chill.js

  Tables/                          ── 스킬이 아닌 표 ──
    enemy/  wave/  round/  synergy/
```

### 새 스킬 하나 = 파일 넷 복붙

이 구조의 목적은 **Ctrl+C / Ctrl+V 로 스킬 하나가 뚝딱** 나오는 것이다.

```
Skill/Master/bark_cone.js   →  Skill/Master/새이름.js     id 와 name 만 바꾼다
Skill/Visual/bark_cone.js   →  Skill/Visual/새이름.js     그림 키와 수치를 바꾼다
Skill/Part/bark_cone.js     →  Skill/Part/새이름.js       skillId 를 새 id 로
Skill/Option/bark_cone_*.js →  Skill/Option/새이름_*.js   partId 를 새 id 로
```

**Master 는 Visual 을 이름으로 가리키지 않는다.** 파일 이름이 곧 연결이다 —
`Skill/Master/<이름>.js` 는 언제나 `Skill/Visual/<이름>.js` 를 본다.
적게 두면 이름과 참조가 어긋날 수 있고, 어긋나면 조용히 남의 그림이 나간다.

Method 만 이름으로 가리킨다(`method: 'instant'`). 발사 방식은 **재사용이 본질**이라
9종을 27개 스킬이 나눠 쓴다 — 여기까지 복제하면 「투사체 속도를 고치려면 27군데」가 된다.

### `@stat` 과 `@visual` 은 같은 파일을 본다

옛날에는 수치(`stat`)와 그림(`visual`)이 다른 파일이었다. 지금은 `Skill/Visual/` 하나로 합쳤고,
토큰 이름만 둘로 남겨 두었다 — **수치와 그림은 읽는 사람에게 다른 것**이기 때문이다.

```js
triggerParams: { Cooldown: '@stat.Cooldown' }      // 둘 다
params:        { Visual:   '@visual.Impact' }      // Skill/Visual/<이름>.js 를 본다
```

합친 이유: 공유하던 그림 3종(dust · tail · kibble)이 7개 스킬에 걸쳐 있었는데,
공유는 **남의 스킬을 건드릴까 봐 못 고치게 만든다.** 복붙이 목적이면 공유보다 복제가 맞다.

### 파일 경로(확장자 제외) = 모듈 ID

`Skill/Method/projectile/straight.js` → `Skill/Method/projectile/straight`.
스킬에서는 종류 폴더 기준 상대 경로로 참조한다: `method: 'projectile/straight'`.

### 노드 어휘를 늘릴 때

노드 하나를 붙이려면 두 군데를 고친다 — `_enums.js` 의 `ESkillNodeType`, 그리고 노드 클래스.
클래스에 `[BTNode(ESkillNodeType.Xxx)]` 를 붙이면 `BTNodeRegistry` 가 리플렉션으로 집으므로
등록표를 손댈 것이 없다. 받기는 하지만 아직 효과가 없으면 `Inert = new[]{ ... }` 로 신고한다.

유니티에서 **Jinhyeong/GameData/스킬 노드 어휘 검사** 를 돌리면 구현 현황을 찍고
`config.json` 의 `UnimplementedNodes` 를 실제 상태로 갱신한다. 손으로 맞출 것이 없다.

## 조립 규칙

### 왜 트리가 아니라 형제 나열인가 (제일 중요)

런타임 BT 리프는 대부분 **SkillContext 에 값을 써두는 역할**이다.

```
Targeting -> ctx.Targets      Hit     -> ctx.HitNode
Debuff    -> ctx.DebuffNodes  Despawn -> ctx.DespawnNode
```

맨 마지막에 **Launch 리프가 그 ctx 를 읽어서** 이펙트를 스폰한다. 그리고 **Launch 와 Trigger 는 자식을
실행하지 않는다.** 그래서 Hit 을 Launch 의 자식으로 매달면 영영 실행되지 않고, 스킬은 나가는데 피해가 0이 된다.

→ 전부 **형제로 나열**하고 **Launch 를 맨 뒤에** 둔다. 조립기가 `Sequence` 하나로 감싼다.
(`Cooldown`/`Chance`/`Selector`/`Inverter`/`Parallel` 은 자식을 실행하므로 중첩 가능)

```
Sequence
├ TriggerOnAttack
├ TargetNearest
├ HitSingle          ← ctx.HitNode
├ DebuffOnHit        ← ctx.DebuffNodes
├ DespawnAfterHits   ← ctx.DespawnNode
└ LaunchStraight     ← 위를 다 읽어서 실행
```

빌드가 막아주는 것:
- Launch 뒤에 노드가 남아 있음
- Launch 가 2개 이상
- Hit / Despawn 이 2개 이상 (ctx 슬롯이 하나뿐이라 뒤가 앞을 덮어씀)
- 런타임 미구현 노드 사용

### 슬롯 — 모듈이 구멍을 뚫어둔다

launch 모듈은 `nodes`(형제 목록)를 내놓고 `{ slot: '이름' }` 으로 구멍을 남긴다.
스킬이 그 자리에 노드를 꽂으면 빌드 타임에 교체된다.

```js
// modules/launch/projectile/straight.js
slots: ['onHit'],
nodes: [
    { slot: 'onHit' },
    { slot: 'onDebuff', optional: true },      // optional 이면 안 채워도 통과
    { type: 'DespawnAfterHits', params: { Value: 1 } },
    { type: 'LaunchStraight', params: { Speed: '@stat.Speed', Visual: '@visual.Projectile' } }
]
```

- 안 채운 필수 슬롯 → 빌드 실패
- 모듈이 선언 안 한 슬롯을 채움(오타) → 빌드 실패
- `launch: 'self_cast'` 처럼 Launch 노드가 아예 없는 모듈도 된다 (자버프용)

### 토큰 — 값은 모듈에서 끌어온다

`@stat.<키>` / `@visual.<키>` 는 스킬이 참조한 모듈 값으로 빌드 타임에 치환된다.
런타임 해석이 전혀 없다. 없는 키를 쓰면 빌드가 깨진다.

```js
params: { Speed: '@stat.Speed', Visual: '@visual.Projectile' }
```

모듈을 복제하지 않고 스킬 하나만 다르게 하려면 `overrides`:

```js
overrides: { stat: { Radius: 4.5 } }
```

### id 참조 — 버프/디버프도 경로로 건다

`idTable: true` 모듈은 런타임이 숫자 id 로 조회하지만, 스킬 파일에 숫자를 직접 박지 않는다.
`#<종류>/<경로>` 로 쓰면 빌드 타임에 그 모듈의 `id` 로 치환된다.

```js
{ type: 'DebuffOnHit', params: { DebuffId: '#debuff/burn' } }   // -> DebuffId: 2001
```

id 중복과 `type` enum 오타도 빌드가 잡는다 (`config.json` 의 `typeEnum`).

### 레벨 곡선

`stat` 모듈의 `levels` 는 함수 또는 배열.

```js
levels: level => ({ Damage: 10 + (level - 1) * 3 })     // JS
levels: [ { level: 1, Damage: 10 }, { level: 2, ... } ] // JSON/엑셀 등 함수 못 쓰는 소스
```

`maxLevel` 만큼 펼쳐서 `Skill.json` 의 `levels` 로 나간다.

## 확장

### 1. 새 소스 포맷 (확장자)

`readers/<이름>.reader.js` 파일 하나 추가하면 끝. 파이프라인은 안 건드린다.

```js
module.exports = {
    name: 'xlsx',
    extensions: ['.xlsx'],
    read(filePath, ctx) { /* ... */ return object; }
};
```

같은 DB 트리 안에서 포맷을 섞어 쓸 수 있고, **확장자만 바꿔도 모듈 ID 가 같아서 참조가 그대로 물린다**
(`lightning.js` → `lightning.json` 으로 교체해도 출력물 동일). 단 같은 ID 가 두 확장자로 동시에 존재하면 빌드 실패 —
어느 쪽이 이겼는지 모르는 상태가 제일 위험하다.

### 2. 새 모듈 종류

`config.json` 의 `Modules` 에 한 줄 추가 + 폴더 생성.

```json
"debuff": { "dir": "modules/debuff", "table": "SkillDebuff" }
```

### 3. 새 노드 타입 / 파라미터

`GameData/_enums.js` 에 추가 + C# 노드 구현이 세트. enum 에 없는 값은 빌드가 막는다.

## 출력

| 파일 | 내용 |
|---|---|
| `Skill.json` | 조립 완료된 스킬. 슬롯·토큰 다 해석된 중첩 트리 + 레벨 표 |
| `SkillBuff.json` / `SkillDebuff.json` | id 로 조회하는 상태이상 테이블 |
| `SkillVisual.json` / `SkillLaunch.json` / `SkillStat.json` | 모듈 원본 (툴링·디버그용. 런타임이 읽지 않는다) |
| `GameEnum.Generated.cs` | `_enums.js` → C# enum (`namespace Jinhyeong_GameData`) |

검증 에러가 하나라도 있으면 **아무 파일도 쓰지 않는다.** 반쯤 갱신된 출력이 제일 나쁘다.
내용이 같으면 파일을 건드리지 않아 Unity 가 불필요하게 리임포트하지 않는다.

## 런타임 (C#)

| | |
|---|---|
| `Jinhyeong_GameData.Json.JsonValue` | 경량 JSON DOM. JsonUtility 가 중첩 재귀 + 값 타입 섞인 맵을 못 다뤄서 직접 뒀다 |
| `SkillRegistry.Load()` | `Resources/GameData/Skill.json` → `SkillDefinition` (중첩 트리 그대로) |
| `SkillBuffRegistry.Load()` | `SkillBuff.json` / `SkillDebuff.json` |
| `BTBuilder.Build(SkillNodeData root)` | 중첩 트리를 따라 내려가며 실행 노드 생성 |

**레벨 수치가 params 보다 우선한다.** `params` 에는 빌드 타임에 1레벨 기준값이 박혀 있어서,
`SkillNodeData.GetFloat` 가 레벨을 먼저 보지 않으면 성장이 통째로 무시된다.
단 레벨 modifier 는 ParamKey 단위라 같은 키를 쓰는 트리 내 모든 노드에 적용된다 —
한 스킬 안에서 노드별로 다르게 성장시켜야 하면 그때 노드 단위 modifier 를 추가해야 한다.

검증: Unity 메뉴 `Jinhyeong > GameData > DB 검증` (배치모드에서는
`-executeMethod Jinhyeong_GameData.Editor.GameDataValidator.Validate`).
스킬/노드/레벨 수, 참조 비주얼 키, 죽은 버프·디버프 id 를 찍는다.

## 상태이상 스택 정책

buff/debuff 모듈은 같은 효과가 다시 걸렸을 때의 처리를 데이터로 정한다.

```js
stackMode: 'Refresh',  maxStacks: 1   // 화상 — 지속시간만 갱신 (기본)
stackMode: 'Stack',    maxStacks: 3   // 중독 — 3중첩까지 누적
stackMode: 'Ignore',   maxStacks: 1   // 이미 걸려 있으면 무시
```

빔처럼 초당 여러 번 때리는 스킬이 화상을 수십 개 쌓던 문제가 여기서 통제된다.
`stackMode` 오타는 물론 `'Refresh'` 인데 `maxStacks: 3` 같은 모순도 빌드가 막는다.

런타임은 `Damageable.Status`(`StatusList`)가 들고 있다. 컴포넌트가 아니라서
적 프리팹을 고칠 필요도, 런타임 `AddComponent` 도 없다.

## 런타임 컴포넌트 구조

```
Skill_Root.prefab       코드 산출물 1개 (Addressable "Skill_Root")
├ SkillConfig    stat/노드 데이터 + 수명 + 풀 반환
├ SkillMethod    이동 (Instant·Linear·Arc·Curve·Homing)   Update
├ SkillHit       판정·피해·소멸조건                        LateUpdate
└ SkillVisual    아트 자식 부착 · 스케일 · 연쇄 라인
    └ VFX_*.prefab   데이터 산출물 N개, 스크립트 0개
```

DB 축과 1:1이다 — `stat`→SkillConfig, `launch`→SkillMethod, Hit 노드→SkillHit, `visual`→SkillVisual.
스킬을 하나 더 찍는 데 C# 을 건드리면 그 순간 이 설계가 깨진 것이다.

**`Update`/`LateUpdate` 로 순서를 고정한 이유**: 이동이 끝난 뒤에 판정이 돌아야
이번 프레임의 이동 구간(`PrevPos` → 현재 위치)으로 스윕 판정을 할 수 있다.
스크립트 실행 순서 설정 같은 취약한 장치를 쓰지 않는다.

**아트 프리팹에는 스크립트를 넣지 않는다.** 아티스트가 만든 프리팹이나 에셋 스토어 것을
그대로 꽂을 수 있어야 하기 때문이다. 로직은 전부 `Skill_Root` 에 있다.

짧은 연출(`Fx_Hit`, `Fx_Chain`)도 같은 원칙 — 프리팹에 컴포넌트가 박혀 있고 `Pool_Fx` 로 재사용된다.
런타임 `AddComponent` 와 `new Material` 은 스킬 시스템 전체에서 0건이다.

## 도구가 만드는 데이터 — `_raw/`

사람이 표에 적는 것이 아니라 **도구로 그리는** 데이터가 있다 (성좌 그래프).
도구는 `GameData/_raw/*.json` 에만 쓰고, 그것을 `Assets/Resources/GameData/` 로 옮기는 것은
`config.json` 의 `RawTables` 가 한다. 폴더 이름이 `_` 로 시작해 모듈 스캔에서는 빠진다.

도구가 최종 위치에 직접 쓰지 않는 이유는, 그러면 「도구로 만든 것」과 「손으로 고친 것」이
서로 다른 경로로 들어가 결과가 갈리기 때문이다. 굽는 곳은 여기 한 곳이다.

| 테이블 | 원본 | 만드는 것 |
|---|---|---|
| `ConstellationGraph` | `_raw/ConstellationGraph.json` | `Jinhyeong/성좌 편집기` |

빈 입력은 거부한다 — 통과시키면 다음 실행에서 데이터가 통째로 사라진다.
