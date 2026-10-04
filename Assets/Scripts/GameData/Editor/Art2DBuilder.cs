using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using Jinhyeong_Managers;
using Jinhyeong_Parts;
using Jinhyeong_SkillSystem;
using Jinhyeong_View;

namespace Jinhyeong_GameData.Editor
{
    public static class Art2DBuilder
    {
        private const string SpriteDir = "Assets/_Art/Sprites";
        private const string FxDir = "Assets/_Art/FX";

        private const float HpBarHeight = 2.65f;
        // 아트 리부트 무대 바닥 · 소품 (build_stage.py). 톤은 생성기가 이미 굽는다 — 바닥 머티리얼은 원색 그대로
        private const string StageDir = "Assets/_Art/Stage";
        private const string PrefabDir = "Assets/_Art/Prefabs/Gen";
        private const string MaterialDir = "Assets/_Art/Materials";

        // 배경 톤. 목표는 캡처의 배경 채도 30% 이하 · 명도 40~55% (spec 아트 규격 절, ref_art.py 로 잰다)
        private const string ToneShader = "Jinhyeong/ToneSprite";
        private const string GroundMaterialPath = MaterialDir + "/Ground_Tone.mat";
        private const string DecoMaterialPath = MaterialDir + "/Deco_Tone.mat";
        private const float GroundSaturation = 1f;
        private const float GroundBrightness = 1f;
        private const float DecoSaturation = 0.7f;
        private const float DecoBrightness = 0.8f;

        // 무대 바닥 4종 (build_stage.py). 셀마다 해시로 고른다
        private static readonly string[] GrassTileNames = { "tile_stage_1", "tile_stage_2", "tile_stage_3", "tile_stage_4" };

        private const string VolumeProfilePath = "Assets/Settings/Game_VolumeProfile.asset";
        private const float VignetteIntensity = 0.25f;
        private const float VignetteSmoothness = 0.45f;

        private const int OrderCharacter = 10;
        private const int OrderFx = 20;
        private const int OrderPlayer = 25;
        private const int OrderPickup = 5;

        // build_vfx.py 가 내는 26종. 각 폴더에 core/ · glow/ 두 층
        private static readonly Dictionary<string, string> FxAddress = new Dictionary<string, string>
        {
            { "fx_hit", "Fx_Hit" },
            { "fx_shards", "Fx_HitBurst" },

            // 음악 전투 — 주인공 기본 공격 · 다장조 화음
            { "fx_notebass", "Fx_NoteBass" },
            { "fx_notebeat", "Fx_NoteBeat" },
            { "fx_staffbeam", "Fx_StaffBeam" },
            { "fx_notescale", "Fx_NoteScale" },
            { "fx_notewhole", "Fx_NoteWhole" },
            { "fx_chord", "Fx_Chord" },

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

            { "fx_shield", "Fx_Shield" },
            { "fx_awaken", "Fx_Awaken" },
            { "fx_zoom", "Fx_Zoom" },
            { "fx_taunt", "Fx_Taunt" },
            { "fx_eyebeam", "Fx_EyeBeam" },
            { "fx_gaze", "Fx_Gaze" },
            { "fx_static", "Fx_Static" }
        };

        // 제 색을 지키는 물건형. 나머지(기운형)는 흰 심지가 계열 색을 CoreTintEnergy 만큼만 받는다
        private static readonly HashSet<string> FxObjects = new HashSet<string>
        {
            "fx_teeth", "fx_bone", "fx_kibble", "fx_frisbee", "fx_ball", "fx_slipper", "fx_bread"
        };
        private const float CoreTintEnergy = 0.25f;
        private const string GlowShader = "Jinhyeong/SpriteAdditive";
        private const string GlowMaterialPath = MaterialDir + "/Fx_Glow.mat";

        [MenuItem("Tools/개많이잡아/2D 에셋 빌드", priority = 10)]
        public static void BuildAll()
        {
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(MaterialDir);

            int purged = PurgeStaleFxPrefabs();
            if (purged > 0)
                Debug.Log($"[Art2DBuilder] 표에 없는 이펙트 프리팹 {purged}개 삭제");
            int fx = BuildFxPrefabs();
            BuildGhostPrefab();
            BuildPickupPrefabs();
            BuildGroundPrefab();
            BuildCharacterPrefabs();
            CleanLegacyMap();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Art2DBuilder] 완료 — FX 프리팹 {fx}개, 타일맵 바닥 생성");
        }

