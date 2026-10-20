using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Jinhyeong_AI;
using Jinhyeong_Character;
using Jinhyeong_GameData;
using Jinhyeong_Common;
using Jinhyeong_Input;
using Jinhyeong_Managers;
using Jinhyeong_Parts;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_UI
{

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
        private TMP_Text _hpText;
        private int _hpShown = -1;
        private int _hpMaxShown = -1;
        private Image _xpFill;

        private TMP_Text _partsText;
        private bool _levelUpHooked;
        private bool _partsHooked;

        private TMP_Text _banner;
        private GameObject _bannerRoot;
        private float _bannerUntil;
        private bool _bannerHooked;

        private GameObject _resultPanel;
        private TMP_Text _resultText;
        private TMP_Text _resultTitle;
        private GameObject _pausePanel;
        private bool _tipShown;

        // 첫 몇 판은 조작을 알려 준다 (판 기록 3 미만)
        private const int TipRuns = 3;
        private static string TipText { get { return Loc.T("Hud_Tip", "WASD · 방향키로 이동  ·  공격은 자동  ·  보물상자를 주워 무기를 늘리세요"); } }

        public bool PauseOpen { get { return _pausePanel != null && _pausePanel.activeSelf; } }

        public static string DimNote { get; private set; } = string.Empty;
        private bool _endHooked;

        private Damageable _playerHp;

        private static Sprite _whiteSprite;

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
            {
                if (LevelUpOpen)
                    _levelPanel.SetActive(false);
                _pendingLevelUps = 0;
                return;
            }

            ReadPauseKey();
            if (PauseOpen)
                return;
            ReadLevelUpKeys();

            WaveDirector wave = WaveDirector.Instance;
            if (wave != null)
            {
                if (_bannerHooked == false)
                {
                    wave.OnBanner += ShowBanner;
                    _bannerHooked = true;
                    if (_tipShown == false && SaveData.PlayCount < TipRuns)
                    {
                        _tipShown = true;
                        ShowBanner(TipText, 6f, Jinhyeong_UI.Framework.UIBuild.ColorGold);
                    }
                }
                if (_bannerUntil > Time.unscaledTime)
                {
                    if (LevelUpOpen)
                    {
                        _bannerUntil += Time.unscaledDeltaTime;
                        if (_bannerRoot.activeSelf)
                            _bannerRoot.SetActive(false);
                    }
                    else if (_bannerRoot.activeSelf == false)
                    {
                        _bannerRoot.SetActive(true);
                    }
                }
                else if (_bannerRoot.activeSelf)
                {
                    _bannerRoot.SetActive(false);
                }

                int total = Mathf.FloorToInt(wave.Elapsed);
                _timeText.text = string.Format("{0:00}:{1:00}", total / 60, total % 60);
            }

            RefreshBossBar();
            RefreshChestPointer();

            EnemySpawner spawner = EnemySpawner.Instance;
            if (spawner != null)
                _killText.text = Loc.F("Hud_Kills", "처치 {0}", spawner.KillCount);

            RunProgress progress = RunProgress.Instance;
            if (progress != null)
            {
                if (_levelUpHooked == false)
                {
                    progress.OnLevelUp += HandleLevelUp;
                    _levelUpHooked = true;
                }
                _levelText.text = "Lv." + progress.Level;
                _xpFill.fillAmount = progress.XpToNext > 0f ? Mathf.Clamp01(progress.Xp / progress.XpToNext) : 0f;
            }

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

            if (_playerHp == null)
            {
                Player p = GameEvents.CurrentPlayer;
                _playerHp = p != null ? p.GetComponent<Damageable>() : null;
            }
            if (_playerHp != null)
            {
                _hpFill.fillAmount = _playerHp.MaxHp > 0f ? _playerHp.Hp / _playerHp.MaxHp : 0f;
                // 바뀔 때만 글자를 다시 쓴다 (매 프레임 문자열 할당 안 함)
                int hp = Mathf.CeilToInt(Mathf.Max(0f, _playerHp.Hp));
                int max = Mathf.CeilToInt(_playerHp.MaxHp);
                if (_hpText != null && (hp != _hpShown || max != _hpMaxShown))
                {
                    _hpShown = hp;
                    _hpMaxShown = max;
                    _hpText.text = hp + " / " + max;
                }
            }
        }

        private void Build()
        {
            _root = new GameObject("HudCanvas");
            _root.transform.SetParent(transform, false);

            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            _root.AddComponent<GraphicRaycaster>();

            RectTransform safe = MakeRect("SafeArea", _root.transform);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();

            BuildTopBar(safe);
            BuildPartsStrip(safe);
            BuildResultPanel(safe);
            BuildBanner(safe);
            BuildBossBar(safe);
            BuildChestPointer(safe);
            BuildLevelUpPanel(safe);
            BuildPausePanel(safe);

            _root.SetActive(false);
        }

        // 아트 리부트 (볼트 개많이잡아/개많이잡아 아트 방향 UI 방향) — XP 는 화면 위 가로 전체, HP 는 하단 중앙 하트 바
        private static readonly Color HpColor = new Color(0.88f, 0.24f, 0.24f, 1f);
        private static readonly Color XpColor = new Color(0.40f, 0.80f, 1f, 1f);
        private static readonly Color BarBack = new Color(0.06f, 0.06f, 0.10f, 0.88f);
        private static readonly Color TextInk = new Color(0.10f, 0.08f, 0.12f, 1f);

        private static void Ink(TMP_Text t, float width)
        {
            t.fontStyle = FontStyles.Normal;
            t.outlineWidth = width;
            t.outlineColor = TextInk;
        }

        private static void MakeBar(RectTransform rt, Color fill, out Image fillImage)
        {
            Image bg = rt.gameObject.AddComponent<Image>();
            bg.color = BarBack;
            Jinhyeong_UI.Framework.UISkin.ApplySliced(bg, Jinhyeong_UI.Framework.UISkin.BarBg, BarCorner);
            bg.raycastTarget = false;

            RectTransform fillRt = MakeRect("Fill", rt);
            Stretch(fillRt);
            fillRt.offsetMin = new Vector2(3f, 3f);
            fillRt.offsetMax = new Vector2(-3f, -3f);
            fillImage = fillRt.gameObject.AddComponent<Image>();
            fillImage.color = fill;
            fillImage.raycastTarget = false;
            Jinhyeong_UI.Framework.UISkin.ApplySliced(fillImage, Jinhyeong_UI.Framework.UISkin.BarBg, BarCorner);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;

            Jinhyeong_UI.Framework.UIBuild.Outline(rt, Jinhyeong_UI.Framework.UIBuild.ColorOutline,
                Jinhyeong_UI.Framework.UISkin.CardFrame, BarCorner);
        }

        private void BuildTopBar(RectTransform parent)
        {
            RectTransform bar = MakeRect("TopBar", parent);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0f, 70f);
            bar.anchoredPosition = Vector2.zero;

            RectTransform xpBg = MakeRect("XpBar", bar);
            xpBg.anchorMin = new Vector2(0f, 1f);
            xpBg.anchorMax = new Vector2(1f, 1f);
            xpBg.pivot = new Vector2(0.5f, 1f);
            xpBg.sizeDelta = new Vector2(-36f, 16f);
            xpBg.anchoredPosition = new Vector2(0f, -8f);
            MakeBar(xpBg, XpColor, out _xpFill);
            _xpFill.fillAmount = 0f;

            _timeText = MakeText(bar, "Time", "00:00", 34f, TextAlignmentOptions.Center);
            Ink(_timeText, 0.22f);
            RectTransform timeRt = _timeText.rectTransform;
            timeRt.anchorMin = new Vector2(0.5f, 1f);
            timeRt.anchorMax = new Vector2(0.5f, 1f);
            timeRt.pivot = new Vector2(0.5f, 1f);
            timeRt.sizeDelta = new Vector2(220f, 44f);
            timeRt.anchoredPosition = new Vector2(0f, -26f);

            _killText = MakeText(bar, "Kills", "0", 22f, TextAlignmentOptions.Right);
            Ink(_killText, 0.22f);
            _killText.color = Jinhyeong_UI.Framework.UIBuild.ColorGold;
            RectTransform killRt = _killText.rectTransform;
            killRt.anchorMin = new Vector2(1f, 1f);
            killRt.anchorMax = new Vector2(1f, 1f);
            killRt.pivot = new Vector2(1f, 1f);
            killRt.sizeDelta = new Vector2(220f, 36f);
            killRt.anchoredPosition = new Vector2(-18f, -28f);

            _levelText = MakeText(bar, "Level", "Lv.1", 22f, TextAlignmentOptions.Left);
            Ink(_levelText, 0.22f);
            _levelText.color = Color.white;
            RectTransform lvRt = _levelText.rectTransform;
            lvRt.anchorMin = new Vector2(0f, 1f);
            lvRt.anchorMax = new Vector2(0f, 1f);
            lvRt.pivot = new Vector2(0f, 1f);
            lvRt.sizeDelta = new Vector2(180f, 36f);
            lvRt.anchoredPosition = new Vector2(18f, -28f);

            // HP 바는 하단 중앙. 파츠 줄 바로 위
            RectTransform hpBg = MakeRect("HpBar", parent);
            hpBg.anchorMin = new Vector2(0.5f, 0f);
            hpBg.anchorMax = new Vector2(0.5f, 0f);
            hpBg.pivot = new Vector2(0.5f, 0f);
            hpBg.sizeDelta = new Vector2(440f, 24f);
            hpBg.anchoredPosition = new Vector2(14f, 58f);
            MakeBar(hpBg, HpColor, out _hpFill);
            _hpFill.fillAmount = 1f;
            _hpText = MakeText(hpBg, "HpText", "", 15f, TextAlignmentOptions.Center);
            Ink(_hpText, 0.25f);
            Stretch(_hpText.rectTransform);

            RectTransform heartRt = MakeRect("Heart", hpBg);
            heartRt.anchorMin = new Vector2(0f, 0.5f);
            heartRt.anchorMax = new Vector2(0f, 0.5f);
            heartRt.pivot = new Vector2(0.5f, 0.5f);
            heartRt.sizeDelta = new Vector2(44f, 44f);
            heartRt.anchoredPosition = new Vector2(-6f, 2f);
            Image heartInk = heartRt.gameObject.AddComponent<Image>();
            heartInk.color = TextInk;
            heartInk.raycastTarget = false;
            Jinhyeong_UI.Framework.UISkin.ApplySimple(heartInk, Jinhyeong_UI.Framework.UISkin.Heart);

            RectTransform heartFillRt = MakeRect("Fill", heartRt);
            Stretch(heartFillRt);
            heartFillRt.offsetMin = new Vector2(5f, 5f);
            heartFillRt.offsetMax = new Vector2(-5f, -5f);
            Image heart = heartFillRt.gameObject.AddComponent<Image>();
            heart.color = HpColor;
            heart.raycastTarget = false;
            Jinhyeong_UI.Framework.UISkin.ApplySimple(heart, Jinhyeong_UI.Framework.UISkin.Heart);
        }

        private void BuildPartsStrip(RectTransform parent)
        {
            RectTransform strip = MakeRect("PartsStrip", parent);
            strip.anchorMin = new Vector2(0f, 0f);
            strip.anchorMax = new Vector2(1f, 0f);
            strip.pivot = new Vector2(0.5f, 0f);
            strip.sizeDelta = new Vector2(-36f, 40f);
            strip.anchoredPosition = new Vector2(0f, 8f);

            Image bg = strip.gameObject.AddComponent<Image>();
            bg.color = BarBack;
            Jinhyeong_UI.Framework.UISkin.ApplySliced(bg, Jinhyeong_UI.Framework.UISkin.CardBg, Jinhyeong_UI.Framework.UIBuild.CardCorner);
            bg.raycastTarget = false;

            const float slotW = 190f;
            for (int i = 0; i < PartLoadout.SlotCount; i++)
            {
                RectTransform slot = MakeRect("Slot" + i, strip);
                slot.anchorMin = slot.anchorMax = new Vector2(0.5f, 0.5f);
                slot.sizeDelta = new Vector2(slotW - 8f, 34f);
                slot.anchoredPosition = new Vector2((i - (PartLoadout.SlotCount - 1) * 0.5f) * slotW, 0f);

                RectTransform ic = MakeRect("Icon", slot);
                ic.anchorMin = ic.anchorMax = new Vector2(0f, 0.5f);
                ic.pivot = new Vector2(0f, 0.5f);
                ic.sizeDelta = new Vector2(30f, 30f);
                ic.anchoredPosition = new Vector2(2f, 0f);
                _slotIcon[i] = ic.gameObject.AddComponent<Image>();
                _slotIcon[i].raycastTarget = false;
                _slotIcon[i].preserveAspect = true;

                TMP_Text name = MakeText(slot, "Name", "", 15f, TextAlignmentOptions.Left);
                name.enableAutoSizing = true;
                name.fontSizeMin = 11f;
                name.fontSizeMax = 15f;
                name.enableWordWrapping = false;
                RectTransform nr = name.rectTransform;
                nr.anchorMin = new Vector2(0f, 0f);
                nr.anchorMax = new Vector2(1f, 1f);
                nr.offsetMin = new Vector2(38f, 0f);
                nr.offsetMax = new Vector2(-2f, 0f);
                _slotName[i] = name;
            }
            _partsText = _slotName[0];
        }

        private readonly Image[] _slotIcon = new Image[PartLoadout.SlotCount];
        private readonly TMP_Text[] _slotName = new TMP_Text[PartLoadout.SlotCount];
        private static readonly Color SlotEmpty = new Color(1f, 1f, 1f, 0.18f);

        // ── 레벨업 3택1 ─────────────────────────────────────────────────
        // 레벨업하면 멈추고 카드 3장 중 하나를 고르세요 (클릭 · 1/2/3). 한 번에 여러 레벨이 오르면 차례로 연다
        private GameObject _levelPanel;
        private TMP_Text _levelTitle;
        private readonly Image[] _cardFrame = new Image[Jinhyeong_Parts.LevelUpDraft.CardCount];
        private readonly Image[] _cardIcon = new Image[Jinhyeong_Parts.LevelUpDraft.CardCount];
        private readonly TMP_Text[] _cardKind = new TMP_Text[Jinhyeong_Parts.LevelUpDraft.CardCount];
        private readonly TMP_Text[] _cardName = new TMP_Text[Jinhyeong_Parts.LevelUpDraft.CardCount];
        private readonly TMP_Text[] _cardDesc = new TMP_Text[Jinhyeong_Parts.LevelUpDraft.CardCount];
        private readonly GameObject[] _cardRoot = new GameObject[Jinhyeong_Parts.LevelUpDraft.CardCount];
        private System.Collections.Generic.List<Jinhyeong_Parts.LevelUpCard> _cards;
        private int _pendingLevelUps;
        private TMP_Text _levelHint;
        private int _autoDamagePicks;
        private int _autoHpPicks;

        public bool LevelUpOpen { get { return _levelPanel != null && _levelPanel.activeSelf; } }

        private void BuildLevelUpPanel(RectTransform parent)
        {
            RectTransform panel = MakeRect("LevelUpPanel", parent);
            Stretch(panel);
            _levelPanel = panel.gameObject;
            Image dim = _levelPanel.AddComponent<Image>();
            dim.sprite = WhiteSprite();
            dim.color = new Color(0.03f, 0.03f, 0.06f, 0.78f);
            dim.raycastTarget = true;

            _levelTitle = MakeText(panel, "Title", "LEVEL UP!", 46f, TextAlignmentOptions.Center);
            Ink(_levelTitle, 0.22f);
            _levelTitle.color = Jinhyeong_UI.Framework.UIBuild.ColorGold;
            RectTransform tr = _levelTitle.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.sizeDelta = new Vector2(700f, 60f);
            tr.anchoredPosition = new Vector2(0f, 230f);

            TMP_Text hint = MakeText(panel, "Hint", "", 18f, TextAlignmentOptions.Center);
            _levelHint = hint;
            hint.color = Jinhyeong_UI.Framework.UIBuild.ColorSubHead;
            RectTransform hr = hint.rectTransform;
            hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0.5f);
            hr.sizeDelta = new Vector2(700f, 30f);
            hr.anchoredPosition = new Vector2(0f, 190f);

            for (int i = 0; i < _cardRoot.Length; i++)
            {
                int index = i;
                RectTransform card = MakeRect("Card" + i, panel);
                card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
                card.sizeDelta = new Vector2(290f, 340f);
                card.anchoredPosition = new Vector2((i - 1) * 320f, -10f);
                _cardRoot[i] = card.gameObject;

                Image bg = card.gameObject.AddComponent<Image>();
                bg.color = Jinhyeong_UI.Framework.UIBuild.ColorCard;
                Jinhyeong_UI.Framework.UISkin.ApplySliced(bg, Jinhyeong_UI.Framework.UISkin.PanelBg, Jinhyeong_UI.Framework.UIBuild.BigCardCorner);
                Button button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = bg;
                button.onClick.AddListener(() => PickLevelUp(index));
                _cardFrame[i] = Jinhyeong_UI.Framework.UIBuild.Outline(card, Jinhyeong_UI.Framework.UIBuild.ColorOutline,
                    Jinhyeong_UI.Framework.UISkin.PanelFrame, Jinhyeong_UI.Framework.UIBuild.BigCardCorner);

                TMP_Text key = MakeText(card, "Key", (i + 1).ToString(), 20f, TextAlignmentOptions.TopLeft);
                Ink(key, 0.2f);
                RectTransform kr = key.rectTransform;
                kr.anchorMin = kr.anchorMax = new Vector2(0f, 1f);
                kr.pivot = new Vector2(0f, 1f);
                kr.sizeDelta = new Vector2(40f, 30f);
                kr.anchoredPosition = new Vector2(16f, -12f);

                _cardKind[i] = MakeText(card, "Kind", "", 17f, TextAlignmentOptions.Center);
                Ink(_cardKind[i], 0.15f);
                RectTransform kd = _cardKind[i].rectTransform;
                kd.anchorMin = kd.anchorMax = new Vector2(0.5f, 1f);
                kd.sizeDelta = new Vector2(260f, 28f);
                kd.anchoredPosition = new Vector2(0f, -30f);

                RectTransform iconRt = MakeRect("Icon", card);
                iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 1f);
                iconRt.sizeDelta = new Vector2(110f, 110f);
                iconRt.anchoredPosition = new Vector2(0f, -110f);
                _cardIcon[i] = iconRt.gameObject.AddComponent<Image>();
                _cardIcon[i].raycastTarget = false;
                _cardIcon[i].preserveAspect = true;

                _cardName[i] = MakeText(card, "Name", "", 26f, TextAlignmentOptions.Center);
                _cardName[i].enableAutoSizing = true;
                _cardName[i].enableWordWrapping = false;
                _cardName[i].fontSizeMin = 17f;
                _cardName[i].fontSizeMax = 26f;
                Ink(_cardName[i], 0.2f);
                RectTransform nr = _cardName[i].rectTransform;
                nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 1f);
                nr.sizeDelta = new Vector2(276f, 52f);
                nr.anchoredPosition = new Vector2(0f, -194f);

                _cardDesc[i] = MakeText(card, "Desc", "", 16f, TextAlignmentOptions.Top);
                _cardDesc[i].color = Jinhyeong_UI.Framework.UIBuild.ColorSubHead;
                _cardDesc[i].enableWordWrapping = true;
                _cardDesc[i].enableAutoSizing = true;
                _cardDesc[i].fontSizeMin = 12f;
                _cardDesc[i].fontSizeMax = 16f;
                RectTransform dr = _cardDesc[i].rectTransform;
                dr.anchorMin = dr.anchorMax = new Vector2(0.5f, 1f);
                dr.sizeDelta = new Vector2(250f, 110f);
                dr.anchoredPosition = new Vector2(0f, -276f);
            }
            _levelPanel.SetActive(false);
        }

        private void HandleLevelUp(int level)
        {
            Jinhyeong_Managers.SfxManager.Play(Jinhyeong_Managers.ESfx.LevelUp);
            _pendingLevelUps++;
            if (LevelUpOpen == false)
                OpenNextLevelUp();
        }

        private void OpenNextLevelUp()
        {
            Player p = GameEvents.CurrentPlayer;
            PartLoadout loadout = p != null ? p.GetComponent<PartLoadout>() : null;
            _cards = Jinhyeong_Parts.LevelUpDraft.Build(loadout);
            if (_cards.Count == 0)
            {
                _pendingLevelUps = 0;
                return;
            }

            RunProgress progress = RunProgress.Instance;
            if (_levelHint != null)
                _levelHint.text = Loc.T("Hud_LevelUpHint", "하나를 고르세요  ·  클릭 또는 1 · 2 · 3");
            _levelTitle.text = progress != null ? $"LEVEL {progress.Level - _pendingLevelUps + 1}" : "LEVEL UP!";
            for (int i = 0; i < _cardRoot.Length; i++)
            {
                bool on = i < _cards.Count;
                _cardRoot[i].SetActive(on);
                if (on)
                    FillCard(i, _cards[i]);
            }
            _levelPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        private void FillCard(int i, Jinhyeong_Parts.LevelUpCard c)
        {
            Color accent = Jinhyeong_UI.Framework.UIBuild.ColorOutline;
            Sprite icon = null;
            switch (c.Kind)
            {
                case Jinhyeong_Parts.ELevelUpCardKind.NewWeapon:
                    _cardKind[i].text = Loc.T("Hud_CardNewWeapon", "새 무기");
                    break;
                case Jinhyeong_Parts.ELevelUpCardKind.Upgrade:
                    _cardKind[i].text = Loc.T("Hud_CardUpgrade", "무기 강화");
                    break;
                case Jinhyeong_Parts.ELevelUpCardKind.Amp:
                    _cardKind[i].text = Loc.T("Hud_CardAmp", "고유 공격 강화");
                    break;
                default:
                    _cardKind[i].text = Loc.T("Hud_CardStat", "능력");
                    break;
            }
            if (c.Part != null)
            {
                icon = Jinhyeong_UI.Framework.UISkin.SkillIcon(c.SkillId);
                accent = FamilyAccent(c.Family);
                _cardName[i].text = c.Part.DisplayName;
                System.Text.StringBuilder sb = new System.Text.StringBuilder(64);
                for (int k = 0; k < c.Part.Options.Count; k++)
                {
                    if (sb.Length > 0)
                        sb.Append('\n');
                    // «무기 이름 — 효과» 에서 효과만 — 카드 제목이 이미 무기 이름이다
                    string d = c.Part.Options[k].Description;
                    int cut = d.IndexOf('—');
                    sb.Append(cut >= 0 ? d.Substring(cut + 1).Trim() : d);
                }
                _cardDesc[i].text = c.Kind == Jinhyeong_Parts.ELevelUpCardKind.NewWeapon || sb.Length == 0
                    ? c.Part.Data.Description
                    : sb.ToString();
            }
            else
            {
                icon = Jinhyeong_UI.Framework.UISkin.Get(StatIcon(c.StatTrait));
                if (icon == null)
                    icon = Jinhyeong_UI.Framework.UISkin.Get(Jinhyeong_UI.Framework.UISkin.Heart);
                accent = Jinhyeong_UI.Framework.UIBuild.ColorGold;
                _cardName[i].text = StatName(c.StatTrait);
                _cardDesc[i].text = StatDesc(c.StatTrait, c.StatValue);
            }
            _cardIcon[i].sprite = icon;
            _cardIcon[i].enabled = icon != null;
            _cardIcon[i].color = accent;
            _cardKind[i].color = KindColor(c.Kind);
            _cardFrame[i].color = accent;
        }

        private static Color FamilyAccent(ESkillFamily f)
        {
            switch (f)
            {
                case ESkillFamily.Wave: return new Color(0.18f, 0.90f, 1f, 1f);
                case ESkillFamily.Throw: return new Color(0.61f, 1f, 0.24f, 1f);
                case ESkillFamily.Field: return new Color(0.65f, 0.42f, 1f, 1f);
                case ESkillFamily.Melee: return new Color(1f, 0.24f, 0.66f, 1f);
                case ESkillFamily.Gaze: return new Color(0.30f, 0.55f, 1f, 1f);
                case ESkillFamily.Aura: return new Color(1f, 0.69f, 0.23f, 1f);
                default: return Jinhyeong_UI.Framework.UIBuild.ColorOutline;
            }
        }

        // 맵 보스 이름 (Tables/map). 맵이 없으면 일반 문구
        private static string BossClearTitle()
        {
            Jinhyeong_SkillSystem.MapData map = Jinhyeong_SkillSystem.MapRegistry.Current;
            return map != null && string.IsNullOrEmpty(map.Boss) == false ? Loc.F("Hud_BossDefeated", "{0} 격파!", map.Boss) : Loc.T("Hud_BossDefeatedDefault", "보스 격파!");
        }

        private static readonly Color KindNew = new Color(1f, 0.82f, 0.30f, 1f);
        private static readonly Color KindUpgrade = new Color(0.55f, 0.85f, 1f, 1f);
        private static readonly Color KindAmp = new Color(1f, 0.55f, 0.75f, 1f);
        private static readonly Color KindStat = new Color(0.80f, 0.95f, 0.70f, 1f);

        private static Color KindColor(Jinhyeong_Parts.ELevelUpCardKind k)
        {
            switch (k)
            {
                case Jinhyeong_Parts.ELevelUpCardKind.NewWeapon: return KindNew;
                case Jinhyeong_Parts.ELevelUpCardKind.Upgrade: return KindUpgrade;
                case Jinhyeong_Parts.ELevelUpCardKind.Amp: return KindAmp;
                default: return KindStat;
            }
        }

        private static string StatIcon(EPartTrait t)
        {
            switch (t)
            {
                case EPartTrait.Damage: return "Icons/stat_damage";
                case EPartTrait.MaxHp: return "Icons/stat_maxhp";
                case EPartTrait.MoveSpeed: return "Icons/stat_movespeed";
                default: return "Icons/stat_healonkill";
            }
        }

        private static string StatName(EPartTrait t)
        {
            switch (t)
            {
                case EPartTrait.Damage: return Loc.T("Hud_StatDamageName", "날 벼리기");
                case EPartTrait.MaxHp: return Loc.T("Hud_StatMaxHpName", "강철 갑옷");
                case EPartTrait.MoveSpeed: return Loc.T("Hud_StatMoveSpeedName", "바람 발걸음");
                default: return Loc.T("Hud_StatHealOnKillName", "전장의 함성");
            }
        }

        private static string StatDesc(EPartTrait t, float v)
        {
            switch (t)
            {
                case EPartTrait.Damage: return Loc.F("Hud_StatDamageDesc", "공격력 +{0}%", Mathf.RoundToInt(v * 100f));
                case EPartTrait.MaxHp: return Loc.F("Hud_StatMaxHpDesc", "최대 체력 +{0}%", Mathf.RoundToInt(v * 100f));
                case EPartTrait.MoveSpeed: return Loc.F("Hud_StatMoveSpeedDesc", "이동속도 +{0}%", Mathf.RoundToInt(v * 100f));
                default: return Loc.F("Hud_StatHealOnKillDesc", "처치 시 체력 +{0:0.#}", v);
            }
        }

        public void PickLevelUp(int index)
        {
            if (LevelUpOpen == false || _cards == null || index < 0 || index >= _cards.Count)
                return;

            Player p = GameEvents.CurrentPlayer;
            PartLoadout loadout = p != null ? p.GetComponent<PartLoadout>() : null;
            Jinhyeong_Parts.LevelUpDraft.Apply(_cards[index], loadout, p != null ? p.Skills : null);
            _cards = null;
            _levelPanel.SetActive(false);
            _pendingLevelUps = Mathf.Max(0, _pendingLevelUps - 1);
            if (_pendingLevelUps > 0)
            {
                OpenNextLevelUp();
                return;
            }
            if (GameFlow.IsRunning)
                Time.timeScale = 1f;
        }

        // 하네스 — 정책대로 자동으로 고른다
        public void DebugAutoPick()
        {
            if (LevelUpOpen == false || _cards == null)
                return;
            RunProgress progress = RunProgress.Instance;
            float ratio = _playerHp != null && _playerHp.MaxHp > 0f ? _playerHp.Hp / _playerHp.MaxHp : 1f;
            int pick = Jinhyeong_Parts.LevelUpDraft.AutoPick(_cards, progress != null ? progress.Level : 1, ratio, _autoDamagePicks, _autoHpPicks);
            Jinhyeong_Parts.LevelUpCard c = _cards[pick];
            if (c.Kind == Jinhyeong_Parts.ELevelUpCardKind.Stat && c.StatTrait == EPartTrait.Damage)
                _autoDamagePicks++;
            if (c.Kind == Jinhyeong_Parts.ELevelUpCardKind.Stat && c.StatTrait == EPartTrait.MaxHp)
                _autoHpPicks++;
            PickLevelUp(pick);
        }

        // ESC 로 일시정지 · 계속. 레벨업 3택1 이 열려 있을 때는 열지 않는다
        private void ReadPauseKey()
        {
            if (Input.GetKeyDown(KeyCode.Escape) == false || GameFlow.IsRunning == false || LevelUpOpen)
                return;
            SetPaused(PauseOpen == false);
        }

        private void SetPaused(bool on)
        {
            if (_pausePanel == null)
                return;
            _pausePanel.SetActive(on);
            Time.timeScale = on ? 0f : 1f;
            SfxManager.Play(ESfx.Click);
        }

        private void GiveUp()
        {
            _pausePanel.SetActive(false);
            Time.timeScale = 1f;
            GameFlow.EndGame();
        }

        private void BuildPausePanel(RectTransform parent)
        {
            RectTransform panel = MakeRect("PausePanel", parent);
            Stretch(panel);
            _pausePanel = panel.gameObject;

            Image dim = _pausePanel.AddComponent<Image>();
            dim.sprite = WhiteSprite();
            dim.color = new Color(0.03f, 0.03f, 0.06f, 0.82f);
            dim.raycastTarget = true;

            TMP_Text title = MakeText(panel, "Title", Loc.T("Hud_PauseTitle", "일시정지"), 52f, TextAlignmentOptions.Center);
            Ink(title, 0.25f);
            title.color = Jinhyeong_UI.Framework.UIBuild.ColorGold;
            RectTransform titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0.5f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(520f, 66f);
            titleRt.anchoredPosition = new Vector2(0f, 120f);

            MakePauseButton(panel, "Resume", Loc.T("Hud_PauseResume", "계속하기 (ESC)"), new Vector2(0f, 10f), Jinhyeong_UI.Framework.UIBuild.ColorCardOn, () => SetPaused(false));
            MakePauseButton(panel, "GiveUp", Loc.T("Hud_PauseGiveUp", "포기하고 결과 보기"), new Vector2(0f, -76f), Jinhyeong_UI.Framework.UIBuild.ColorDanger, GiveUp);

            _pausePanel.SetActive(false);
        }

        private void MakePauseButton(RectTransform parent, string name, string label, Vector2 pos, Color color, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform btn = MakeRect(name, parent);
            btn.anchorMin = new Vector2(0.5f, 0.5f);
            btn.anchorMax = new Vector2(0.5f, 0.5f);
            btn.sizeDelta = new Vector2(340f, 66f);
            btn.anchoredPosition = pos;
            Image bg = btn.gameObject.AddComponent<Image>();
            bg.color = color;
            Jinhyeong_UI.Framework.UISkin.ApplySliced(bg, Jinhyeong_UI.Framework.UISkin.CardBg, Jinhyeong_UI.Framework.UIBuild.CardCorner);
            Jinhyeong_UI.Framework.UIBuild.Outline(btn, Jinhyeong_UI.Framework.UIBuild.ColorOutline, Jinhyeong_UI.Framework.UISkin.CardFrame, Jinhyeong_UI.Framework.UIBuild.CardCorner);
            Button button = btn.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(onClick);
            TMP_Text text = MakeText(btn, "Label", label, 22f, TextAlignmentOptions.Center);
            Ink(text, 0.2f);
            Stretch(text.rectTransform);
        }

        private void ReadLevelUpKeys()
        {
            if (LevelUpOpen == false)
                return;
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                PickLevelUp(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                PickLevelUp(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                PickLevelUp(2);
        }

        private void RefreshParts(PartInstance inst)
        {
            if (_partsText == null)
                return;

            Player p = GameEvents.CurrentPlayer;
            PartLoadout loadout = p != null ? p.GetComponent<PartLoadout>() : null;
            if (loadout == null)
                return;

            System.Collections.Generic.IReadOnlyList<PartInstance> eq = loadout.Equipped;
            for (int i = 0; i < PartLoadout.SlotCount; i++)
            {
                bool has = i < eq.Count && eq[i] != null;
                Sprite icon = has ? Jinhyeong_UI.Framework.UISkin.SkillIcon(eq[i].SkillId) : null;
                _slotIcon[i].sprite = icon != null ? icon : Jinhyeong_UI.Framework.UISkin.Get(Jinhyeong_UI.Framework.UISkin.Heart);
                _slotIcon[i].color = has ? FamilyAccent(eq[i].Data.Family) : SlotEmpty;
                _slotName[i].text = has ? eq[i].DisplayName : string.Empty;
                _slotName[i].color = Jinhyeong_UI.Framework.UIBuild.ColorSubHead;
            }
        }

        private void BuildResultPanel(RectTransform parent)
        {
            RectTransform panel = MakeRect("ResultPanel", parent);
            Stretch(panel);
            _resultPanel = panel.gameObject;

            Image dim = _resultPanel.AddComponent<Image>();
            dim.sprite = WhiteSprite();
            dim.color = new Color(0.03f, 0.03f, 0.06f, 0.92f);
            dim.raycastTarget = true;

            TMP_Text title = MakeText(panel, "Title", "GAME OVER", 56f, TextAlignmentOptions.Center);
            Ink(title, 0.25f);
            title.color = Jinhyeong_UI.Framework.UIBuild.ColorDangerArmed;
            _resultTitle = title;
            RectTransform titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0.5f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(520f, 66f);
            titleRt.anchoredPosition = new Vector2(0f, 130f);

            _resultText = MakeText(panel, "Stats", "", 22f, TextAlignmentOptions.Center);
            _resultText.enableAutoSizing = true;
            _resultText.fontSizeMin = 14f;
            _resultText.fontSizeMax = 22f;
            RectTransform statsRt = _resultText.rectTransform;
            statsRt.anchorMin = new Vector2(0.5f, 0.5f);
            statsRt.anchorMax = new Vector2(0.5f, 0.5f);
            statsRt.sizeDelta = new Vector2(620f, 210f);
            statsRt.anchoredPosition = new Vector2(0f, 10f);

            RectTransform btn = MakeRect("Restart", panel);
            btn.anchorMin = new Vector2(0.5f, 0.5f);
            btn.anchorMax = new Vector2(0.5f, 0.5f);
            btn.sizeDelta = new Vector2(320f, 68f);
            btn.anchoredPosition = new Vector2(0f, -150f);
            Image btnBg = btn.gameObject.AddComponent<Image>();
            btnBg.color = Jinhyeong_UI.Framework.UIBuild.ColorStart;
            Jinhyeong_UI.Framework.UISkin.ApplySliced(btnBg, Jinhyeong_UI.Framework.UISkin.CardBg, Jinhyeong_UI.Framework.UIBuild.CardCorner);
            Jinhyeong_UI.Framework.UIBuild.Outline(btn, Color.white, Jinhyeong_UI.Framework.UISkin.CardFrame, Jinhyeong_UI.Framework.UIBuild.CardCorner);
            Button button = btn.gameObject.AddComponent<Button>();
            button.targetGraphic = btnBg;
            button.onClick.AddListener(Restart);

            TMP_Text btnText = MakeText(btn, "Label", Loc.T("Hud_Restart", "다시 하기"), 24f, TextAlignmentOptions.Center);
            Ink(btnText, 0.2f);
            Stretch(btnText.rectTransform);

            _resultPanel.SetActive(false);
        }

        private void ShowResult()
        {
            if (_hpFill != null)
                _hpFill.fillAmount = 0f;

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

            if (_bannerRoot != null)
                _bannerRoot.SetActive(false);
            _bannerUntil = 0f;
            if (LevelUpOpen)
                _levelPanel.SetActive(false);
            _pendingLevelUps = 0;

            bool cleared = Jinhyeong_UI.GameFlow.IsCleared;
            if (_resultTitle != null)
            {
                _resultTitle.text = cleared ? "CLEAR!" : "GAME OVER";
                _resultTitle.color = cleared
                    ? Jinhyeong_UI.Framework.UIBuild.ColorGold
                    : Jinhyeong_UI.Framework.UIBuild.ColorDangerArmed;
            }

            WaveDirector wave = WaveDirector.Instance;
            EnemySpawner spawner = EnemySpawner.Instance;
            RunProgress progress = RunProgress.Instance;

            int total = wave != null ? Mathf.FloorToInt(wave.Elapsed) : 0;
            _resultText.text = Loc.F("Hud_ResultStats",
                "생존 시간  {0:00}:{1:00}\n처치  {2}\n레벨  {3}",
                total / 60, total % 60,
                spawner != null ? spawner.KillCount : 0,
                progress != null ? progress.Level : 1);

            if (cleared)
                _resultText.text = BossClearTitle() + System.Environment.NewLine + _resultText.text;

            _resultText.text += System.Environment.NewLine + System.Environment.NewLine
                + "<color=#FFD45A>" + Loc.F("Hud_ResultReward", "사료 +{0}  ·  슈퍼 DNA +{1}", SaveData.LastKibble, SaveData.LastDna) + "</color>";
            if (SaveData.LastNewMap > 0)
            {
                Jinhyeong_SkillSystem.MapData cur = Jinhyeong_SkillSystem.MapRegistry.Current;
                Jinhyeong_SkillSystem.MapData next = Jinhyeong_SkillSystem.MapRegistry.Get(SaveData.LastNewMap + 1);
                if (next != null)
                    _resultText.text += System.Environment.NewLine + "<color=#8CE0FF>" + Loc.F("Hud_ResultNewMap", "새 맵 열림 — {0}", next.DisplayName) + "</color>";
                if (cur != null && cur.Hero > 0)
                    _resultText.text += System.Environment.NewLine + "<color=#8CE0FF>" + Loc.F("Hud_ResultNewHero", "{0} 합류!", RunSelection.HeroOf(cur.Hero).Name) + "</color>";
            }
            if (spawner != null && _killText != null)
                _killText.text = Loc.F("Hud_Kills", "처치 {0}", spawner.KillCount);

            _resultPanel.SetActive(true);
        }

        private GameObject _bossRoot;
        private Image _bossFill;
        private TMP_Text _bossName;
        private Damageable _bossHp;
        private float _bossScanAt;

        private void BuildBossBar(RectTransform parent)
        {
            RectTransform rt = MakeRect("BossBar", parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(560f, 18f);
            rt.anchoredPosition = new Vector2(0f, -112f);
            MakeBar(rt, HpColor, out _bossFill);
            _bossRoot = rt.gameObject;

            _bossName = MakeText(rt, "Name", "", 20f, TextAlignmentOptions.Center);
            Ink(_bossName, 0.22f);
            _bossName.color = Jinhyeong_UI.Framework.UIBuild.ColorGold;
            RectTransform nr = _bossName.rectTransform;
            nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 1f);
            nr.pivot = new Vector2(0.5f, 0f);
            nr.sizeDelta = new Vector2(560f, 26f);
            nr.anchoredPosition = new Vector2(0f, 2f);
            _bossRoot.SetActive(false);
        }

        // 화면 밖 상자 표시 — 가장 가까운 상자가 화면 밖이면 그 방향 가장자리에 금색 마름모 + 거리
        private RectTransform _chestPtr;
        private TMP_Text _chestDist;
        private static readonly Color ChestPtrColor = new Color(1f, 0.82f, 0.30f, 1f);
        private const float ChestEdge = 0.045f;

        private void BuildChestPointer(RectTransform parent)
        {
            RectTransform rt = MakeRect("ChestPointer", parent);
            rt.sizeDelta = new Vector2(22f, 22f);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = WhiteSprite();
            img.color = ChestPtrColor;
            img.raycastTarget = false;
            rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _chestPtr = rt;

            _chestDist = MakeText(parent, "ChestDist", "", 14f, TextAlignmentOptions.Center);
            Ink(_chestDist, 0.25f);
            _chestDist.color = ChestPtrColor;
            _chestDist.rectTransform.sizeDelta = new Vector2(60f, 20f);
            rt.gameObject.SetActive(false);
            _chestDist.gameObject.SetActive(false);
        }

        private void RefreshChestPointer()
        {
            if (_chestPtr == null)
                return;
            Player p = GameEvents.CurrentPlayer;
            Camera cam = CameraFollow.GameCamera;
            Jinhyeong_Parts.PartPickup best = null;
            float bestSq = float.MaxValue;
            System.Collections.Generic.IReadOnlyList<Jinhyeong_Parts.PartPickup> live = Jinhyeong_Parts.PartPickup.Live;
            for (int i = 0; p != null && i < live.Count; i++)
            {
                if (live[i] == null)
                    continue;
                float sq = (live[i].transform.position - p.transform.position).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = live[i];
                }
            }

            bool show = false;
            Vector3 vp = Vector3.zero;
            if (best != null && cam != null)
            {
                vp = cam.WorldToViewportPoint(best.transform.position);
                show = vp.z > 0f && (vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f);
            }
            if (_chestPtr.gameObject.activeSelf != show)
            {
                _chestPtr.gameObject.SetActive(show);
                _chestDist.gameObject.SetActive(show);
            }
            if (show == false)
                return;

            // 화면 중심에서 상자 쪽으로 뻗은 선이 가장자리(여백 ChestEdge)와 만나는 곳
            Vector2 d = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            float half = 0.5f - ChestEdge;
            float k = half / Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y), 0.0001f);
            Vector2 at = new Vector2(0.5f, 0.5f) + d * k;
            _chestPtr.anchorMin = _chestPtr.anchorMax = at;
            _chestPtr.anchoredPosition = Vector2.zero;
            float pulse = 1f + 0.15f * Mathf.Sin(Time.unscaledTime * 6f);
            _chestPtr.localScale = new Vector3(pulse, pulse, 1f);

            RectTransform tr = _chestDist.rectTransform;
            tr.anchorMin = tr.anchorMax = at;
            tr.anchoredPosition = new Vector2(0f, at.y > 0.5f ? -24f : 24f);
            int meters = Mathf.RoundToInt(Mathf.Sqrt(bestSq));
            if (meters != _chestMeters)
            {
                _chestMeters = meters;
                _chestDist.text = meters + "m";
            }
        }

        private int _chestMeters = -1;

        private void RefreshBossBar()
        {
            if (_bossRoot == null)
                return;
            if (_bossHp == null || _bossHp.IsAlive == false)
            {
                _bossHp = null;
                // 보스는 드물다 — 0.5초마다만 찾는다
                if (Time.unscaledTime >= _bossScanAt)
                {
                    _bossScanAt = Time.unscaledTime + 0.5f;
                    EnemySpawner sp = EnemySpawner.Instance;
                    for (int i = 0; sp != null && i < sp.Active.Count; i++)
                    {
                        Enemy e = sp.Active[i];
                        if (e != null && e.IsBoss && e.Damageable != null && e.Damageable.IsAlive)
                        {
                            _bossHp = e.Damageable;
                            Jinhyeong_SkillSystem.MapData map = Jinhyeong_SkillSystem.MapRegistry.Current;
                            _bossName.text = map != null && string.IsNullOrEmpty(map.Boss) == false ? map.Boss : Loc.T("Hud_Boss", "보스");
                            break;
                        }
                    }
                }
            }
            bool show = _bossHp != null;
            if (_bossRoot.activeSelf != show)
                _bossRoot.SetActive(show);
            if (show)
                _bossFill.fillAmount = _bossHp.MaxHp > 0f ? Mathf.Clamp01(_bossHp.Hp / _bossHp.MaxHp) : 0f;
        }

        private void BuildBanner(RectTransform parent)
        {
            // 패널이 부모, 글자가 자식이다. UGUI 는 자식을 부모 위에 그린다 — 반대로 두면 패널이 글자를 덮는다
            RectTransform rt = MakeRect("Banner", parent);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620f, 46f);
            rt.anchoredPosition = new Vector2(0f, 196f);
            _bannerRoot = rt.gameObject;

            Image bgImage = _bannerRoot.AddComponent<Image>();
            bgImage.color = BarBack;
            Jinhyeong_UI.Framework.UISkin.ApplySliced(bgImage, Jinhyeong_UI.Framework.UISkin.CardBg, Jinhyeong_UI.Framework.UIBuild.CardCorner);
            bgImage.raycastTarget = false;
            Jinhyeong_UI.Framework.UIBuild.Outline(rt, Jinhyeong_UI.Framework.UIBuild.ColorOutline, Jinhyeong_UI.Framework.UISkin.CardFrame, Jinhyeong_UI.Framework.UIBuild.CardCorner);

            _banner = MakeText(rt, "Label", "", 28f, TextAlignmentOptions.Center);
            _banner.enableAutoSizing = true;
            _banner.fontSizeMin = 14f;
            _banner.fontSizeMax = 24f;
            _banner.margin = new Vector4(16f, 0f, 16f, 0f);
            _banner.color = Jinhyeong_UI.Framework.UIBuild.ColorDangerArmed;
            Ink(_banner, 0.15f);
            Stretch(_banner.rectTransform);

            _bannerRoot.SetActive(false);
        }

        private void ShowBanner(string text)
        {
            ShowBanner(text, 2.5f, Jinhyeong_UI.Framework.UIBuild.ColorDangerArmed);
        }

        private void ShowBanner(string text, float seconds)
        {
            ShowBanner(text, seconds, Jinhyeong_UI.Framework.UIBuild.ColorDangerArmed);
        }

        private void ShowBanner(string text, float seconds, Color color)
        {
            if (_banner != null)
                _banner.color = color;

            _banner.text = text;
            _bannerRoot.SetActive(true);
            _bannerUntil = Time.unscaledTime + seconds;
        }

        private void Restart()
        {
            _resultPanel.SetActive(false);
            if (_pausePanel != null)
                _pausePanel.SetActive(false);
            _tipShown = false;
            if (WaveDirector.Instance != null)
                WaveDirector.Instance.OnBanner -= ShowBanner;
            _bannerHooked = false;
            GameFlow.OnGameEnded -= ShowResult;
            if (RunProgress.Instance != null)
                RunProgress.Instance.OnLevelUp -= HandleLevelUp;
            _endHooked = false;
            _levelUpHooked = false;
            _pendingLevelUps = 0;
            _autoDamagePicks = 0;
            _autoHpPicks = 0;
            if (_levelPanel != null)
                _levelPanel.SetActive(false);
            _playerHp = null;
            Time.timeScale = 1f;

            if (GameInitializer.Instance != null)
                GameInitializer.Instance.Restart();
        }

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

            tex.hideFlags = HideFlags.HideAndDontSave;

            _whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
            _whiteSprite.hideFlags = HideFlags.HideAndDontSave;
            return _whiteSprite;
        }
    }
}
