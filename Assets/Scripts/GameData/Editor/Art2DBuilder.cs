using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Tilemaps;
using Jinhyeong_Managers;
using Jinhyeong_Parts;
using Jinhyeong_SkillSystem;
using Jinhyeong_View;

namespace Jinhyeong_GameData.Editor
{
    /// <summary>
    /// 2D 에셋을 프리팹으로 굽는 빌더.
    ///
    /// 프리팹 YAML 을 손으로 쓰지 않는 이유: 스프라이트 참조는 GUID+fileID 쌍이라
    /// 하나만 틀려도 조용히 빈 참조가 되고, 에디터를 열기 전엔 알 수가 없다.
    /// 코드로 만들면 Unity 가 참조를 직접 채우므로 틀릴 수가 없다.
    ///
    /// 메뉴: Tools/개많이잡아/2D 에셋 빌드
    /// </summary>
    public static class Art2DBuilder
    {
        private const string SpriteDir = "Assets/_Art/Sprites";
        private const string FxDir = "Assets/_Art/FX";

        /// <summary>HP 바를 머리 위 어디에 둘지(월드 유닛). 스프라이트가 2유닛이라 그 위로 올린다.</summary>
        private const float HpBarHeight = 2.65f;
        private const string TileDir = "Assets/_Art/Tiles";
        private const string PrefabDir = "Assets/_Art/Prefabs/Gen";
        private const string MaterialDir = "Assets/_Art/Materials";

        /// <summary>스프라이트를 그리는 정렬 레이어. 바닥보다 위, UI 보다 아래.</summary>
        private const int OrderCharacter = 10;
        private const int OrderFx = 20;
        private const int OrderPickup = 5;

        // FX 폴더명 -> Addressable 주소. DB(SkillVisual)가 쓰는 키와 반드시 같아야 한다.
        private static readonly Dictionary<string, string> FxAddress = new Dictionary<string, string>
        {
            { "fx_hit", "Fx_Hit" },
            { "fx_hit_small", "Fx_HitSmall" },
            { "fx_explosion", "Fx_Explosion" },
            { "fx_burst_big", "Fx_BurstBig" },
            { "fx_slash", "Fx_Slash" },
            { "fx_slash_cross", "Fx_SlashCross" },
            { "fx_arc_wave", "Fx_ArcWave" },
            { "fx_ring", "Fx_Ring" },
            { "fx_pillar", "Fx_Pillar" },
            { "fx_bolt", "Fx_Bolt" },
            { "fx_vortex", "Fx_Vortex" },
            { "fx_drop", "Fx_Drop" },
            { "fx_ground", "Fx_Ground" },
            { "fx_buff", "Fx_Buff" },
            { "fx_heal", "Fx_Heal" },
            { "fx_trail", "Fx_Trail" },

            // 스킬 정체성 이펙트. 받아온 팩은 범용 폭발/베기뿐이라
            // 「물기」와 「으르렁」이 같은 그림으로 나갔다 — 이름이 보여야 할 스킬은 직접 그린다.
            { "fx_teeth", "Fx_Teeth" },
            { "fx_growl", "Fx_Growl" },
            { "fx_bark", "Fx_Bark" },
            { "fx_yawn", "Fx_Yawn" },
            { "fx_dogclaw", "Fx_DogClaw" },
            { "fx_burst", "Fx_Burst" },
            { "fx_bone", "Fx_Bone" },
            { "fx_tailsw", "Fx_TailSwing" },
            { "fx_scent", "Fx_Scent" },
            { "fx_sonar", "Fx_Sonar" },
            { "fx_howl", "Fx_Howl" },
            { "fx_kibble", "Fx_Kibble" },
            { "fx_frisbee", "Fx_Frisbee" },
            { "fx_ball", "Fx_Ball" },
            { "fx_slipper", "Fx_Slipper" },
            { "fx_bread", "Fx_Bread" },
            { "fx_paw", "Fx_Paw" },
            { "fx_dogdust", "Fx_DogDust" },

            // 팩 2차 반입. 절차로 그린 것보다 확실히 나은 것만 골랐다.
            { "fx_claw3", "Fx_Claw3" },
            { "fx_ring_spike", "Fx_RingSpike" },
            { "fx_crescent", "Fx_Crescent" },
            { "fx_swirl", "Fx_Swirl" },
            { "fx_ring_dense", "Fx_RingDense" },
            { "fx_sparkle", "Fx_Sparkle" },

            // Tiny Swords 파티클. 색이 이미 들어 있어 틴트를 끈다(KeepOwnColor).
            { "fx_ts_dust", "Fx_TsDust" },
            { "fx_ts_explosion", "Fx_TsExplosion" },
            { "fx_ts_fire", "Fx_TsFire" },
            { "fx_ts_splash", "Fx_TsSplash" },

            // 「보호」 하나를 4개 스킬이 돌려썼는데 실제로는 보호막·각성·질주·도발로 전부 달랐다.
            { "fx_shield", "Fx_Shield" },
            { "fx_awaken", "Fx_Awaken" },
            { "fx_zoom", "Fx_Zoom" },
            { "fx_taunt", "Fx_Taunt" },
            { "fx_eyebeam", "Fx_EyeBeam" },
            { "fx_gaze", "Fx_Gaze" },
            { "fx_static", "Fx_Static" }
        };

