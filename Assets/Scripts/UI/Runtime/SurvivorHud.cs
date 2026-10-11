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

            ReadLevelUpKeys();

            WaveDirector wave = WaveDirector.Instance;
            if (wave != null)
            {
                if (_bannerHooked == false)
                {
                    wave.OnBanner += ShowBanner;
                    _bannerHooked = true;
                }
                if (_bannerRoot.activeSelf && Time.unscaledTime >= _bannerUntil)
                    _bannerRoot.SetActive(false);

                int total = Mathf.FloorToInt(wave.Elapsed);
                _timeText.text = string.Format("{0:00}:{1:00}", total / 60, total % 60);
            }

            EnemySpawner spawner = EnemySpawner.Instance;
            if (spawner != null)
                _killText.text = spawner.KillCount.ToString();

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
                _hpFill.fillAmount = _playerHp.MaxHp > 0f ? _playerHp.Hp / _playerHp.MaxHp : 0f;
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
            BuildLevelUpPanel(safe);

            _root.SetActive(false);
        }

        // 아트 리부트 (볼트 개많이잡아/개많이잡아 아트 방향 UI 방향) — XP 는 화면 위 가로 전체, HP 는 하단 중앙 하트 바
        private static readonly Color HpColor = new Color(1f, 0.25f, 0.62f, 1f);
        private static readonly Color XpColor = new Color(0.20f, 0.90f, 1f, 1f);
        private static readonly Color BarBack = new Color(0.10f, 0.05f, 0.18f, 0.88f);
        private static readonly Color TextInk = new Color(0.18f, 0.06f, 0.30f, 1f);

        private static void Ink(TMP_Text t, float width)
        {
            t.fontStyle = FontStyles.Bold;
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
            _levelText.color = XpColor;
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

            TMP_Text t = MakeText(strip, "Parts", "", 15f, TextAlignmentOptions.Center);
            t.color = Jinhyeong_UI.Framework.UIBuild.ColorSubHead;
            Stretch(t.rectTransform);
            _partsText = t;
        }

        // ── 레벨업 3택1 ─────────────────────────────────────────────────
        // 레벨업하면 멈추고 카드 3장 중 하나를 고른다 (클릭 · 1/2/3). 한 번에 여러 레벨이 오르면 차례로 연다
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
            dim.color = new Color(0.06f, 0.02f, 0.12f, 0.78f);
            dim.raycastTarget = true;

            _levelTitle = MakeText(panel, "Title", "LEVEL UP!", 46f, TextAlignmentOptions.Center);
            Ink(_levelTitle, 0.22f);
            _levelTitle.color = Jinhyeong_UI.Framework.UIBuild.ColorGold;
            RectTransform tr = _levelTitle.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.sizeDelta = new Vector2(700f, 60f);
            tr.anchoredPosition = new Vector2(0f, 230f);

            TMP_Text hint = MakeText(panel, "Hint", "하나를 고른다  ·  클릭 또는 1 · 2 · 3", 18f, TextAlignmentOptions.Center);
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
                Ink(_cardName[i], 0.2f);
                RectTransform nr = _cardName[i].rectTransform;
                nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 1f);
                nr.sizeDelta = new Vector2(270f, 40f);
                nr.anchoredPosition = new Vector2(0f, -196f);

                _cardDesc[i] = MakeText(card, "Desc", "", 16f, TextAlignmentOptions.Top);
                _cardDesc[i].color = Jinhyeong_UI.Framework.UIBuild.ColorSubHead;
                _cardDesc[i].enableWordWrapping = true;
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
                    _cardKind[i].text = "새 무기";
                    break;
                case Jinhyeong_Parts.ELevelUpCardKind.Upgrade:
                    _cardKind[i].text = "무기 강화";
                    break;
                case Jinhyeong_Parts.ELevelUpCardKind.Amp:
                    _cardKind[i].text = "기본 공격 앰프";
                    break;
                default:
                    _cardKind[i].text = "능력";
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
                _cardDesc[i].text = sb.Length > 0 ? sb.ToString() : c.Part.Data.Description;
            }
            else
            {
                icon = Jinhyeong_UI.Framework.UISkin.Get(Jinhyeong_UI.Framework.UISkin.Heart);
                accent = Jinhyeong_UI.Framework.UIBuild.ColorGold;
                _cardName[i].text = StatName(c.StatTrait);
                _cardDesc[i].text = StatDesc(c.StatTrait, c.StatValue);
            }
            _cardIcon[i].sprite = icon;
            _cardIcon[i].enabled = icon != null;
            _cardIcon[i].color = accent;
            _cardKind[i].color = accent;
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

        private static string StatName(EPartTrait t)
        {
            switch (t)
            {
                case EPartTrait.Damage: return "볼륨 업";
                case EPartTrait.MaxHp: return "튼튼한 성대";
                case EPartTrait.MoveSpeed: return "경쾌한 스텝";
                default: return "앙코르 함성";
            }
        }

        private static string StatDesc(EPartTrait t, float v)
        {
            switch (t)
            {
                case EPartTrait.Damage: return $"공격력 +{Mathf.RoundToInt(v * 100f)}%";
                case EPartTrait.MaxHp: return $"최대 체력 +{Mathf.RoundToInt(v * 100f)}%";
                case EPartTrait.MoveSpeed: return $"이동속도 +{Mathf.RoundToInt(v * 100f)}%";
                default: return $"처치 시 체력 +{v:0.#}";
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

        private void BuildResultPanel(RectTransform parent)
        {
            RectTransform panel = MakeRect("ResultPanel", parent);
            Stretch(panel);
            _resultPanel = panel.gameObject;

            Image dim = _resultPanel.AddComponent<Image>();
            dim.sprite = WhiteSprite();
            dim.color = new Color(0.06f, 0.02f, 0.12f, 0.92f);
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
            btnBg.color = Jinhyeong_UI.Framework.UIBuild.ColorStart;
            Jinhyeong_UI.Framework.UISkin.ApplySliced(btnBg, Jinhyeong_UI.Framework.UISkin.CardBg, Jinhyeong_UI.Framework.UIBuild.CardCorner);
            Jinhyeong_UI.Framework.UIBuild.Outline(btn, Color.white, Jinhyeong_UI.Framework.UISkin.CardFrame, Jinhyeong_UI.Framework.UIBuild.CardCorner);
            Button button = btn.gameObject.AddComponent<Button>();
            button.targetGraphic = btnBg;
            button.onClick.AddListener(Restart);

            TMP_Text btnText = MakeText(btn, "Label", "다시 하기", 24f, TextAlignmentOptions.Center);
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
            // 패널이 부모, 글자가 자식이다. UGUI 는 자식을 부모 위에 그린다 — 반대로 두면 패널이 글자를 덮는다
            RectTransform rt = MakeRect("Banner", parent);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(700f, 64f);
            rt.anchoredPosition = new Vector2(0f, 150f);
            _bannerRoot = rt.gameObject;

            Image bgImage = _bannerRoot.AddComponent<Image>();
            bgImage.color = BarBack;
            Jinhyeong_UI.Framework.UISkin.ApplySliced(bgImage, Jinhyeong_UI.Framework.UISkin.CardBg, Jinhyeong_UI.Framework.UIBuild.CardCorner);
            bgImage.raycastTarget = false;
            Jinhyeong_UI.Framework.UIBuild.Outline(rt, Jinhyeong_UI.Framework.UIBuild.ColorOutline, Jinhyeong_UI.Framework.UISkin.CardFrame, Jinhyeong_UI.Framework.UIBuild.CardCorner);

            _banner = MakeText(rt, "Label", "", 28f, TextAlignmentOptions.Center);
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