        private static void CleanLegacyMap()
        {
            const string scenePath = "Assets/Scenes/Main.unity";
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (scene.IsValid() == false)
            {
                Debug.LogWarning($"[Art2DBuilder] '{scenePath}' 를 열지 못했습니다");
                return;
            }

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
            floor.center = new Vector3(0f, -0.5f, 0f);

            GameObject gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(root.transform, false);
            Grid grid = gridGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(TileWorldSize, TileWorldSize, 0f);

            gridGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            GameObject mapGo = new GameObject("Tilemap_Ground");
            mapGo.transform.SetParent(gridGo.transform, false);
            Tilemap map = mapGo.AddComponent<Tilemap>();
            TilemapRenderer tr = mapGo.AddComponent<TilemapRenderer>();
            tr.sortingOrder = -100;
            tr.mode = TilemapRenderer.Mode.Chunk;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
            Material groundTone = LoadToneMaterial(GroundMaterialPath, GroundSaturation, GroundBrightness);
            if (groundTone != null)
                tr.sharedMaterial = groundTone;

            // 화면 마감(비네트). 바닥은 판마다 하나라 월드 전역 볼륨을 여기에 둔다
            GameObject volumeGo = new GameObject("Volume");
            volumeGo.transform.SetParent(root.transform, false);
            Volume volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = LoadGameVolumeProfile();

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

            Sprite[] deco = LoadByPrefix(StageDir, "deco_");
            SerializedProperty decos = so.FindProperty("_decoSprites");
            decos.arraySize = deco.Length;
            for (int i = 0; i < deco.Length; i++)
                decos.GetArrayElementAtIndex(i).objectReferenceValue = deco[i];

            so.ApplyModifiedPropertiesWithoutUndo();
            SetPrivate(st, "_decoMaterial", LoadToneMaterial(DecoMaterialPath, DecoSaturation, DecoBrightness));
        }

