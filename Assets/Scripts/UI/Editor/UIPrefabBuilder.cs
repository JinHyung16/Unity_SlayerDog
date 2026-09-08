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

    /// <summary>
    /// 로비의 골격 프리팹을 굽는다 — Window 1 · Panel 4 · Component 4.
    ///
    /// 확정표 7행 = **하이브리드**: 정적 골격(배치 · 라벨 · 컨테이너)은 프리팹, 동적 값과 반복 요소는 코드 + 풀.
    /// 그래서 여기서 굽는 것은 「빈 자리가 있는 골격」이다. 카드 · 칩 · 버튼 N개를 박지 않는다.
    ///
    /// - 같은 경로에 덮어써 **GUID 를 유지**한다. 지우고 새로 만들면 씬 · 어드레서블 참조가 전부 끊긴다
    /// - **멱등**이다. 두 번 돌려도 결과가 같다
    /// - YAML 손편집 대신 공식 API(PrefabUtility · SerializedObject)로만 만진다
    /// - 어드레서블 "UI" 그룹에 주소 = 클래스의 PrefabPath 로 등록한다
    /// - 구운 뒤 육안 확인은 면제되지 않는다 — 하네스 스크린샷으로 본다
    /// </summary>
    public static class UIPrefabBuilder
    {
        private const string PrefabRoot = "Assets/_Art/Prefabs/";
        private const string GroupName = "UI";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string UIRootName = "UIRoot";

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

        /// <summary>
        /// 스킨 스프라이트가 «지금 어떤 경계로 임포트돼 있는지» 찍는다.
        /// meta 를 고쳤다고 반영된 것이 아니다 — 리임포트가 돌아야 값이 바뀐다.
        /// 굽을 때마다 실제 값을 보면 「고쳤는데 왜 그대로냐」를 추측하지 않아도 된다.
        /// </summary>
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

        // ───────────────────────── Window ─────────────────────────

        private static int BuildLobbyWindow()
        {
            GameObject root = new GameObject("LobbyWindow", typeof(RectTransform));
            RectTransform rootRt = (RectTransform)root.transform;
            UIBuild.Stretch(rootRt);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Jinhyeong_Common.CommonConfig.UI.ReferenceResolution;
            scaler.matchWidthOrHeight = Jinhyeong_Common.CommonConfig.UI.MatchWidthOrHeight;
            root.AddComponent<GraphicRaycaster>();

            // 배경은 SafeArea 밖 — 노치 영역이 게임 화면으로 비치면 안 된다
            Image bg = UIBuild.MakeImage(root.transform, "Background", UIBuild.ColorBg);
            UIBuild.Stretch(bg.rectTransform);

            RectTransform safe = UIBuild.MakeRect("SafeArea", root.transform);
            UIBuild.Stretch(safe);
            SafeArea safeArea = safe.gameObject.AddComponent<SafeArea>();
            Bind(safeArea, "_rectTF", safe);
            BindInt(safeArea, "_applySide", (int)SafeAreaUtil.ApplySideType.All);   // 가로 화면은 좌우도 피한다

            // 좌측 내비 — 탭 버튼 4개. 개수가 고정이라 골격에 둔다
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
                UIBuild.PreferredHeight((RectTransform)b.transform, 62f);
                tabButtons[i] = b;
                tabImages[i] = b.image;
            }

            // 패널이 로드되는 빈 자리
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

        // ───────────────────────── Panels ─────────────────────────

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

            // 가로 화면은 좌우로 나눠 쓴다 — 왼쪽 주인공 · 오른쪽 글과 버튼
            Image sky = UIBuild.MakeImage(page, "Sky", UIBuild.ColorSky);
            UIBuild.Stretch(sky.rectTransform);

            RectTransform left = UIBuild.MakeRect("Left", page);
            left.anchorMin = new Vector2(0f, 0f);
            left.anchorMax = new Vector2(0.42f, 1f);
            left.offsetMin = new Vector2(24f, 24f);
            left.offsetMax = new Vector2(-12f, -24f);

            Image portrait = UIBuild.MakeImage(left, "Portrait", Color.white);
            portrait.preserveAspect = true;
            portrait.enabled = false;
            UIBuild.Stretch(portrait.rectTransform);

            RectTransform right = UIBuild.MakeRect("Right", page);
            right.anchorMin = new Vector2(0.42f, 0f);
            right.anchorMax = new Vector2(1f, 1f);
            right.offsetMin = new Vector2(12f, 20f);
            right.offsetMax = new Vector2(-40f, -20f);

            TMP_Text title = UIBuild.MakeText(right, "Title", LobbyWindow.GameTitle, 60f, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;
            title.color = UIBuild.ColorGold;
            UIBuild.Band(title.rectTransform, 0.80f, 0.98f, 0f);

            TMP_Text tagline = UIBuild.MakeText(right, "Tagline", LobbyWindow.GameTagline, 22f, TextAlignmentOptions.Left);
            tagline.color = new Color(0.78f, 0.82f, 0.95f, 1f);
            UIBuild.Band(tagline.rectTransform, 0.71f, 0.80f, 0f);

            TMP_Text loadout = UIBuild.MakeText(right, "Loadout", string.Empty, 20f, TextAlignmentOptions.TopLeft);
            loadout.color = new Color(0.82f, 0.86f, 1f, 1f);
            UIBuild.Band(loadout.rectTransform, 0.30f, 0.68f, 0f);

            TMP_Text record = UIBuild.MakeText(right, "Record", string.Empty, 17f, TextAlignmentOptions.Left);
            record.color = new Color(0.66f, 0.70f, 0.82f, 1f);
            UIBuild.Band(record.rectTransform, 0.26f, 0.38f, 0f);

            TMP_Text status = UIBuild.MakeText(right, "Status", string.Empty, 18f, TextAlignmentOptions.Left);
            status.color = new Color(0.7f, 0.7f, 0.75f, 1f);
            UIBuild.Band(status.rectTransform, 0.19f, 0.25f, 0f);

            TMP_Text startLabel;
            Button start = UIBuild.MakeButton(right, "StartButton", "게임 시작", 30f, UIBuild.ColorStart, out startLabel);
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

            return Save(page.gameObject, LobbyStartPanel.PrefabPath);
        }

        private static int BuildCharacterPanel()
        {
            RectTransform page = NewPanelRoot("CharacterPanel");

            TMP_Text title = UIBuild.MakeText(page, "Title", "캐릭터 목록", 34f, TextAlignmentOptions.Center);
            UIBuild.Band(title.rectTransform, 0.86f, 0.99f, 0f);

            TMP_Text hint = UIBuild.MakeText(page, "Hint", "고유 스킬의 계열이 이번 판에 더 자주 나온다", 19f, TextAlignmentOptions.Center);
            hint.color = UIBuild.ColorMuted;
            UIBuild.Band(hint.rectTransform, 0.79f, 0.86f, 0f);

            // 카드가 쌓이는 자리. 아래 1/4 는 강화 줄 몫 — 카드는 0.25 위에서 끝난다
            RectTransform cardRoot = UIBuild.MakeRect("CardRoot", page);
            cardRoot.anchorMin = new Vector2(0f, 0.25f);
            cardRoot.anchorMax = new Vector2(1f, 0.76f);
            // [사고] 여백을 0 으로 뒀더니 카드 테두리가 화면 오른쪽 끝에서 잘렸다.
            // 단색이던 시절엔 안 보였다 — 모양이 생기면 «가장자리»가 보이기 시작한다.
            cardRoot.offsetMin = new Vector2(24f, 0f);
            cardRoot.offsetMax = new Vector2(-24f, 0f);
            UIBuild.Vertical(cardRoot, 10f, false);

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

        /// <summary>
        /// 성좌 화면. 골격은 셋뿐이다 — 창(Viewport) · 통째로 움직이는 판(Content) · 아래 정보줄.
        /// 별 220개와 선 250개는 골격에 없다. 열 때 만들어 풀에 돌려준다 (확정표 7행: 하이브리드).
        /// </summary>
        /// <summary>
        /// 성좌 화면. 골격은 셋뿐이다 — 창(Viewport) · 통째로 움직이는 판(Content) · 떠 있는 설명 패널.
        /// 별 200개와 선 240개는 골격에 없다. 열 때 만들어 풀에 돌려준다 (확정표 7행: 하이브리드).
        ///
        /// [사고] 설명을 화면 «아래 고정 줄»에 뒀더니, 보고 있는 별과 글자가 화면 양 끝으로 갈라져
        /// 눈이 왕복했다. 설명은 누른 별 옆에 있어야 한다 — 그리고 그 자리를 성좌에 돌려주면
        /// 창이 세로로 130px 넓어진다.
        /// </summary>
        private static int BuildSkillPanel()
        {
            RectTransform page = NewPanelRoot("SkillPanel");

            TMP_Text hint = UIBuild.MakeText(page, "Hint", string.Empty, 18f, TextAlignmentOptions.Center);
            hint.color = UIBuild.ColorMuted;
            UIBuild.Band(hint.rectTransform, 0.93f, 0.995f, 0f);

            // 창. 여기서 잘라야 성좌가 내비 레일 위로 넘치지 않는다.
            RectTransform viewport = UIBuild.MakeRect("Viewport", page);
            viewport.anchorMin = new Vector2(0f, 0.02f);
            viewport.anchorMax = new Vector2(1f, 0.92f);
            viewport.offsetMin = new Vector2(8f, 0f);
            viewport.offsetMax = new Vector2(-8f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();

            // 드래그를 받으려면 투명하더라도 그래픽이 있어야 한다. 완전 투명이면 레이캐스트가 통과한다.
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

            // [사고] CanvasRenderer 없이 Graphic 을 붙였다가 MissingComponentException 이 났다.
            // RequireComponent 는 «인스펙터에서 붙일 때» 도와주는 것이지, 코드로 AddComponent 하는
            // 이 경로를 대신해 주지 않는다. 렌더러를 먼저 세운다.
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

            // 설명 패널 — 창 «밖»에 둔다. 창 안에 두면 잘라내기에 걸려 가장자리 별에서 반쯤 잘린다.
            RectTransform tip = UIBuild.MakeRect("Tooltip", page);
            tip.anchorMin = new Vector2(0.5f, 0.5f);
            tip.anchorMax = new Vector2(0.5f, 0.5f);
            tip.pivot = new Vector2(0.5f, 0f);
            tip.sizeDelta = new Vector2(340f, 108f);
            Image tipBg = tip.gameObject.AddComponent<Image>();
            tipBg.color = new Color(0.10f, 0.11f, 0.16f, 0.97f);
            UISkin.ApplySliced(tipBg, UISkin.CardBg, UIBuild.CardCorner);
            UIBuild.Outline(tip, UIBuild.ColorOutline, UISkin.CardFrame, UIBuild.CardCorner);

            TMP_Text tipName = UIBuild.MakeText(tip, "TipName", string.Empty, 21f, TextAlignmentOptions.Center);
            UIBuild.Band(tipName.rectTransform, 0.62f, 0.97f, -16f);

            TMP_Text tipDesc = UIBuild.MakeText(tip, "TipDesc", string.Empty, 16f, TextAlignmentOptions.Center);
            tipDesc.color = UIBuild.ColorMuted;
            UIBuild.Band(tipDesc.rectTransform, 0.30f, 0.62f, -16f);

            TMP_Text tipCost = UIBuild.MakeText(tip, "TipCost", string.Empty, 16f, TextAlignmentOptions.Center);
            tipCost.color = UIBuild.ColorGold;
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
            UIBuild.Band(title.rectTransform, 0.88f, 0.99f, 0f);

            TMP_Text resLabel = UIBuild.MakeText(page, "ResLabel", "해상도", 22f, TextAlignmentOptions.Center);
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
            moveLabel.color = UIBuild.ColorSubHead;
            UIBuild.Band(moveLabel.rectTransform, 0.585f, 0.655f, 0f);

            // 프리셋이 둘뿐이라 해상도 줄보다 좁다. 같은 폭으로 두면 버튼 하나가 과하게 커진다.
            RectTransform moveRow = UIBuild.MakeRect("MoveRow", page);
            moveRow.anchorMin = new Vector2(0.5f, 0.475f);
            moveRow.anchorMax = new Vector2(0.5f, 0.575f);
            moveRow.pivot = new Vector2(0.5f, 0.5f);
            moveRow.sizeDelta = new Vector2(420f, 0f);
            moveRow.anchoredPosition = Vector2.zero;
            UIBuild.Horizontal(moveRow, 12f);

            TMP_Text fsLabel;
            Button fullscreen = UIBuild.MakeButton(page, "Fullscreen", "전체화면", 20f, UIBuild.ColorCard, out fsLabel);
            RectTransform fr = (RectTransform)fullscreen.transform;
            fr.anchorMin = new Vector2(0.5f, 0.365f);
            fr.anchorMax = new Vector2(0.5f, 0.455f);
            fr.pivot = new Vector2(0.5f, 0.5f);
            fr.sizeDelta = new Vector2(300f, 0f);
            fr.anchoredPosition = Vector2.zero;

            TMP_Text note = UIBuild.MakeText(page, "Note", "에디터에서는 해상도 변경이 Game 뷰에 반영되지 않는다", 16f, TextAlignmentOptions.Center);
            note.color = UIBuild.ColorDim;
            UIBuild.Band(note.rectTransform, 0.30f, 0.35f, 0f);

            TMP_Text wipeLabel;
            Button wipe = UIBuild.MakeButton(page, "Wipe", "데이터 초기화", 18f, UIBuild.ColorDanger, out wipeLabel);
            RectTransform wr = (RectTransform)wipe.transform;
            wr.anchorMin = new Vector2(0.5f, 0.14f);
            wr.anchorMax = new Vector2(0.5f, 0.24f);
            wr.pivot = new Vector2(0.5f, 0.5f);
            wr.sizeDelta = new Vector2(300f, 0f);
            wr.anchoredPosition = Vector2.zero;

            TMP_Text wipeNote = UIBuild.MakeText(page, "WipeNote", "기록 · 재화 · 해금 · 강화가 모두 사라진다", 15f, TextAlignmentOptions.Center);
            wipeNote.color = new Color(0.6f, 0.45f, 0.45f, 1f);
            UIBuild.Band(wipeNote.rectTransform, 0.08f, 0.13f, 0f);

            LobbySettingsPanel panel = page.gameObject.AddComponent<LobbySettingsPanel>();
            Bind(panel, "_resRow", resRow);
            Bind(panel, "_moveRow", moveRow);
            Bind(panel, "_fullscreenButton", fullscreen);
            Bind(panel, "_fullscreenLabel", fsLabel);
            Bind(panel, "_wipeButton", wipe);
            Bind(panel, "_wipeLabel", wipeLabel);

            return Save(page.gameObject, LobbySettingsPanel.PrefabPath);
        }

        // ───────────────────────── Components ─────────────────────────

        private static int BuildHeroCard()
        {
            GameObject go = new GameObject("HeroCard", typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            UIBuild.PreferredHeight(rt, 100f);
            Image bg = go.AddComponent<Image>();
            bg.color = UIBuild.ColorCard;
            // 카드가 100px 로 크다. 버튼과 같은 10px 로 두면 각져 보인다 — 모서리는 크기에 비례해야 같아 보인다.
            UISkin.ApplySliced(bg, UISkin.CardBg, UIBuild.BigCardCorner);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            UIBuild.Outline(rt, UIBuild.ColorOutline, UISkin.CardFrame, UIBuild.BigCardCorner);

            Image face = UIBuild.MakeImage(rt, "Face", Color.white);
            face.preserveAspect = true;
            face.raycastTarget = false;
            face.enabled = false;
            face.rectTransform.anchorMin = new Vector2(0f, 0f);
            face.rectTransform.anchorMax = new Vector2(0.3f, 1f);
            face.rectTransform.offsetMin = new Vector2(20f, 12f);
            face.rectTransform.offsetMax = new Vector2(-10f, -12f);

            TMP_Text label = UIBuild.MakeText(rt, "Label", string.Empty, 22f, TextAlignmentOptions.Left);
            label.rectTransform.anchorMin = new Vector2(0.32f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.offsetMin = new Vector2(0f, 12f);
            label.rectTransform.offsetMax = new Vector2(-24f, -12f);

            HeroCardComponent comp = go.AddComponent<HeroCardComponent>();
            Bind(comp, "_bg", bg);
            Bind(comp, "_face", face);
            Bind(comp, "_label", label);
            Bind(comp, "_button", button);

            return Save(go, HeroCardComponent.PrefabPath);
        }

        /// <summary>
        /// 성좌의 별 하나. 이름표는 «별 아래»에 둔다 — 별 안에 넣으면 22유닛짜리 잔가지에서 글자가 삐져나온다.
        /// 이름표는 레이캐스트를 끈다. 켜 두면 옆 별의 이름이 클릭을 가로챈다.
        /// </summary>
        /// <summary>
        /// 성좌의 별 — 바탕 · 아이콘 · 테두리 세 겹.
        /// 셋이 서로 다른 것을 말한다: 계열(색) · 무슨 스킬(그림) · 살 수 있나(테두리 밝기).
        /// 이름표는 없다 — 누르면 그 위에 설명 패널이 뜬다.
        /// </summary>
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

            // 아이콘은 별보다 작다. 테두리에 닿으면 무슨 그림인지 안 읽힌다.
            Image icon = UIBuild.MakeImage(rt, "Icon", Color.white);
            icon.raycastTarget = false;
            icon.enabled = false;
            icon.preserveAspect = true;
            RectTransform ir = icon.rectTransform;
            ir.anchorMin = Vector2.zero;
            ir.anchorMax = Vector2.one;
            ir.offsetMin = new Vector2(IconInset, IconInset);
            ir.offsetMax = new Vector2(-IconInset, -IconInset);

            // 테두리가 «맨 위»다. 아이콘 뒤에 두면 아이콘이 테두리를 덮어 상태가 안 보인다.
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

        /// <summary>별 안쪽 여백(그래프 유닛). 34유닛짜리 잔가지 기준이라 비율로 두면 큰 별에서 과해진다.</summary>
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
            label.fontStyle = FontStyles.Bold;
            UIBuild.Stretch(label.rectTransform);

            LabelButtonComponent comp = go.AddComponent<LabelButtonComponent>();
            Bind(comp, "_bg", bg);
            Bind(comp, "_label", label);
            Bind(comp, "_button", button);

            return Save(go, LabelButtonComponent.PrefabPath);
        }

        // ───────────────────────── 씬 이전 ─────────────────────────

        /// <summary>
        /// 씬에 박혀 있던 옛 LobbyWindow 인스턴스를 걷어낸다. 창은 이제 키로 로드된다.
        /// UIRoot 는 남긴다 — 런타임 창이 세워지는 자리다.
        /// </summary>
        private static void MigrateScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            int removed = 0;
            LobbyWindow[] old = Object.FindObjectsByType<LobbyWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < old.Length; i++)
            {
                if (old[i] == null)
                {
                    continue;
                }
                Object.DestroyImmediate(old[i].gameObject);
                removed++;
            }

            if (GameObject.Find(UIRootName) == null)
            {
                new GameObject(UIRootName);
            }

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[UIPrefabBuilder] 씬에서 옛 LobbyWindow 인스턴스 {removed}개 제거 — 이제 키로 로드된다");
            }
        }

        // ───────────────────────── 공통 ─────────────────────────

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

            // 같은 경로에 덮어쓴다 — GUID 가 유지된다
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