        [MenuItem("Tools/개많이잡아/2D 에셋 빌드", priority = 10)]
        public static void BuildAll()
        {
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(MaterialDir);

            int fx = BuildFxPrefabs();
            BuildPickupPrefabs();
            BuildGroundPrefab();
            BuildCharacterPrefabs();
            CleanLegacyMap();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Art2DBuilder] 완료 — FX 프리팹 {fx}개, 타일맵 바닥 생성");
        }

        /// <summary>
        /// 3D 시절 맵(벽으로 둘러싼 아레나)을 씬에서 걷어낸다.
        ///
        /// 2D 전환으로 바닥은 GroundTiler 가 무한으로 깔고, 뱀서류에 아레나 벽은 필요 없다.
        /// 게다가 정리 단계에서 그 머티리얼들을 지워서 지금은 **마젠타로 렌더된다** —
        /// 머티리얼이 없는 렌더러를 엔진이 그렇게 그리는데, 로그에는 아무 것도 안 남는다.
        /// </summary>
        private static void CleanLegacyMap()
        {
            const string scenePath = "Assets/Scenes/Main.unity";
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (scene.IsValid() == false)
            {
                Debug.LogWarning($"[Art2DBuilder] '{scenePath}' 를 열지 못했습니다");
                return;
            }

            // 이름으로 찾지 않는다 — 씬에서 인스턴스 이름이 바뀌어 있을 수 있다(실제로 'Map' 이었다).
            // 프리팹 원본 경로로 판정해야 이름과 무관하게 잡힌다.
            int removed = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject go = roots[i];
                if (go == null)
                    continue;

                string src = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                bool isLegacyMap = string.IsNullOrEmpty(src) == false
                    && src.EndsWith("OBJ_MAP.prefab", System.StringComparison.OrdinalIgnoreCase);

                // 프리팹 연결이 끊겼을 때를 대비해 「머티리얼 없는 렌더러만 든 오브젝트」도 함께 본다.
                if (isLegacyMap == false)
                    continue;

                Object.DestroyImmediate(go);
                removed++;
            }

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log($"[Art2DBuilder] 옛 맵 정리 — {removed}개 제거");
        }

        /// <summary>
        /// 바닥 프리팹 — 진짜 타일맵.
        ///
        /// 구조가 셋으로 갈린다. 각각 이유가 있다.
        ///   루트   : 움직이지 않는 거대한 지면 콜라이더. [사고] 이게 없으면 중력이 걸린 전원이 무한 낙하한다
        ///   Grid   : XZY 스위즐. 우리 월드는 XZ 평면이라 타일맵을 눕혀야 한다
        ///   Tilemap: 플레이어 주변만 채워지는 창. 12분 이동 거리를 다 깔 수는 없다
        /// </summary>
        private static void BuildGroundPrefab()
        {
            TileBase[] grass = BuildGrassTiles();
            if (grass.Length == 0)
            {
                Debug.LogWarning("[Art2DBuilder] 잔디 타일이 없습니다 — 바닥을 만들지 않았습니다");
                return;
            }

            GameObject root = new GameObject("OBJ_Ground");
            BoxCollider floor = root.AddComponent<BoxCollider>();
            floor.size = new Vector3(4000f, 1f, 4000f);
            floor.center = new Vector3(0f, -0.5f, 0f);   // 윗면이 y=0

            GameObject gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(root.transform, false);
            Grid grid = gridGo.AddComponent<Grid>();
            // 타일 64px / PPU 32 = 2유닛. 셀 크기가 다르면 타일 사이에 틈이나 겹침이 생긴다.
            grid.cellSize = new Vector3(TileWorldSize, TileWorldSize, 0f);

            // [사고] CellSwizzle.XZY 로 XZ 평면에 깔았더니 타일이 **세로로 선 채** 배치됐다.
            // 스위즐은 셀 위치만 바꾸고 스프라이트를 눕히지는 않는다. 50° 카메라에서
            // 줄지어 선 카드처럼 보이고 그 틈으로 배경이 비쳤다 — 원인이 「타일 색 변형」처럼 보였다.
            // 눕히려면 Grid 자체를 돌려야 한다. -90° 면 스프라이트 앞면이 위(+Y)를 본다.
            gridGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            GameObject mapGo = new GameObject("Tilemap_Ground");
            mapGo.transform.SetParent(gridGo.transform, false);
            Tilemap map = mapGo.AddComponent<Tilemap>();
            TilemapRenderer tr = mapGo.AddComponent<TilemapRenderer>();
            tr.sortingOrder = -100;                       // 무엇보다도 뒤
            tr.mode = TilemapRenderer.Mode.Chunk;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;

            TilemapStreamerSetup(root, map, grass);

            SavePrefab(root, PrefabDir + "/OBJ_Ground.prefab", "obj_ground");
        }

        private const float TileWorldSize = 2f;

        private static void TilemapStreamerSetup(GameObject root, Tilemap map, TileBase[] grass)
        {
            TilemapStreamer st = root.AddComponent<TilemapStreamer>();
            SetPrivate(st, "_ground", map);
            SetPrivate(st, "_cellSize", TileWorldSize);

            SerializedObject so = new SerializedObject(st);
            SerializedProperty tiles = so.FindProperty("_grassTiles");
            tiles.arraySize = grass.Length;
            for (int i = 0; i < grass.Length; i++)
                tiles.GetArrayElementAtIndex(i).objectReferenceValue = grass[i];

            // 장식은 타일이 아니라 세워 놓는 스프라이트다 — 원본 아트가 측면 뷰라 눕히면 뭉개진다.
            Sprite[] deco = LoadByPrefix(TileDir, "deco_");
            SerializedProperty decos = so.FindProperty("_decoSprites");
            decos.arraySize = deco.Length;
            for (int i = 0; i < deco.Length; i++)
                decos.GetArrayElementAtIndex(i).objectReferenceValue = deco[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>잔디 스프라이트를 Tile 에셋으로 굽는다. Tilemap 은 Sprite 가 아니라 TileBase 를 받는다.</summary>
        private static TileBase[] BuildGrassTiles()
        {
            const string tileAssetDir = "Assets/_Art/Tiles/Assets";
            Directory.CreateDirectory(tileAssetDir);

            Sprite[] sprites = LoadByPrefix(TileDir, "tile_grass_");
            List<TileBase> made = new List<TileBase>(sprites.Length);
            for (int i = 0; i < sprites.Length; i++)
            {
                string path = $"{tileAssetDir}/{sprites[i].name}.asset";
                Tile t = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (t == null)
                {
                    t = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(t, path);
                }
                t.sprite = sprites[i];
                t.colliderType = Tile.ColliderType.None;   // 바닥은 루트 콜라이더가 담당한다
                EditorUtility.SetDirty(t);
                made.Add(t);
            }
            return made.ToArray();
        }

        private static Sprite[] LoadByPrefix(string dir, string prefix)
        {
            if (Directory.Exists(dir) == false)
                return new Sprite[0];
            return Directory.GetFiles(dir, prefix + "*.png")
                .OrderBy(p => p)
                .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p.Replace('\\', '/')))
                .Where(s => s != null)
                .ToArray();
        }

        // ── FX ──────────────────────────────────────────────────
        private static int BuildFxPrefabs()
        {
            int made = 0;
            foreach (KeyValuePair<string, string> kv in FxAddress)
            {
                string dir = FxDir + "/" + kv.Key;
                Sprite[] frames = LoadSprites(dir);
                if (frames.Length == 0)
                {
                    Debug.LogWarning($"[Art2DBuilder] '{dir}' 에 스프라이트가 없습니다");
                    continue;
                }

                GameObject go = new GameObject(kv.Value);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = frames[0];
                sr.sortingOrder = OrderFx;

                SpriteAnimator anim = go.AddComponent<SpriteAnimator>();
                SetPrivate(anim, "_renderer", sr);
                SetPrivate(anim, "_frames", frames);
                // [사고] 20fps · 1회로 두었더니 5프레임이 0.25초 만에 끝나고
                // 마지막 프레임에서 얼어붙었다. 실제 속도는 SkillVisual 이
                // 스킬 수명을 보고 다시 정한다 — 여기는 안전한 기본값만 둔다.
                SetPrivate(anim, "_fps", 10f);
                SetPrivate(anim, "_loop", true);

                // 색이 들어 있는 팩 그림은 물들이지 않는다. 이미 명암이 있는 그림에
                // 색을 또 곱하면 탁해진다.
                if (kv.Key.StartsWith("fx_ts_"))
                    go.AddComponent<Jinhyeong_View.KeepOwnColor>();

                // 타격 연출만 풀 반환/색 틴트 로직을 갖는다. 나머지는 스킬 비주얼이 직접 쓴다.
                if (kv.Value == "Fx_Hit")
                {
                    HitEffect hit = go.AddComponent<HitEffect>();
                    SetPrivate(hit, "_renderer", sr);
                    SetPrivate(hit, "_animator", anim);
                }

                SavePrefab(go, PrefabDir + "/" + kv.Value + ".prefab", kv.Value);
                made++;
            }
            return made;
        }

        // ── 픽업 ────────────────────────────────────────────────
        private static void BuildPickupPrefabs()
        {
            // 파츠 상자
            Sprite box = LoadSprite(SpriteDir + "/pickup_box.png");
            if (box != null)
            {
                GameObject go = new GameObject(PartDropper.PickupKey);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = box;
                sr.sortingOrder = OrderPickup;
                go.AddComponent<Jinhyeong_UI.Billboard>();

                PartPickup pickup = go.AddComponent<PartPickup>();
                SetPrivate(pickup, "_renderer", sr);
                SavePrefab(go, PrefabDir + "/" + PartDropper.PickupKey + ".prefab", PartDropper.PickupKey);
            }

            // 경험치 사료
            Sprite kibble = LoadSprite(SpriteDir + "/pickup_kibble.png");
            if (kibble != null)
            {
                GameObject go = new GameObject("Fx_XpGem");
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = kibble;
                sr.sortingOrder = OrderPickup;
                go.AddComponent<Jinhyeong_UI.Billboard>();
                go.AddComponent<XpGem>();
                SavePrefab(go, PrefabDir + "/Fx_XpGem.prefab", "Fx_XpGem");
            }
        }

        // ── 캐릭터 ──────────────────────────────────────────────
        private static void BuildCharacterPrefabs()
        {
            RebuildVisual("Assets/_Art/Prefabs/OBJ_Player.prefab", "dog_shiba", null);

            string[] enemyKeys = { "grunt", "runner", "brute", "spitter", "swarm", "ufo" };
            RebuildVisual("Assets/_Art/Prefabs/OBJ_Enemy.prefab", "alien_grunt", enemyKeys);
        }

        /// <summary>
        /// 기존 프리팹의 로직 컴포넌트는 그대로 두고 비주얼 자식만 갈아 끼운다.
        /// 통째로 다시 만들면 SerializeField 배선이 전부 끊긴다.
        /// </summary>
        private static void RebuildVisual(string prefabPath, string spritePrefix, string[] enemyKeys)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (asset == null)
            {
                Debug.LogWarning($"[Art2DBuilder] '{prefabPath}' 를 찾을 수 없습니다");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                // 깨진 참조를 먼저 걷어낸다. 이게 남아 있으면 Unity 가 프리팹 저장을 거부한다.
                //   - 삭제한 3D 비주얼(Free Stickman)이 중첩 프리팹으로 물려 있고
                //   - 삭제한 CharacterAnimator 가 missing script 로 남아 있다
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                {
                    Transform c = root.transform.GetChild(i);
                    GameObject cg = c.gameObject;

                    // 카메라 루트와 UI(HP바)는 살린다 — 비주얼이 아니라 로직에 물려 있다
                    if (c.name == "CameraRoot" || c.GetComponentInChildren<Canvas>(true) != null)
                        continue;

                    bool brokenNest = PrefabUtility.IsPrefabAssetMissing(cg);
                    bool oldVisual = c.name == "Visual"
                        || c.GetComponentInChildren<SkinnedMeshRenderer>(true) != null
                        || c.GetComponentInChildren<MeshRenderer>(true) != null;

                    if (brokenNest || oldVisual)
                        Object.DestroyImmediate(cg);
                }

                // 루트와 남은 자식 전체에서 missing script 제거
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
                Transform[] rest = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < rest.Length; i++)
                {
                    if (rest[i] != null)
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(rest[i].gameObject);
                }

                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
                sr.sortingOrder = OrderCharacter;
                visual.AddComponent<SpriteAnimator>();
                visual.AddComponent<Jinhyeong_UI.Billboard>();

                Sprite[] idle = { LoadSprite($"{SpriteDir}/{spritePrefix}_idle.png") };
                Sprite[] walk = LoadNumbered(SpriteDir, spritePrefix + "_walk", 4);
                if (idle[0] == null)
                    idle = walk;
                sr.sprite = idle.Length > 0 ? idle[0] : null;

                SpriteAnimator anim = visual.GetComponent<SpriteAnimator>();
                SetPrivate(anim, "_renderer", sr);
                SetPrivate(anim, "_frames", walk);

                CharacterView view = root.GetComponent<CharacterView>();
                if (view == null)
                    view = root.AddComponent<CharacterView>();
                SetPrivate(view, "_renderer", sr);
                SetPrivate(view, "_animator", anim);
                SetPrivate(view, "_idle", idle);
                SetPrivate(view, "_walk", walk);

                // 방향 그림. 적은 이 파일들이 없어 빈 배열이 들어가고, CharacterView 가
                // 정면 그림으로 떨어진다 — 있으면 쓰고 없으면 그만인 구조라 분기가 없다.
                Sprite[] walkUp = LoadNumbered(SpriteDir, spritePrefix + "_walkup", 4);
                Sprite[] walkSide = LoadNumbered(SpriteDir, spritePrefix + "_walkside", 4);
                Sprite upIdle = LoadSprite($"{SpriteDir}/{spritePrefix}_idleup.png");
                Sprite sideIdle = LoadSprite($"{SpriteDir}/{spritePrefix}_idleside.png");
                SetPrivate(view, "_walkUp", walkUp);
                SetPrivate(view, "_walkSide", walkSide);
                SetPrivate(view, "_idleUp", upIdle != null ? new[] { upIdle } : walkUp);
                SetPrivate(view, "_idleSide", sideIdle != null ? new[] { sideIdle } : walkSide);
                SetPrivate(view, "_motor", root.GetComponent<Jinhyeong_Character.CharacterMotor>());
                SetPrivate(view, "_facing", root.GetComponent<Jinhyeong_Character.CharacterFacing>());

                if (enemyKeys != null)
                {
                    SpriteSetLibrary lib = root.GetComponent<SpriteSetLibrary>();
                    if (lib == null)
                        lib = root.AddComponent<SpriteSetLibrary>();
                    SetPrivate(lib, "_view", view);

                    SpriteSetLibrary.Entry[] entries = enemyKeys
                        .Select(k => new SpriteSetLibrary.Entry
                        {
                            Key = k,
                            Frames = LoadNumbered(SpriteDir, "alien_" + k + "_walk", 4),
                            FramesUp = LoadNumbered(SpriteDir, "alien_" + k + "_walkup", 4),
                            FramesSide = LoadNumbered(SpriteDir, "alien_" + k + "_walkside", 4)
                        })
                        .Where(e => e.Frames.Length > 0)
                        .ToArray();
                    SetPrivate(lib, "_entries", entries);
                }
                else
                {
                    // 플레이어에만 파츠 착용 컴포넌트가 붙는다
                    if (root.GetComponent<PartLoadout>() == null)
                        root.AddComponent<PartLoadout>();
                    SetPrivate(root.GetComponent<PartLoadout>(), "_skills", root.GetComponent<SkillObject>());

                    // 자동 조준. 없으면 스킬이 「걷는 방향」으로만 나가 도망칠 때 등 뒤로 쏜다.
                    if (root.GetComponent<Jinhyeong_Character.AutoAim>() == null)
                        root.AddComponent<Jinhyeong_Character.AutoAim>();
                }

                // 월드 HP 바.
                //
                // [사고] HPBar 는 **중첩 프리팹**이라 코드/씬 검색에 안 걸렸고, 로컬 위치가 (0,0,0)
                // 이라 캐릭터 **가슴 한가운데**에 박혀 있었다. 빌보드도 없어서 루트가 조준으로 돌면
                // 바가 같이 돌아 눕거나 뒤집혔다.
                // 바는 「누가 얼마나 남았나」를 읽는 장치다. 캐릭터에 가려지거나 회전하면 못 읽는다.
                Jinhyeong_UI.WorldHpBar[] bars =
                    root.GetComponentsInChildren<Jinhyeong_UI.WorldHpBar>(true);
                for (int c = 0; c < bars.Length; c++)
                {
                    // [사고] 처음엔 localPosition 을 세팅했는데 값이 저장되지 않았다.
                    // HP 바 루트가 **RectTransform** 이라 위치의 실제 주인은 anchoredPosition 이고,
                    // localPosition 은 레이아웃이 다시 덮어쓴다. UI 는 UI 의 방식으로 옮겨야 한다.
                    RectTransform rt = bars[c].transform as RectTransform;
                    if (rt != null)
                        rt.anchoredPosition3D = new Vector3(0f, HpBarHeight, 0f);
                    else
                        bars[c].transform.localPosition = new Vector3(0f, HpBarHeight, 0f);

                    if (bars[c].GetComponent<Jinhyeong_UI.Billboard>() == null)
                        bars[c].gameObject.AddComponent<Jinhyeong_UI.Billboard>();
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ── 유틸 ────────────────────────────────────────────────
        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite[] LoadSprites(string dir)
        {
            if (Directory.Exists(dir) == false)
                return new Sprite[0];
            return Directory.GetFiles(dir, "*.png")
                .OrderBy(p => p)
                .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p.Replace('\\', '/')))
                .Where(s => s != null)
                .ToArray();
        }

        private static Sprite[] LoadNumbered(string dir, string prefix, int count)
        {
            List<Sprite> list = new List<Sprite>(count);
            for (int i = 0; i < count; i++)
            {
                Sprite s = LoadSprite($"{dir}/{prefix}{i}.png");
                if (s != null)
                    list.Add(s);
            }
            return list.ToArray();
        }

        /// <summary>SerializeField 는 public 이 아니라 SerializedObject 로 넣는다.</summary>
        private static void SetPrivate(Object target, string field, object value)
        {
            if (target == null)
                return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogWarning($"[Art2DBuilder] '{target.GetType().Name}.{field}' 를 찾지 못했습니다");
                return;
            }

            if (value is Object[] arr)
            {
                p.arraySize = arr.Length;
                for (int i = 0; i < arr.Length; i++)
                    p.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
            }
            else if (value is SpriteSetLibrary.Entry[] entries)
            {
                p.arraySize = entries.Length;
                for (int i = 0; i < entries.Length; i++)
                {
                    SerializedProperty e = p.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("Key").stringValue = entries[i].Key;
                    SerializedProperty fr = e.FindPropertyRelative("Frames");
                    fr.arraySize = entries[i].Frames.Length;
                    for (int k = 0; k < entries[i].Frames.Length; k++)
                        fr.GetArrayElementAtIndex(k).objectReferenceValue = entries[i].Frames[k];
                }
            }
            else if (value is Object o)
            {
                p.objectReferenceValue = o;
            }
            else if (value is float f)
            {
                p.floatValue = f;
            }
            else if (value is bool b)
            {
                p.boolValue = b;
            }
            else if (value is int n)
            {
                p.intValue = n;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SavePrefab(GameObject go, string path, string address)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            RegisterAddressable(path, address);
        }

        /// <summary>풀 매니저가 주소로 꺼내 쓰므로 Addressable 등록이 필수다.</summary>
        private static void RegisterAddressable(string path, string address)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("[Art2DBuilder] Addressables 설정이 없습니다 — 수동 등록 필요");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
                return;

            AddressableAssetGroup group = settings.FindGroup("Skill") ?? settings.DefaultGroup;
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry != null)
                entry.address = address;
        }
    }
}