        private static TileBase[] BuildGrassTiles()
        {
            const string tileAssetDir = "Assets/_Art/Tiles/Assets";
            Directory.CreateDirectory(tileAssetDir);

            Sprite[] sprites = GrassTileNames
                .Select(n => LoadSprite($"{StageDir}/{n}.png"))
                .Where(sp => sp != null)
                .ToArray();
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
                t.colliderType = Tile.ColliderType.None;
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

        private static int BuildFxPrefabs()
        {
            int made = 0;
            Material glowMat = LoadGlowMaterial();
            foreach (KeyValuePair<string, string> kv in FxAddress)
            {
                string dir = FxDir + "/" + kv.Key;
                Sprite[] core = LoadSprites(dir + "/core");
                Sprite[] glow = LoadSprites(dir + "/glow");
                if (core.Length == 0)
                {
                    Debug.LogWarning($"[Art2DBuilder] '{dir}/core' 에 스프라이트가 없습니다 — build_vfx.py 를 돌렸는지 확인");
                    continue;
                }

                GameObject go = new GameObject(kv.Value);
                // 번짐이 먼저(아래), 심지가 위. 같은 정렬 순서 안에서 형제 순서를 따르지 않으므로 order 로 가른다
                if (glow.Length > 0)
                    AddFxLayer(go, "Glow", glow, OrderFx, glowMat, -1f);
                bool keepOwn = FxObjects.Contains(kv.Key);
                AddFxLayer(go, "Core", core, OrderFx + 1, null, keepOwn ? 0f : CoreTintEnergy);

                if (kv.Value == "Fx_Hit" || kv.Value == "Fx_HitBurst")
                    go.AddComponent<HitEffect>();

                SavePrefab(go, PrefabDir + "/" + kv.Value + ".prefab", kv.Value);
                made++;
            }
            return made;
        }

        // 이동형 스킬 잔상 한 장. 스프라이트는 런타임에 심지 그림을 받는다 — 프리팹엔 렌더러 · 가산 머티리얼만
        private static void BuildGhostPrefab()
        {
            GameObject go = new GameObject("Fx_Ghost");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = OrderFx - 1;
            Material glow = LoadGlowMaterial();
            if (glow != null)
                sr.sharedMaterial = glow;
            GhostFade ghost = go.AddComponent<GhostFade>();
            SetPrivate(ghost, "_renderer", sr);
            SavePrefab(go, PrefabDir + "/Fx_Ghost.prefab", "Fx_Ghost");
        }

        // 표(FxAddress)에서 빠진 Fx_* 프리팹을 지운다. 남겨 두면 지워진 스프라이트를 가리킨 채 어드레서블에 남는다
        private static int PurgeStaleFxPrefabs()
        {
            HashSet<string> keep = new HashSet<string>(FxAddress.Values) { "Fx_XpGem", "Fx_Ghost", PartDropper.PickupKey };
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            int removed = 0;
            foreach (string path in Directory.GetFiles(PrefabDir, "Fx_*.prefab"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (keep.Contains(name))
                    continue;
                string assetPath = path.Replace('\\', '/');
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (settings != null && string.IsNullOrEmpty(guid) == false)
                    settings.RemoveAssetEntry(guid);
                if (AssetDatabase.DeleteAsset(assetPath))
                    removed++;
            }
            return removed;
        }

        private static void AddFxLayer(GameObject root, string name, Sprite[] frames, int order, Material mat, float tintWeight)
        {
            GameObject layer = new GameObject(name);
            layer.transform.SetParent(root.transform, false);
            SpriteRenderer sr = layer.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingOrder = order;
            if (mat != null)
                sr.sharedMaterial = mat;

            SpriteAnimator anim = layer.AddComponent<SpriteAnimator>();
            SetPrivate(anim, "_renderer", sr);
            SetPrivate(anim, "_frames", frames);
            SetPrivate(anim, "_fps", 10f);
            SetPrivate(anim, "_loop", true);

            if (tintWeight >= 0f)
            {
                Jinhyeong_View.KeepOwnColor keep = layer.AddComponent<Jinhyeong_View.KeepOwnColor>();
                SetPrivate(keep, "_tintWeight", tintWeight);
            }
        }

        private static Material LoadGlowMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath);
            if (mat != null)
                return mat;

            Shader shader = Shader.Find(GlowShader);
            if (shader == null)
            {
                Debug.LogWarning($"[Art2DBuilder] '{GlowShader}' 셰이더가 없습니다 — 번짐 층을 기본 스프라이트로 둡니다");
                return null;
            }
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, GlowMaterialPath);
            return mat;
        }

        // 먹는 아이템의 «모양 그대로 빛나는 테두리» — 실루엣을 키워 번지게 한 흰 그림을 가산으로 몸 뒤에 깐다
        private static SpriteRenderer AddPickupGlow(GameObject root, string spritePath, Color color)
        {
            Sprite glow = LoadSprite(spritePath);
            if (glow == null)
                return null;
            GameObject g = new GameObject("Glow");
            g.transform.SetParent(root.transform, false);
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = glow;
            sr.sortingOrder = OrderPickup - 1;
            sr.color = color;
            Material mat = LoadGlowMaterial();
            if (mat != null)
                sr.sharedMaterial = mat;
            return sr;
        }

        private static void BuildPickupPrefabs()
        {
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
                SetPrivate(pickup, "_glow", AddPickupGlow(go, SpriteDir + "/pickup_box_glow.png", Color.white));
                SavePrefab(go, PrefabDir + "/" + PartDropper.PickupKey + ".prefab", PartDropper.PickupKey);
            }

            Sprite kibble = LoadSprite(SpriteDir + "/pickup_kibble.png");
            if (kibble != null)
            {
                GameObject go = new GameObject("Fx_XpGem");
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = kibble;
                sr.sortingOrder = OrderPickup;
                go.AddComponent<Jinhyeong_UI.Billboard>();
                go.AddComponent<XpGem>();
                // 보상 색(레몬) 테두리 — 역할 색 예약
                AddPickupGlow(go, SpriteDir + "/pickup_kibble_glow.png", new Color(1f, 0.93f, 0.32f, 0.9f));
                SavePrefab(go, PrefabDir + "/Fx_XpGem.prefab", "Fx_XpGem");
            }
        }

        private static void BuildCharacterPrefabs()
        {
            RebuildVisual("Assets/_Art/Prefabs/OBJ_Player.prefab", "dog_shiba", null);

            string[] enemyKeys = { "grunt", "runner", "brute", "spitter", "swarm", "ufo" };
            RebuildVisual("Assets/_Art/Prefabs/OBJ_Enemy.prefab", "alien_grunt", enemyKeys);
        }

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
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                {
                    Transform c = root.transform.GetChild(i);
                    GameObject cg = c.gameObject;

                    if (c.name == "CameraRoot" || c.GetComponentInChildren<Canvas>(true) != null)
                        continue;

                    bool brokenNest = PrefabUtility.IsPrefabAssetMissing(cg);
                    bool oldVisual = c.name == "Visual"
                        || c.GetComponentInChildren<SkinnedMeshRenderer>(true) != null
                        || c.GetComponentInChildren<MeshRenderer>(true) != null;

                    if (brokenNest || oldVisual)
                        Object.DestroyImmediate(cg);
                }

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
                // 플레이어는 이펙트(20 · 21)보다 위 — 최대 밀도에서 이펙트에 묻혔다 (QA3 캡처). 적은 그대로 이펙트 아래
                sr.sortingOrder = enemyKeys == null ? OrderPlayer : OrderCharacter;
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
                    BindHeroSkins(root, view);

                    if (root.GetComponent<PartLoadout>() == null)
                        root.AddComponent<PartLoadout>();
                    SetPrivate(root.GetComponent<PartLoadout>(), "_skills", root.GetComponent<SkillObject>());

                    if (root.GetComponent<Jinhyeong_Character.AutoAim>() == null)
                        root.AddComponent<Jinhyeong_Character.AutoAim>();
                }

                Jinhyeong_UI.WorldHpBar[] bars =
                    root.GetComponentsInChildren<Jinhyeong_UI.WorldHpBar>(true);
                for (int c = 0; c < bars.Length; c++)
                {
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

        // RunSelection.Heroes 순서. build_heroes.py 가 dog_<키>_* 를 낸다
        private static readonly string[] HeroKeys = { "shiba", "corgi", "greyhound", "pomeranian", "samoyed" };

        private static void BindHeroSkins(GameObject root, CharacterView view)
        {
            HeroSkin skin = root.GetComponent<HeroSkin>();
            if (skin == null)
                skin = root.AddComponent<HeroSkin>();
            SetPrivate(skin, "_view", view);

            HeroSkin.Set[] sets = HeroKeys.Select(k => new HeroSkin.Set
            {
                Key = k,
                Idle = new[] { LoadSprite($"{SpriteDir}/dog_{k}_idle.png") },
                Walk = LoadNumbered(SpriteDir, $"dog_{k}_walk", 4),
                IdleUp = new[] { LoadSprite($"{SpriteDir}/dog_{k}_idleup.png") },
                WalkUp = LoadNumbered(SpriteDir, $"dog_{k}_walkup", 4),
                IdleSide = new[] { LoadSprite($"{SpriteDir}/dog_{k}_idleside.png") },
                WalkSide = LoadNumbered(SpriteDir, $"dog_{k}_walkside", 4)
            }).ToArray();
            SetPrivate(skin, "_sets", sets);
        }

        private static Material LoadToneMaterial(string path, float saturation, float brightness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find(ToneShader);
                if (shader == null)
                {
                    Debug.LogWarning($"[Art2DBuilder] '{ToneShader}' 셰이더가 없습니다 — 기본 스프라이트 머티리얼로 둡니다");
                    return null;
                }
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetFloat("_Saturation", saturation);
            mat.SetFloat("_Brightness", brightness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static VolumeProfile LoadGameVolumeProfile()
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            if (profile.TryGet(out Vignette vignette) == false)
            {
                vignette = profile.Add<Vignette>(false);
                vignette.name = "Vignette";
                AssetDatabase.AddObjectToAsset(vignette, profile);
            }
            vignette.intensity.Override(VignetteIntensity);
            vignette.smoothness.Override(VignetteSmoothness);
            EditorUtility.SetDirty(vignette);
            EditorUtility.SetDirty(profile);
            return profile;
        }

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

        private static void WriteSprites(SerializedProperty p, Sprite[] sprites)
        {
            p.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

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
            else if (value is HeroSkin.Set[] sets)
            {
                p.arraySize = sets.Length;
                for (int i = 0; i < sets.Length; i++)
                {
                    SerializedProperty e = p.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("Key").stringValue = sets[i].Key;
                    WriteSprites(e.FindPropertyRelative("Idle"), sets[i].Idle);
                    WriteSprites(e.FindPropertyRelative("Walk"), sets[i].Walk);
                    WriteSprites(e.FindPropertyRelative("IdleUp"), sets[i].IdleUp);
                    WriteSprites(e.FindPropertyRelative("WalkUp"), sets[i].WalkUp);
                    WriteSprites(e.FindPropertyRelative("IdleSide"), sets[i].IdleSide);
                    WriteSprites(e.FindPropertyRelative("WalkSide"), sets[i].WalkSide);
                }
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
