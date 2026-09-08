using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Jinhyeong_AI;
using Jinhyeong_Character;
using Jinhyeong_Common;
using Jinhyeong_Input;
using Jinhyeong_Managers;
using Jinhyeong_Parts;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_UI
{

    /// <summary>
    /// 세로 서바이버 인게임 HUD. 캔버스를 코드로 구성한다 —
    /// 씬·프리팹을 손대지 않아야 다른 작업과 충돌하지 않고, 매니저들의 Ensure() 패턴과도 맞는다.
    ///
    /// 구성:
    ///   SafeArea(Vertical)
    ///   ├ 상단  생존시간 · 킬 수 · HP 바
    ///   └ 하단  플로팅 조이스틱 존 (화면 아래 45%)
    ///
    /// 게임 시작 전에는 숨긴다. 이벤트 구독 대신 GameFlow.IsRunning 을 폴링하는 이유는
    /// GameFlow.Bootstrap(AfterSceneLoad)이 이벤트를 통째로 비워서 구독 순서 문제에 걸리기 때문이다.
    /// </summary>
    public class SurvivorHud : BaseBehaviour
    {
        public static SurvivorHud Instance { get; private set; }

        public static SurvivorHud Ensure()
        {
            if (Instance != null)
                return Instance;
            GameObject go = new GameObject("_SurvivorHud");
            return go.AddComponent<SurvivorHud>();
        }

        private GameObject _root;
        private TMP_Text _timeText;
        private TMP_Text _killText;
        private TMP_Text _levelText;
        private Image _hpFill;
        private Image _xpFill;

        private TMP_Text _partsText;
        private bool _levelUpHooked;
        private bool _partsHooked;

        private TMP_Text _banner;
        private float _bannerUntil;
        private bool _bannerHooked;

        private GameObject _resultPanel;
        private TMP_Text _resultText;
        private TMP_Text _resultTitle;

        /// <summary>결과 화면 딤의 상태. 실측 리포트가 읽는다.</summary>
        public static string DimNote { get; private set; } = string.Empty;
        private bool _endHooked;

        private Damageable _playerHp;

        private static Sprite _whiteSprite;

        /// <summary>막대의 모서리 반지름(픽셀). 로비와 같은 스프라이트를 쓰되 막대는 더 얇아 작게 잡는다.</summary>
        private const float BarCorner = 7f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            // 결과 패널이 떠 있는 동안은 런이 끝났어도 HUD 루트를 유지한다
            bool show = GameFlow.IsRunning || GameFlow.IsEnded;
            if (_root.activeSelf != show)
                _root.SetActive(show);
            if (show == false)
                return;

            if (_endHooked == false && (GameFlow.IsRunning || GameFlow.IsEnded))
            {
                GameFlow.OnGameEnded += ShowResult;
                _endHooked = true;
            }
            if (GameFlow.IsEnded)
                return;

            WaveDirector wave = WaveDirector.Instance;
            if (wave != null)
            {
                if (_bannerHooked == false)
                {
                    wave.OnBanner += ShowBanner;
                    _bannerHooked = true;
                }
                if (_banner.gameObject.activeSelf && Time.unscaledTime >= _bannerUntil)
                    _banner.gameObject.SetActive(false);

                int total = Mathf.FloorToInt(wave.Elapsed);
                _timeText.text = string.Format("{0:00}:{1:00}", total / 60, total % 60);
            }

            EnemySpawner spawner = EnemySpawner.Instance;
            if (spawner != null)
                _killText.text = spawner.KillCount.ToString();

            RunProgress progress = RunProgress.Instance;
            if (progress != null)
            {
                // 부트스트랩 순서에 안 걸리게 인스턴스가 잡히는 시점에 구독한다
                if (_levelUpHooked == false)
                {
                    progress.OnLevelUp += HandleLevelUp;
                    _levelUpHooked = true;
                }
                _levelText.text = "Lv." + progress.Level;
                _xpFill.fillAmount = progress.XpToNext > 0f ? Mathf.Clamp01(progress.Xp / progress.XpToNext) : 0f;
            }

            // 파츠 스트립은 변경 이벤트로만 갱신한다. 매 프레임 문자열을 만들면 GC 가 튄다.
            if (_partsHooked == false)
            {
                Player pp = GameEvents.CurrentPlayer;
                PartLoadout pl = pp != null ? pp.GetComponent<PartLoadout>() : null;
                if (pl != null)
                {
                    pl.OnPartChanged += RefreshParts;
                    _partsHooked = true;
                    RefreshParts(null);
                }
            }

            // 풀·리스폰으로 플레이어가 바뀔 수 있어 참조가 죽으면 다시 잡는다
            if (_playerHp == null)
            {
                Player p = GameEvents.CurrentPlayer;
                _playerHp = p != null ? p.GetComponent<Damageable>() : null;
            }
            if (_playerHp != null)
                _hpFill.fillAmount = _playerHp.MaxHp > 0f ? _playerHp.Hp / _playerHp.MaxHp : 0f;
        }

        // ---------- UI 구성 ----------

        private void Build()
        {
            _root = new GameObject("HudCanvas");
            _root.transform.SetParent(transform, false);

            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // [사고] 로비만 PC 로 고치고 인게임 HUD 는 세로(1080x1920) 기준으로 남아 있었다.
            // 같은 화면에 두 기준이 섞이면 어느 쪽이 맞는지 볼 때마다 헷갈린다.
            scaler.referenceResolution = new Vector2(1280f, 720f);
            // [사고] 기준 해상도만 바꾸고 고정 좌표를 그대로 뒀더니, 세로 1920 기준으로 잡힌 값들이
            // 720 화면에서는 화면 밖으로 나갔다. 배너가 y=+420 이라 **레벨업 연출이 아예 안 보였다**.
            // 「연출이 없다」가 아니라 「화면 밖에 있다」였다 — 기준을 바꾸면 좌표도 함께 환산한다.
            scaler.matchWidthOrHeight = 0.5f;

            _root.AddComponent<GraphicRaycaster>();

            RectTransform safe = MakeRect("SafeArea", _root.transform);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();

            BuildTopBar(safe);
            BuildPartsStrip(safe);
            BuildResultPanel(safe);
            BuildBanner(safe);

            _root.SetActive(false);
        }

        private void BuildTopBar(RectTransform parent)
        {
            RectTransform bar = MakeRect("TopBar", parent);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0f, 62f);
            bar.anchoredPosition = Vector2.zero;

            _timeText = MakeText(bar, "Time", "00:00", 26f, TextAlignmentOptions.Center);
            RectTransform timeRt = _timeText.rectTransform;
            timeRt.anchorMin = new Vector2(0.5f, 1f);
            timeRt.anchorMax = new Vector2(0.5f, 1f);
            timeRt.pivot = new Vector2(0.5f, 1f);
            timeRt.sizeDelta = new Vector2(220f, 40f);
            timeRt.anchoredPosition = new Vector2(0f, -6f);

            _killText = MakeText(bar, "Kills", "0", 17f, TextAlignmentOptions.Right);
            RectTransform killRt = _killText.rectTransform;
            killRt.anchorMin = new Vector2(1f, 1f);
            killRt.anchorMax = new Vector2(1f, 1f);
            killRt.pivot = new Vector2(1f, 1f);
            killRt.sizeDelta = new Vector2(180f, 34f);
            killRt.anchoredPosition = new Vector2(-18f, -10f);

            RectTransform hpBg = MakeRect("HpBar", bar);
            hpBg.anchorMin = new Vector2(0.5f, 1f);
            hpBg.anchorMax = new Vector2(0.5f, 1f);
            hpBg.pivot = new Vector2(0.5f, 1f);
            hpBg.sizeDelta = new Vector2(420f, 16f);
            hpBg.anchoredPosition = new Vector2(0f, -44f);
            Image bg = hpBg.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);
            Jinhyeong_UI.Framework.UISkin.ApplySliced(bg, Jinhyeong_UI.Framework.UISkin.BarBg, BarCorner);
            bg.raycastTarget = false;

            // fillAmount 를 쓰려면 type=Filled 인데, Filled 는 스프라이트가 있어야 동작한다.
            // 에셋을 만들기 싫으니 흰 사각 스프라이트를 절차 생성해 쓴다.
            RectTransform hpFillRt = MakeRect("Fill", hpBg);
            Stretch(hpFillRt);
            _hpFill = hpFillRt.gameObject.AddComponent<Image>();
            _hpFill.color = new Color(0.35f, 0.9f, 0.35f, 1f);
            _hpFill.raycastTarget = false;
            Jinhyeong_UI.Framework.UISkin.ApplySliced(_hpFill, Jinhyeong_UI.Framework.UISkin.BarBg, BarCorner);
            _hpFill.type = Image.Type.Filled;
            _hpFill.fillMethod = Image.FillMethod.Horizontal;
            _hpFill.fillAmount = 1f;

            _levelText = MakeText(bar, "Level", "Lv.1", 17f, TextAlignmentOptions.Left);
            RectTransform lvRt = _levelText.rectTransform;
            lvRt.anchorMin = new Vector2(0f, 1f);
            lvRt.anchorMax = new Vector2(0f, 1f);
            lvRt.pivot = new Vector2(0f, 1f);
            lvRt.sizeDelta = new Vector2(150f, 34f);
            lvRt.anchoredPosition = new Vector2(18f, -10f);

            RectTransform xpBg = MakeRect("XpBar", bar);
            xpBg.anchorMin = new Vector2(0.5f, 1f);
            xpBg.anchorMax = new Vector2(0.5f, 1f);
            xpBg.pivot = new Vector2(0.5f, 1f);
            xpBg.sizeDelta = new Vector2(420f, 9f);
            xpBg.anchoredPosition = new Vector2(0f, -62f);
            Image xpBgImage = xpBg.gameObject.AddComponent<Image>();
            xpBgImage.color = new Color(0f, 0f, 0f, 0.55f);
            Jinhyeong_UI.Framework.UISkin.ApplySliced(xpBgImage, Jinhyeong_UI.Framework.UISkin.BarBg, BarCorner);
            xpBgImage.raycastTarget = false;

            RectTransform xpFillRt = MakeRect("Fill", xpBg);
            Stretch(xpFillRt);
            _xpFill = xpFillRt.gameObject.AddComponent<Image>();
            _xpFill.color = new Color(0.4f, 0.7f, 1f, 1f);
            _xpFill.raycastTarget = false;
            Jinhyeong_UI.Framework.UISkin.ApplySliced(_xpFill, Jinhyeong_UI.Framework.UISkin.BarBg, BarCorner);
            _xpFill.type = Image.Type.Filled;
            _xpFill.fillMethod = Image.FillMethod.Horizontal;
            _xpFill.fillAmount = 0f;
        }

        /// <summary>
        /// 착용 중인 파츠 6부위를 화면 하단에 한 줄로 보여준다.
        ///
        /// 3택1 패널을 없앤 자리다. 성장은 파츠로만 일어나므로 「지금 뭘 끼고 있나」가
        /// 상시 보여야 한다 — 이게 안 보이면 무엇이 세졌는지 알 수 없다.
        /// </summary>
        private void BuildPartsStrip(RectTransform parent)
        {
            RectTransform strip = MakeRect("PartsStrip", parent);
            strip.anchorMin = new Vector2(0f, 0f);
            strip.anchorMax = new Vector2(1f, 0f);
            strip.pivot = new Vector2(0.5f, 0f);
            strip.sizeDelta = new Vector2(0f, 40f);
            strip.anchoredPosition = new Vector2(0f, 6f);

            Image bg = strip.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.35f);
            Jinhyeong_UI.Framework.UISkin.ApplySliced(bg, Jinhyeong_UI.Framework.UISkin.CardBg, Jinhyeong_UI.Framework.UIBuild.CardCorner);
            bg.raycastTarget = false;

            TMP_Text t = MakeText(strip, "Parts", "", 14f, TextAlignmentOptions.Center);
            t.color = new Color(0.92f, 0.95f, 1f, 1f);
            Stretch(t.rectTransform);
            _partsText = t;
        }

        /// <summary>레벨업은 선택지 없이 스탯만 오른다. 배너로 짧게 알리기만 한다.</summary>
        private void HandleLevelUp(int level)
        {
            // [사고] 「LEVEL 5」만 띄웠다. 성장 축을 파츠로 몰았어도 레벨은 체력·공격력을 올리는데,
            // 무엇이 올랐는지 안 알려주면 「그래서 뭐」로 지나간다.
            // 파츠는 화면에 보이지만 레벨 보상은 안 보이므로, 글로라도 말해 준다.
            Jinhyeong_Managers.SfxManager.Play(Jinhyeong_Managers.ESfx.LevelUp);

            // 성장은 좋은 일이라 경고와 색을 가른다. 같은 색이면 둘 다 안 읽힌다.
            ShowBanner($"레벨 {level}  ·  체력 · 공격력 상승", 1.4f, new Color(1f, 0.86f, 0.42f, 1f));
        }

        /// <summary>파츠 스트립 갱신. 매 프레임 문자열을 새로 만들지 않도록 변경 이벤트로만 돈다.</summary>
        private void RefreshParts(PartInstance inst)
        {
            if (_partsText == null)
                return;

            Player p = GameEvents.CurrentPlayer;
            PartLoadout loadout = p != null ? p.GetComponent<PartLoadout>() : null;
            if (loadout == null)
                return;

            System.Text.StringBuilder sb = new System.Text.StringBuilder(96);
            System.Collections.Generic.IReadOnlyList<PartInstance> eq = loadout.Equipped;
            for (int i = 0; i < PartLoadout.SlotCount; i++)
            {
                if (sb.Length > 0)
                    sb.Append("   ");
                sb.Append(i < eq.Count ? eq[i].DisplayName : "—");
            }
            _partsText.text = sb.ToString();
        }

        /// <summary>사망 결과. 생존시간·킬·레벨을 보여주고 재시작한다.</summary>
        private void BuildResultPanel(RectTransform parent)
        {
            RectTransform panel = MakeRect("ResultPanel", parent);
            Stretch(panel);
            _resultPanel = panel.gameObject;

            Image dim = _resultPanel.AddComponent<Image>();
            // [사고] 스프라이트를 안 넣으면 색만 지정해도 아무것도 안 그려져,
            // 결과 화면 뒤로 게임이 그대로 비쳤다. 이 코드베이스의 다른 Image 들처럼 흰 판을 깐다.
            dim.sprite = WhiteSprite();
            // [실측] 0.8 로도 화면이 충분히 안 어두워져 결과 문구가 배경과 싸웠다.
            // 결과 화면은 「판이 끝났다」를 말하는 자리다. 게임 화면이 계속 읽히면 그 말이 약해진다.
            dim.color = new Color(0f, 0f, 0f, 0.92f);
            dim.raycastTarget = true;

            TMP_Text title = MakeText(panel, "Title", "GAME OVER", 14f, TextAlignmentOptions.Center);
            title.color = new Color(1f, 0.4f, 0.4f, 1f);
            _resultTitle = title;
            RectTransform titleRt = title.rectTransform;
            // 전부 화면 중앙 기준으로 잡는다 — 상단 기준이면 가로/세로에 따라 수치와 겹친다
            titleRt.anchorMin = new Vector2(0.5f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(520f, 66f);
            titleRt.anchoredPosition = new Vector2(0f, 130f);

            _resultText = MakeText(panel, "Stats", "", 18f, TextAlignmentOptions.Center);
            RectTransform statsRt = _resultText.rectTransform;
            statsRt.anchorMin = new Vector2(0.5f, 0.5f);
            statsRt.anchorMax = new Vector2(0.5f, 0.5f);
            statsRt.sizeDelta = new Vector2(520f, 150f);
            statsRt.anchoredPosition = new Vector2(0f, 20f);

            RectTransform btn = MakeRect("Restart", panel);
            btn.anchorMin = new Vector2(0.5f, 0.5f);
            btn.anchorMax = new Vector2(0.5f, 0.5f);
            btn.sizeDelta = new Vector2(320f, 68f);
            btn.anchoredPosition = new Vector2(0f, -92f);
            Image btnBg = btn.gameObject.AddComponent<Image>();
            btnBg.color = new Color(0.2f, 0.7f, 0.35f, 1f);
            Jinhyeong_UI.Framework.UISkin.ApplySliced(btnBg, Jinhyeong_UI.Framework.UISkin.CardBg, Jinhyeong_UI.Framework.UIBuild.CardCorner);
            Button button = btn.gameObject.AddComponent<Button>();
            button.targetGraphic = btnBg;
            button.onClick.AddListener(Restart);

            TMP_Text btnText = MakeText(btn, "Label", "다시 하기", 20f, TextAlignmentOptions.Center);
            Stretch(btnText.rectTransform);

            _resultPanel.SetActive(false);
        }

        private void ShowResult()
        {
            if (_hpFill != null)
                _hpFill.fillAmount = 0f;

            // 클리어와 사망은 같은 패널을 쓰고 제목만 갈린다. 12분을 버텨 낸 판은
            // 실패와 같은 화면으로 끝나면 안 된다.
            // [진단] 딤이 「안 그려지는지」와 「작게 그려지는지」를 가른다.
            // 이걸 안 찍으면 스크린샷을 눈으로 비교하다 원인을 못 좁힌다.
            Image dimNow = _resultPanel != null ? _resultPanel.GetComponent<Image>() : null;
            if (dimNow != null)
            {
                Rect r = dimNow.rectTransform.rect;
                DimNote = $"enabled={dimNow.enabled} sprite={(dimNow.sprite != null ? "있음" : "없음")} " +
                          $"alpha={dimNow.color.a:0.00} rect={r.width:0}x{r.height:0}";
            }
            else
            {
                DimNote = "Image 없음";
            }

            // 라운드 배너가 결과 문구와 겹쳐 「ROUND 3 / GAME OVER」가 한 덩이로 읽혔다.
            // 판이 끝났으면 진행 중 알림은 내린다.
            if (_banner != null)
                _banner.gameObject.SetActive(false);

            bool cleared = Jinhyeong_UI.GameFlow.IsCleared;
            if (_resultTitle != null)
            {
                _resultTitle.text = cleared ? "CLEAR!" : "GAME OVER";
                _resultTitle.color = cleared
                    ? new Color(1f, 0.85f, 0.3f, 1f)
                    : new Color(1f, 0.4f, 0.4f, 1f);
            }

            WaveDirector wave = WaveDirector.Instance;
            EnemySpawner spawner = EnemySpawner.Instance;
            RunProgress progress = RunProgress.Instance;

            int total = wave != null ? Mathf.FloorToInt(wave.Elapsed) : 0;
            _resultText.text = string.Format(
                "생존 시간  {0:00}:{1:00}\n처치  {2}\n레벨  {3}",
                total / 60, total % 60,
                spawner != null ? spawner.KillCount : 0,
                progress != null ? progress.Level : 1);

            if (cleared)
                _resultText.text = "납치선 격추!" + System.Environment.NewLine + _resultText.text;

            _resultPanel.SetActive(true);
        }

        private void BuildBanner(RectTransform parent)
        {
            _banner = MakeText(parent, "Banner", "", 24f, TextAlignmentOptions.Center);
            _banner.color = new Color(1f, 0.35f, 0.3f, 1f);
            _banner.fontStyle = FontStyles.Bold;
            RectTransform rt = _banner.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(700f, 64f);
            rt.anchoredPosition = new Vector2(0f, 150f);

            // Image 가 TMP 와 같은 오브젝트에 붙으면 TMP 가 안 그려지므로 배경은 자식으로
            RectTransform bgRt = MakeRect("Bg", rt);
            Stretch(bgRt);
            bgRt.SetAsFirstSibling();
            Image bgImage = bgRt.gameObject.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.6f);
            Jinhyeong_UI.Framework.UISkin.ApplySliced(bgImage, Jinhyeong_UI.Framework.UISkin.CardBg, Jinhyeong_UI.Framework.UIBuild.CardCorner);
            bgImage.raycastTarget = false;

            _banner.gameObject.SetActive(false);
        }

        private void ShowBanner(string text)
        {
            // 라운드 배너는 경고다 — 붉게.
            ShowBanner(text, 2.5f, new Color(1f, 0.35f, 0.3f, 1f));
        }

        private void ShowBanner(string text, float seconds)
        {
            ShowBanner(text, seconds, new Color(1f, 0.35f, 0.3f, 1f));
        }

        private void ShowBanner(string text, float seconds, Color color)
        {
            if (_banner != null)
                _banner.color = color;

            _banner.text = text;
            _banner.gameObject.SetActive(true);
            _bannerUntil = Time.unscaledTime + seconds;
        }

        private void Restart()
        {
            _resultPanel.SetActive(false);
            if (WaveDirector.Instance != null)
                WaveDirector.Instance.OnBanner -= ShowBanner;
            _bannerHooked = false;
            // 정적 이벤트는 씬 리로드를 넘어 살아남는다 — 빼지 않으면 재시작마다 구독이 쌓인다
            GameFlow.OnGameEnded -= ShowResult;
            if (RunProgress.Instance != null)
                RunProgress.Instance.OnLevelUp -= HandleLevelUp;
            _endHooked = false;
            _levelUpHooked = false;
            _playerHp = null;
            Time.timeScale = 1f;

            if (GameInitializer.Instance != null)
                GameInitializer.Instance.Restart();
        }

        // ---------- 헬퍼 ----------

        private static RectTransform MakeRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TMP_Text MakeText(Transform parent, string name, string content, float size, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.alignment = align;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }


        private static Sprite WhiteSprite()
        {
            if (_whiteSprite != null)
                return _whiteSprite;

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();

            // [사고] 씬을 다시 로드하면 런타임 생성 텍스처가 정리된다. 그러면 스프라이트 참조는
            // 살아 있는데 **텍스처가 없어 아무것도 안 그려진다** — 결과 화면의 어둡게가
            // 안 먹던 증상이 이것이다. 정적 캐시는 씬을 넘어 사니 수명도 씬 밖으로 빼야 한다.
            tex.hideFlags = HideFlags.HideAndDontSave;

            _whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
            _whiteSprite.hideFlags = HideFlags.HideAndDontSave;
            return _whiteSprite;
        }
    }
}
