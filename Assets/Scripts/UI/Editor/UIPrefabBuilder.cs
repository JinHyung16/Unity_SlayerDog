using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Jinhyeong_UI.Framework;
using Jinhyeong_UI.Lobby;

namespace Jinhyeong_UI.Editor
{

    public static class UIPrefabBuilder
    {
        private const string PrefabRoot = "Assets/_Art/Prefabs/";
        private const string GroupName = "UI";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string UIRootName = "UIRoot";

        private const string LobbyPrefabPath = "Assets/_Art/Prefabs/UI/LobbyWindow.prefab";
        private const string StartPanelPrefabPath = "Assets/_Art/Prefabs/UI/Lobby/StartPanel.prefab";

        // 메인 화면(로비)을 씬에 미리 배치한다 — 플레이 전에도 씬 · 게임 뷰에서 보이게 (10-04 사용자 지시).
        // 프리팹 인스턴스로 두므로 ui 태스크로 다시 구워도 씬이 따라온다. 런타임에는 WindowFactory 가 이것을 그대로 쓴다
        public static string PlaceLobbyInScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                return $"[UIPrefabBuilder] 활성 씬이 {ScenePath} 가 아니다 ({scene.path}) — 배치하지 않았다";
            }
            if (scene.isDirty)
            {
                return "[UIPrefabBuilder] 씬에 저장 안 한 변경이 있다 — 사람 작업을 덮지 않으려고 배치하지 않았다";
            }

            string log = EnsureLobbyInScene(scene);
            return log ?? "[UIPrefabBuilder] 로비가 이미 씬에 있다 (프리팹 인스턴스 1개)";
        }

        [MenuItem("Jinhyeong/UI/로비 프리팹 굽기", false, 20)]
        public static void Build()
        {
            int made = 0;
            made += BuildLabelButton();
            made += BuildHeroCard();
            made += BuildConstellationNode();
            made += BuildStartPanel();
            made += BuildCharacterPanel();
            made += BuildSkillPanel();
            made += BuildSettingsPanel();
            made += BuildLobbyWindow();

            MigrateScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UIPrefabBuilder] 완료 — 프리팹 {made}개, 어드레서블 그룹 '{GroupName}'");
            Debug.Log(SkinReport());
        }

        private static string SkinReport()
        {
            string[] names =
            {
                UISkin.CardBg, UISkin.CardFrame, UISkin.PanelBg, UISkin.PanelFrame,
                UISkin.CircleBg, UISkin.CircleFrame, UISkin.BarBg
            };

            System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
            sb.Append("  스킨 경계: ");
            for (int i = 0; i < names.Length; i++)
            {
                Sprite sprite = UISkin.Get(names[i]);
                sb.Append(sprite != null
                    ? $"{names[i]}={sprite.border.x:0}  "
                    : $"{names[i]}=없음  ");
            }
            return sb.ToString();
        }

        private static int BuildLobbyWindow()
        {
            GameObject root;
            Canvas canvas;
            CanvasScaler scaler;
            RectTransform safe = MakeWindowRoot("LobbyWindow", SafeAreaUtil.ApplySideType.All, out root, out canvas, out scaler);

            RectTransform rail = UIBuild.MakeRect("NavRail", safe);
            rail.anchorMin = new Vector2(0f, 0f);
            rail.anchorMax = new Vector2(0f, 1f);
            rail.pivot = new Vector2(0f, 0.5f);
            rail.sizeDelta = new Vector2(LobbyWindow.NavWidth, 0f);
            rail.anchoredPosition = Vector2.zero;
            Image railBg = rail.gameObject.AddComponent<Image>();
            railBg.color = UIBuild.ColorRail;
            UISkin.ApplySliced(railBg, UISkin.PanelBg, UIBuild.PanelCorner);
            UIBuild.Outline(rail, UIBuild.ColorOutline, UISkin.PanelFrame, UIBuild.PanelCorner);
            VerticalLayoutGroup railLayout = UIBuild.Vertical(rail, 6f, false);
            railLayout.padding = new RectOffset(14, 14, 24, 24);

            Button[] tabButtons = new Button[LobbyWindow.TabNames.Length];
            Image[] tabImages = new Image[LobbyWindow.TabNames.Length];
            for (int i = 0; i < LobbyWindow.TabNames.Length; i++)
            {
                TMP_Text label;
                Button b = UIBuild.MakeButton(rail, "Tab_" + i, LobbyWindow.TabNames[i], 28f, UIBuild.ColorTabOff, out label);
                AttachTabLabel(label, (LobbyWindow.ETab)i);
                UIBuild.PreferredHeight((RectTransform)b.transform, 62f);
                tabButtons[i] = b;
                tabImages[i] = b.image;
            }

            RectTransform container = UIBuild.MakeRect("Container", safe);
            container.anchorMin = Vector2.zero;
            container.anchorMax = Vector2.one;
            container.offsetMin = new Vector2(LobbyWindow.NavWidth, 0f);
            container.offsetMax = Vector2.zero;

            LobbyWindow window = root.AddComponent<LobbyWindow>();
            Bind(window, "_canvas", canvas);
            Bind(window, "_scaler", scaler);
            BindInt(window, "_windowType", (int)EWindowType.Normal);
            BindBool(window, "_isFullSizeWindow", true);
            BindInt(window, "_closeType", (int)ECloseType.Close);
            Bind(window, "_container", container);
            BindArray(window, "_tabButtons", tabButtons);
            BindArray(window, "_tabImages", tabImages);

            return Save(root, LobbyWindow.PrefabPath);
        }

        // 탭 글자 — 추출기가 읽도록 키와 한국어를 글자 그대로 둔다 (LobbyWindow.TabNames 와 같은 순서)
        private static void AttachTabLabel(TMP_Text label, LobbyWindow.ETab tab)
        {
            switch (tab)
            {
                case LobbyWindow.ETab.Start:
                    LocLabel.Attach(label, "Lobby_TabStart", "시작");
                    break;
                case LobbyWindow.ETab.Character:
                    LocLabel.Attach(label, "Lobby_TabCharacter", "캐릭터");
                    break;
                case LobbyWindow.ETab.Skill:
                    LocLabel.Attach(label, "Lobby_TabSkill", "스킬 트리");
                    break;
                case LobbyWindow.ETab.Settings:
                    LocLabel.Attach(label, "Lobby_TabSettings", "설정");
                    break;
            }
        }

        public static RectTransform MakeWindowRoot(string name, SafeAreaUtil.ApplySideType applySide,
            out GameObject root, out Canvas canvas, out CanvasScaler scaler)
        {
            root = MakeCanvasRoot(name, out canvas, out scaler);

            Image bg = UIBuild.MakeImage(root.transform, "Background", UIBuild.ColorBg);
            UIBuild.Stretch(bg.rectTransform);

            return MakeSafeArea(root.transform, applySide);
        }

        public static RectTransform MakePopupRoot(string name, SafeAreaUtil.ApplySideType applySide, float dimAlpha, Vector2 bodySize,
            out GameObject root, out Canvas canvas, out CanvasScaler scaler)
        {
            root = MakeCanvasRoot(name, out canvas, out scaler);

            Image dim = UIBuild.MakeImage(root.transform, "Dim", new Color(0f, 0f, 0f, dimAlpha));
            UIBuild.Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            RectTransform safe = MakeSafeArea(root.transform, applySide);

            RectTransform body = UIBuild.MakeRect("Body", safe);
            body.anchorMin = new Vector2(0.5f, 0.5f);
            body.anchorMax = new Vector2(0.5f, 0.5f);
            body.pivot = new Vector2(0.5f, 0.5f);
            body.sizeDelta = bodySize;
            body.anchoredPosition = Vector2.zero;
            return body;
        }

        private static GameObject MakeCanvasRoot(string name, out Canvas canvas, out CanvasScaler scaler)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            UIBuild.Stretch((RectTransform)root.transform);

            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Jinhyeong_Common.CommonConfig.UI.ReferenceResolution;
            scaler.matchWidthOrHeight = Jinhyeong_Common.CommonConfig.UI.MatchWidthOrHeight;
            root.AddComponent<GraphicRaycaster>();
            return root;
        }

        private static RectTransform MakeSafeArea(Transform parent, SafeAreaUtil.ApplySideType applySide)
        {
            RectTransform safe = UIBuild.MakeRect("SafeArea", parent);
            UIBuild.Stretch(safe);
            SafeArea safeArea = safe.gameObject.AddComponent<SafeArea>();
            Bind(safeArea, "_rectTF", safe);
            BindInt(safeArea, "_applySide", (int)applySide);
            return safe;
        }

        private static RectTransform NewPanelRoot(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            UIBuild.Stretch(rt);
            return rt;
        }

        private static int BuildStartPanel()
        {
            RectTransform page = NewPanelRoot("StartPanel");

            Image sky = UIBuild.MakeImage(page, "Sky", UIBuild.ColorSky);
            UIBuild.Stretch(sky.rectTransform);

            RectTransform left = UIBuild.MakeRect("Left", page);
            left.anchorMin = new Vector2(0f, 0f);
            left.anchorMax = new Vector2(0.42f, 1f);
            left.offsetMin = new Vector2(24f, 24f);
            left.offsetMax = new Vector2(-12f, -24f);

            Image portrait = UIBuild.MakeImage(left, "Portrait", Color.white);
            portrait.preserveAspect = true;
            // 씬에 미리 배치된 메인 화면이 플레이 전에도 비어 보이지 않게 첫 주인공을 기본값으로 굽는다. 열리면 Open() 이 고른 주인공으로 바꾼다
            portrait.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/" + Jinhyeong_Common.RunSelection.HeroOf(1).PortraitPath + ".png");
            portrait.enabled = portrait.sprite != null;
            UIBuild.Stretch(portrait.rectTransform);

            RectTransform right = UIBuild.MakeRect("Right", page);
            right.anchorMin = new Vector2(0.42f, 0f);
            right.anchorMax = new Vector2(1f, 1f);
            right.offsetMin = new Vector2(12f, 20f);
            right.offsetMax = new Vector2(-40f, -20f);

            TMP_Text title = UIBuild.MakeText(right, "Title", LobbyWindow.GameTitle, 60f, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Normal;
            title.color = UIBuild.ColorGold;
            UIBuild.Band(title.rectTransform, 0.80f, 0.98f, 0f);

            TMP_Text tagline = UIBuild.MakeText(right, "Tagline", LobbyWindow.GameTagline, 22f, TextAlignmentOptions.Left);
            LocLabel.Attach(tagline, "Lobby_Tagline", "열 세계의 개 영웅, 12분 동안 침공군을 벤다.");
            tagline.color = new Color(0.78f, 0.82f, 0.95f, 1f);
            UIBuild.Band(tagline.rectTransform, 0.71f, 0.80f, 0f);

            TMP_Text loadout = UIBuild.MakeText(right, "Loadout", string.Empty, 20f, TextAlignmentOptions.TopLeft);
            loadout.color = new Color(0.82f, 0.86f, 1f, 1f);
            UIBuild.Band(loadout.rectTransform, 0.50f, 0.68f, 0f);

            // 맵 선택 줄 — ◀ 맵 이름 ▶. 잠긴 맵은 이름 대신 열리는 조건을 보여 주고 시작을 막는다
            RectTransform mapRow = UIBuild.MakeRect("MapRow", right);
            UIBuild.Band(mapRow, 0.39f, 0.48f, 0f);
            TMP_Text prevLabel;
            Button mapPrev = UIBuild.MakeButton(mapRow, "MapPrev", "<", 26f, UIBuild.ColorCard, out prevLabel);
            RectTransform mp = (RectTransform)mapPrev.transform;
            mp.anchorMin = new Vector2(0f, 0f);
            mp.anchorMax = new Vector2(0.12f, 1f);
            mp.offsetMin = Vector2.zero;
            mp.offsetMax = Vector2.zero;
            TMP_Text nextLabel;
            Button mapNext = UIBuild.MakeButton(mapRow, "MapNext", ">", 26f, UIBuild.ColorCard, out nextLabel);
            RectTransform mn = (RectTransform)mapNext.transform;
            mn.anchorMin = new Vector2(0.88f, 0f);
            mn.anchorMax = new Vector2(1f, 1f);
            mn.offsetMin = Vector2.zero;
            mn.offsetMax = Vector2.zero;
            TMP_Text mapLabel = UIBuild.MakeText(mapRow, "MapLabel", string.Empty, 20f, TextAlignmentOptions.Center);
            mapLabel.enableAutoSizing = true;
            mapLabel.fontSizeMin = 11f;
            mapLabel.fontSizeMax = 20f;
            mapLabel.rectTransform.anchorMin = new Vector2(0.12f, 0f);
            mapLabel.rectTransform.anchorMax = new Vector2(0.88f, 1f);
            mapLabel.rectTransform.offsetMin = Vector2.zero;
            mapLabel.rectTransform.offsetMax = Vector2.zero;

            TMP_Text record = UIBuild.MakeText(right, "Record", string.Empty, 17f, TextAlignmentOptions.Left);
            record.color = new Color(0.66f, 0.70f, 0.82f, 1f);
            UIBuild.Band(record.rectTransform, 0.26f, 0.37f, 0f);

            TMP_Text status = UIBuild.MakeText(right, "Status", string.Empty, 18f, TextAlignmentOptions.Left);
            status.color = new Color(0.7f, 0.7f, 0.75f, 1f);
            UIBuild.Band(status.rectTransform, 0.19f, 0.25f, 0f);

            TMP_Text startLabel;
            Button start = UIBuild.MakeButton(right, "StartButton", "게임 시작", 30f, UIBuild.ColorStart, out startLabel);
            LocLabel.Attach(startLabel, "Start_StartButton", "게임 시작");
            RectTransform sb = (RectTransform)start.transform;
            sb.anchorMin = new Vector2(0f, 0.04f);
            sb.anchorMax = new Vector2(0.62f, 0.17f);
            sb.offsetMin = Vector2.zero;
            sb.offsetMax = Vector2.zero;
            start.interactable = false;

            LobbyStartPanel panel = page.gameObject.AddComponent<LobbyStartPanel>();
            Bind(panel, "_portrait", portrait);
            Bind(panel, "_loadout", loadout);
            Bind(panel, "_record", record);
            Bind(panel, "_status", status);
            Bind(panel, "_startButton", start);
            TMP_Text version = UIBuild.MakeText(page, "Version", "v" + Application.version, 14f, TextAlignmentOptions.BottomRight);
            version.color = UIBuild.ColorDim;
            version.rectTransform.anchorMin = new Vector2(0.35f, 0f);
            version.rectTransform.anchorMax = new Vector2(1f, 0.05f);
            version.rectTransform.offsetMin = Vector2.zero;
            version.rectTransform.offsetMax = new Vector2(-16f, 0f);
            Bind(panel, "_version", version);
            Bind(panel, "_mapPrev", mapPrev);
            Bind(panel, "_mapNext", mapNext);
            Bind(panel, "_mapLabel", mapLabel);

            return Save(page.gameObject, LobbyStartPanel.PrefabPath);
        }

        private static int BuildCharacterPanel()
        {
            RectTransform page = NewPanelRoot("CharacterPanel");

            TMP_Text title = UIBuild.MakeText(page, "Title", "캐릭터 목록", 34f, TextAlignmentOptions.Center);
            LocLabel.Attach(title, "Char_Title", "캐릭터 목록");
            UIBuild.Band(title.rectTransform, 0.86f, 0.99f, 0f);

            TMP_Text hint = UIBuild.MakeText(page, "Hint", "고유 스킬의 계열이 이번 판에 더 자주 나옵니다", 19f, TextAlignmentOptions.Center);
            LocLabel.Attach(hint, "Char_Hint", "고유 스킬의 계열이 이번 판에 더 자주 나옵니다");
            hint.color = UIBuild.ColorMuted;
            UIBuild.Band(hint.rectTransform, 0.79f, 0.86f, 0f);

            RectTransform cardRoot = UIBuild.MakeRect("CardRoot", page);
            cardRoot.anchorMin = new Vector2(0f, 0.25f);
            cardRoot.anchorMax = new Vector2(1f, 0.76f);
            cardRoot.offsetMin = new Vector2(24f, 0f);
            cardRoot.offsetMax = new Vector2(-24f, 0f);
            // 캐릭터 10 → 5열 2줄. 세로 목록은 10장에서 카드가 26px 로 찌그러진다
            GridLayoutGroup grid = cardRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.cellSize = new Vector2(188f, 172f);
            grid.spacing = new Vector2(12f, 12f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            TMP_Text shopHint = UIBuild.MakeText(page, "ShopHint", string.Empty, 17f, TextAlignmentOptions.Center);
            shopHint.color = UIBuild.ColorMuted;
            UIBuild.Band(shopHint.rectTransform, 0.055f, 0.10f, 0f);

            RectTransform statRow = UIBuild.MakeRect("StatRow", page);
            statRow.anchorMin = new Vector2(0f, 0.10f);
            statRow.anchorMax = new Vector2(1f, 0.20f);
            statRow.offsetMin = new Vector2(40f, 0f);
            statRow.offsetMax = new Vector2(-40f, 0f);
            UIBuild.Horizontal(statRow, 12f);

            LobbyCharacterPanel panel = page.gameObject.AddComponent<LobbyCharacterPanel>();
            Bind(panel, "_cardRoot", cardRoot);
            Bind(panel, "_shopHint", shopHint);
            Bind(panel, "_statRow", statRow);

            return Save(page.gameObject, LobbyCharacterPanel.PrefabPath);
        }

        private static int BuildSkillPanel()
        {
            RectTransform page = NewPanelRoot("SkillPanel");

            TMP_Text hint = UIBuild.MakeText(page, "Hint", string.Empty, 18f, TextAlignmentOptions.Center);
            hint.color = UIBuild.ColorMuted;
            UIBuild.Band(hint.rectTransform, 0.93f, 0.995f, 0f);

            RectTransform viewport = UIBuild.MakeRect("Viewport", page);
            viewport.anchorMin = new Vector2(0f, 0.02f);
            viewport.anchorMax = new Vector2(1f, 0.92f);
            viewport.offsetMin = new Vector2(8f, 0f);
            viewport.offsetMax = new Vector2(-8f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();

            Image catcher = viewport.gameObject.AddComponent<Image>();
            catcher.color = new Color(0.055f, 0.06f, 0.09f, 1f);
            UISkin.ApplySliced(catcher, UISkin.PanelBg, UIBuild.PanelCorner);
            UIBuild.Outline(viewport, UIBuild.ColorOutline, UISkin.PanelFrame, UIBuild.PanelCorner);

            RectTransform content = UIBuild.MakeRect("Content", viewport);
            content.anchorMin = new Vector2(0.5f, 0.5f);
            content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(3000f, 3000f);
            content.anchoredPosition = Vector2.zero;

            GameObject edgeGo = new GameObject("Edges", typeof(RectTransform), typeof(CanvasRenderer));
            edgeGo.transform.SetParent(content, false);
            RectTransform edgeRt = (RectTransform)edgeGo.transform;
            edgeRt.anchorMin = new Vector2(0.5f, 0.5f);
            edgeRt.anchorMax = new Vector2(0.5f, 0.5f);
            edgeRt.pivot = new Vector2(0.5f, 0.5f);
            edgeRt.sizeDelta = new Vector2(3000f, 3000f);
            edgeRt.anchoredPosition = Vector2.zero;
            ConstellationEdgeGraphic edges = edgeGo.AddComponent<ConstellationEdgeGraphic>();
            edges.raycastTarget = false;

            ConstellationView view = viewport.gameObject.AddComponent<ConstellationView>();
            Bind(view, "_content", content);

            RectTransform tip = UIBuild.MakeRect("Tooltip", page);
            tip.anchorMin = new Vector2(0.5f, 0.5f);
            tip.anchorMax = new Vector2(0.5f, 0.5f);
            tip.pivot = new Vector2(0.5f, 0f);
            tip.sizeDelta = new Vector2(420f, 140f);
            Image tipBg = tip.gameObject.AddComponent<Image>();
            tipBg.color = new Color(0.10f, 0.11f, 0.16f, 0.97f);
            UISkin.ApplySliced(tipBg, UISkin.CardBg, UIBuild.CardCorner);
            UIBuild.Outline(tip, UIBuild.ColorOutline, UISkin.CardFrame, UIBuild.CardCorner);

            TMP_Text tipName = UIBuild.MakeText(tip, "TipName", string.Empty, 21f, TextAlignmentOptions.Center);
            UIBuild.Band(tipName.rectTransform, 0.62f, 0.97f, -16f);

            TMP_Text tipDesc = UIBuild.MakeText(tip, "TipDesc", string.Empty, 16f, TextAlignmentOptions.Center);
            tipDesc.color = UIBuild.ColorMuted;
            tipDesc.enableAutoSizing = true;
            tipDesc.fontSizeMin = 12f;
            tipDesc.fontSizeMax = 16f;
            UIBuild.Band(tipDesc.rectTransform, 0.30f, 0.62f, -16f);

            TMP_Text tipCost = UIBuild.MakeText(tip, "TipCost", string.Empty, 16f, TextAlignmentOptions.Center);
            tipCost.color = UIBuild.ColorGold;
            tipCost.enableAutoSizing = true;
            tipCost.fontSizeMin = 12f;
            tipCost.fontSizeMax = 16f;
            UIBuild.Band(tipCost.rectTransform, 0.03f, 0.30f, -16f);

            tip.gameObject.SetActive(false);

            LobbySkillPanel panel = page.gameObject.AddComponent<LobbySkillPanel>();
            Bind(panel, "_hint", hint);
            Bind(panel, "_content", content);
            Bind(panel, "_edges", edges);
            Bind(panel, "_view", view);
            Bind(panel, "_tooltip", tip);
            Bind(panel, "_tipName", tipName);
            Bind(panel, "_tipDesc", tipDesc);
            Bind(panel, "_tipCost", tipCost);

            return Save(page.gameObject, LobbySkillPanel.PrefabPath);
        }

        private static int BuildSettingsPanel()
        {
            RectTransform page = NewPanelRoot("SettingsPanel");

            TMP_Text title = UIBuild.MakeText(page, "Title", "설정", 34f, TextAlignmentOptions.Center);
            LocLabel.Attach(title, "Settings_Title", "설정");
            UIBuild.Band(title.rectTransform, 0.88f, 0.99f, 0f);

            TMP_Text resLabel = UIBuild.MakeText(page, "ResLabel", "해상도", 22f, TextAlignmentOptions.Center);
            LocLabel.Attach(resLabel, "Settings_Resolution", "해상도");
            resLabel.color = UIBuild.ColorSubHead;
            UIBuild.Band(resLabel.rectTransform, 0.79f, 0.86f, 0f);

            RectTransform resRow = UIBuild.MakeRect("ResRow", page);
            resRow.anchorMin = new Vector2(0.5f, 0.68f);
            resRow.anchorMax = new Vector2(0.5f, 0.78f);
            resRow.pivot = new Vector2(0.5f, 0.5f);
            resRow.sizeDelta = new Vector2(620f, 0f);
            resRow.anchoredPosition = Vector2.zero;
            UIBuild.Horizontal(resRow, 12f);

            TMP_Text moveLabel = UIBuild.MakeText(page, "MoveLabel", "이동키", 22f, TextAlignmentOptions.Center);
            LocLabel.Attach(moveLabel, "Settings_MoveKeys", "이동키");
            moveLabel.color = UIBuild.ColorSubHead;
            UIBuild.Band(moveLabel.rectTransform, 0.585f, 0.655f, 0f);

            RectTransform moveRow = UIBuild.MakeRect("MoveRow", page);
            moveRow.anchorMin = new Vector2(0.5f, 0.475f);
            moveRow.anchorMax = new Vector2(0.5f, 0.575f);
            moveRow.pivot = new Vector2(0.5f, 0.5f);
            moveRow.sizeDelta = new Vector2(420f, 0f);
            moveRow.anchoredPosition = Vector2.zero;
            UIBuild.Horizontal(moveRow, 12f);

            RectTransform langRow = UIBuild.MakeRect("LangRow", page);
            langRow.anchorMin = new Vector2(0.5f, 0.88f);
            langRow.anchorMax = new Vector2(0.5f, 0.97f);
            langRow.pivot = new Vector2(1f, 0.5f);
            langRow.sizeDelta = new Vector2(300f, 0f);
            langRow.anchoredPosition = new Vector2(420f, 0f);
            UIBuild.Horizontal(langRow, 8f);

            TMP_Text volLabel = UIBuild.MakeText(page, "VolLabel", "음량", 22f, TextAlignmentOptions.Center);
            LocLabel.Attach(volLabel, "Settings_Volume", "음량");
            volLabel.color = UIBuild.ColorSubHead;
            UIBuild.Band(volLabel.rectTransform, 0.40f, 0.465f, 0f);

            RectTransform volRow = UIBuild.MakeRect("VolRow", page);
            volRow.anchorMin = new Vector2(0.5f, 0.30f);
            volRow.anchorMax = new Vector2(0.5f, 0.39f);
            volRow.pivot = new Vector2(0.5f, 0.5f);
            volRow.sizeDelta = new Vector2(620f, 0f);
            volRow.anchoredPosition = Vector2.zero;
            UIBuild.Horizontal(volRow, 10f);

            TMP_Text fsLabel;
            Button fullscreen = UIBuild.MakeButton(page, "Fullscreen", "전체화면", 20f, UIBuild.ColorCard, out fsLabel);
            RectTransform fr = (RectTransform)fullscreen.transform;
            fr.anchorMin = new Vector2(0.5f, 0.20f);
            fr.anchorMax = new Vector2(0.5f, 0.28f);
            fr.pivot = new Vector2(0.5f, 0.5f);
            fr.sizeDelta = new Vector2(300f, 0f);
            fr.anchoredPosition = Vector2.zero;

            TMP_Text note = UIBuild.MakeText(page, "Note", "에디터에서는 해상도 변경이 Game 뷰에 반영되지 않는다", 15f, TextAlignmentOptions.Center);
            LocLabel.Attach(note, "Settings_EditorResNote", "에디터에서는 해상도 변경이 Game 뷰에 반영되지 않는다");
            note.color = UIBuild.ColorDim;
            UIBuild.Band(note.rectTransform, 0.155f, 0.195f, 0f);


            TMP_Text wipeLabel;
            Button wipe = UIBuild.MakeButton(page, "Wipe", "데이터 초기화", 18f, UIBuild.ColorDanger, out wipeLabel);
            RectTransform wr = (RectTransform)wipe.transform;
            wr.anchorMin = new Vector2(0.5f, 0.065f);
            wr.anchorMax = new Vector2(0.5f, 0.14f);
            wr.pivot = new Vector2(0.5f, 0.5f);
            wr.sizeDelta = new Vector2(300f, 0f);
            wr.anchoredPosition = Vector2.zero;

            TMP_Text wipeNote = UIBuild.MakeText(page, "WipeNote", "기록 · 재화 · 해금 · 강화가 모두 사라집니다", 14f, TextAlignmentOptions.Center);
            LocLabel.Attach(wipeNote, "Settings_WipeNote", "기록 · 재화 · 해금 · 강화가 모두 사라집니다");
            wipeNote.color = new Color(0.6f, 0.45f, 0.45f, 1f);
            UIBuild.Band(wipeNote.rectTransform, 0.02f, 0.06f, 0f);

            LobbySettingsPanel panel = page.gameObject.AddComponent<LobbySettingsPanel>();
            Bind(panel, "_resRow", resRow);
            Bind(panel, "_moveRow", moveRow);
            Bind(panel, "_volRow", volRow);
            Bind(panel, "_langRow", langRow);
            Bind(panel, "_fullscreenButton", fullscreen);
            Bind(panel, "_fullscreenLabel", fsLabel);
            Bind(panel, "_wipeButton", wipe);
            Bind(panel, "_wipeLabel", wipeLabel);

            return Save(page.gameObject, LobbySettingsPanel.PrefabPath);
        }

        private static int BuildHeroCard()
        {
            GameObject go = new GameObject("HeroCard", typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            UIBuild.PreferredHeight(rt, 100f);
            Image bg = go.AddComponent<Image>();
            bg.color = UIBuild.ColorCard;
            UISkin.ApplySliced(bg, UISkin.CardBg, UIBuild.BigCardCorner);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            UIBuild.Outline(rt, UIBuild.ColorOutline, UISkin.CardFrame, UIBuild.BigCardCorner);

            Image face = UIBuild.MakeImage(rt, "Face", Color.white);
            face.preserveAspect = true;
            face.raycastTarget = false;
            face.enabled = false;
            face.rectTransform.anchorMin = new Vector2(0f, 0.36f);
            face.rectTransform.anchorMax = new Vector2(1f, 1f);
            face.rectTransform.offsetMin = new Vector2(12f, 0f);
            face.rectTransform.offsetMax = new Vector2(-12f, -8f);

            TMP_Text label = UIBuild.MakeText(rt, "Label", string.Empty, 18f, TextAlignmentOptions.Center);
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 18f;
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 0.36f);
            label.rectTransform.offsetMin = new Vector2(8f, 6f);
            label.rectTransform.offsetMax = new Vector2(-8f, 0f);

            HeroCardComponent comp = go.AddComponent<HeroCardComponent>();
            Bind(comp, "_bg", bg);
            Bind(comp, "_face", face);
            Bind(comp, "_label", label);
            Bind(comp, "_button", button);

            return Save(go, HeroCardComponent.PrefabPath);
        }

        private static int BuildConstellationNode()
        {
            GameObject go = new GameObject("ConstellationNode", typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(34f, 34f);

            Image bg = go.AddComponent<Image>();
            bg.color = UIBuild.ColorCard;
            UISkin.ApplySimple(bg, UISkin.CircleBg);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = bg;

            Image icon = UIBuild.MakeImage(rt, "Icon", Color.white);
            icon.raycastTarget = false;
            icon.enabled = false;
            icon.preserveAspect = true;
            RectTransform ir = icon.rectTransform;
            ir.anchorMin = Vector2.zero;
            ir.anchorMax = Vector2.one;
            ir.offsetMin = new Vector2(IconInset, IconInset);
            ir.offsetMax = new Vector2(-IconInset, -IconInset);

            Image frame = UIBuild.MakeImage(rt, "Frame", ConstellationPalette.FrameLocked);
            frame.raycastTarget = false;
            UIBuild.Stretch(frame.rectTransform);
            UISkin.ApplySimple(frame, UISkin.CircleFrame);

            ConstellationNodeComponent comp = go.AddComponent<ConstellationNodeComponent>();
            Bind(comp, "_bg", bg);
            Bind(comp, "_icon", icon);
            Bind(comp, "_frame", frame);
            Bind(comp, "_button", button);

            return Save(go, ConstellationNodeComponent.PrefabPath);
        }

        private const float IconInset = 7f;

        private static int BuildLabelButton()
        {
            GameObject go = new GameObject("LabelButton", typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            Image bg = go.AddComponent<Image>();
            bg.color = UIBuild.ColorCard;
            UISkin.ApplySliced(bg, UISkin.CardBg, UIBuild.CardCorner);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            UIBuild.Outline(rt, UIBuild.ColorOutline, UISkin.CardFrame, UIBuild.CardCorner);

            TMP_Text label = UIBuild.MakeText(rt, "Label", string.Empty, 18f, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Normal;
            UIBuild.Stretch(label.rectTransform);

            LabelButtonComponent comp = go.AddComponent<LabelButtonComponent>();
            Bind(comp, "_bg", bg);
            Bind(comp, "_label", label);
            Bind(comp, "_button", button);

            return Save(go, LabelButtonComponent.PrefabPath);
        }

        // 씬의 로비 = 프리팹 인스턴스 정확히 하나 (10-04 사용자: "미리 씬에 배치되어 있는게 맞다").
        // 예전에는 여기서 씬 인스턴스를 전부 지우고 키로만 로드했다 — 이제는 프리팹과 끊긴 옛 사본만 지우고 없으면 배치한다
        private static void MigrateScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            string log = EnsureLobbyInScene(scene);
            if (string.IsNullOrEmpty(log) == false)
            {
                Debug.Log(log);
            }
        }

        private static string EnsureLobbyInScene(Scene scene)
        {
            GameObject root = GameObject.Find(UIRootName);
            if (root == null)
            {
                root = new GameObject(UIRootName);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LobbyPrefabPath);
            int removed = 0;
            int kept = 0;
            LobbyWindow[] found = Object.FindObjectsByType<LobbyWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] == null)
                {
                    continue;
                }
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(found[i].gameObject);
                bool linked = prefab != null && source == prefab && kept == 0;
                if (linked)
                {
                    kept++;
                    continue;
                }
                Object.DestroyImmediate(found[i].gameObject);
                removed++;
            }

            bool placed = false;
            if (kept == 0 && prefab != null)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                inst.SetActive(true);
                placed = true;
            }

            // 시작 탭 패널도 Container 밑에 미리 둔다 — 로비 창만 있으면 레일만 보이고 본문이 비어 있다
            LobbyWindow lobby = root.GetComponentInChildren<LobbyWindow>(true);
            Transform container = lobby != null ? lobby.transform.Find("SafeArea/Container") : null;
            GameObject startPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StartPanelPrefabPath);
            if (container != null && startPrefab != null && container.GetComponentInChildren<LobbyStartPanel>(true) == null)
            {
                GameObject panel = (GameObject)PrefabUtility.InstantiatePrefab(startPrefab, container);
                RectTransform rt = panel.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchoredPosition3D = Vector3.zero;
                    rt.localScale = Vector3.one;
                }
                panel.SetActive(true);
                placed = true;
            }

            if (removed == 0 && placed == false)
            {
                return null;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return $"[UIPrefabBuilder] 씬 로비 정리 — 옛 사본 {removed}개 제거 · 프리팹 배치 {(placed ? 1 : 0)}개";
        }

        private static string AssetPathOf(string address)
        {
            return PrefabRoot + address + ".prefab";
        }

        private static int Save(GameObject go, string address)
        {
            string path = AssetPathOf(address);
            string dir = System.IO.Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir) == false && System.IO.Directory.Exists(dir) == false)
            {
                System.IO.Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            RegisterAddressable(path, address);
            return 1;
        }

        private static void RegisterAddressable(string path, string address)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("[UIPrefabBuilder] Addressables 설정이 없다 — 수동 등록 필요");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                return;
            }

            AddressableAssetGroup group = settings.FindGroup(GroupName);
            if (group == null)
            {
                group = settings.CreateGroup(GroupName, false, false, true, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry != null)
            {
                entry.address = address;
            }
        }

        private static void Bind(Object target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError($"[UIPrefabBuilder] {target.GetType().Name} 에 '{field}' 가 없다 — 바인딩 계약이 어긋났다");
                return;
            }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindArray(Object target, string field, Object[] values)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError($"[UIPrefabBuilder] {target.GetType().Name} 에 '{field}' 가 없다 — 바인딩 계약이 어긋났다");
                return;
            }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindInt(Object target, string field, int value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError($"[UIPrefabBuilder] {target.GetType().Name} 에 '{field}' 가 없다");
                return;
            }
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindBool(Object target, string field, bool value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError($"[UIPrefabBuilder] {target.GetType().Name} 에 '{field}' 가 없다");
                return;
            }
            p.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
