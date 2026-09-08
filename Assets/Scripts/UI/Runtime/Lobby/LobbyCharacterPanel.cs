using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;
using Jinhyeong_UI.Framework;

namespace Jinhyeong_UI.Lobby
{

    /// <summary>
    /// 캐릭터 탭 — 슈퍼독을 고르고, 사료로 영구 강화한다.
    /// 카드와 강화 칸은 Component 로 풀에서 꺼낸다 — 프리팹에 N개 박아 두지 않는다.
    ///
    /// 고유 스킬의 계열이 곧 이번 판의 선호 계열이라, **캐릭터 선택이 빌드 방향 선택**이다.
    /// </summary>
    public class LobbyCharacterPanel : BasePanel
        , HeroCardComponent.IListener
        , LabelButtonComponent.IListener
    {
        public static readonly PrefabAuto<LobbyCharacterPanel> PrefabAuto = new PrefabAuto<LobbyCharacterPanel>(PrefabPath);
        public const string PrefabPath = "UI/Lobby/CharacterPanel";

        private static readonly SaveData.EStat[] Stats = { SaveData.EStat.Hp, SaveData.EStat.Attack, SaveData.EStat.Speed };
        private static readonly string[] StatNames = { "체력", "공격력", "이동속도" };

        [SerializeField] private RectTransform _cardRoot;
        [SerializeField] private TMP_Text _shopHint;
        [SerializeField] private RectTransform _statRow;

        private LobbyWindow _lobby;
        private readonly List<HeroCardComponent> _cards = new List<HeroCardComponent>(4);
        private readonly List<LabelButtonComponent> _statButtons = new List<LabelButtonComponent>(3);

        protected override void Opening()
        {
            _lobby = Window as LobbyWindow;

            RunSelection.Hero[] heroes = RunSelection.Heroes;
            for (int i = 0; i < heroes.Length; i++)
            {
                HeroCardComponent card = HeroCardComponent.PrefabAuto.CreateForUI(_cardRoot);
                if (card == null)
                {
                    continue;
                }
                Sprite face = Resources.Load<Sprite>(heroes[i].PortraitPath);
                card.Open(this, heroes[i].Index, heroes[i].Name, face);
                _cards.Add(card);
            }

            for (int i = 0; i < Stats.Length; i++)
            {
                LabelButtonComponent b = LabelButtonComponent.PrefabAuto.CreateForUI(_statRow);
                if (b == null)
                {
                    continue;
                }
                b.Open(this, i, string.Empty);
                b.SetColor(UIBuild.ColorCardSoft);
                _statButtons.Add(b);
            }

            // 파츠 테이블은 비동기로 늦게 로드된다. 상태가 바뀔 때 카드 설명을 다시 그린다.
            if (_lobby != null)
            {
                _lobby.OnStatusChanged -= RefreshCards;
                _lobby.OnStatusChanged += RefreshCards;
            }

            RefreshCards();
            RefreshShop();
        }

        protected override void OnReShown()
        {
            RefreshCards();
            RefreshShop();
        }

        protected override void OnClose()
        {
            if (_lobby != null)
            {
                _lobby.OnStatusChanged -= RefreshCards;
            }
            for (int i = 0; i < _cards.Count; i++)
            {
                UILoader.Restore(_cards[i]);
            }
            _cards.Clear();
            for (int i = 0; i < _statButtons.Count; i++)
            {
                UILoader.Restore(_statButtons[i]);
            }
            _statButtons.Clear();
            _lobby = null;
        }

        /// <summary>선택 표시와 설명. 파츠 테이블이 늦게 로드되므로 문구는 매번 만든다.</summary>
        private void RefreshCards()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                HeroCardComponent card = _cards[i];
                card.SetSelected(card.Index == RunSelection.CharacterIndex);

                PartData unique = PartRegistry.GetUniqueFor(card.Index);
                card.SetDetail(unique != null
                    ? "<size=17>고유 · " + unique.DisplayName + "  ·  " + LobbyText.FamilyName(unique.Family) + " 계열</size>"
                    : "<size=17>고유 스킬 없음</size>");
            }
        }

        /// <summary>강화 칸 셋. 재화가 바뀌면 살 수 있는 것도 바뀐다.</summary>
        private void RefreshShop()
        {
            if (_shopHint != null)
            {
                _shopHint.text = $"사료 {SaveData.Kibble} · 눌러서 영구 강화";
            }

            for (int i = 0; i < _statButtons.Count && i < Stats.Length; i++)
            {
                SaveData.EStat st = Stats[i];
                int lv = SaveData.StatLevel(st);
                bool max = lv >= SaveData.StatMaxLevel;
                string detail = max ? "최대" : $"사료 {SaveData.StatCost(st)}";
                _statButtons[i].SetText($"{StatNames[i]} Lv.{lv}"
                    + $"  <size=14>+{Mathf.RoundToInt(SaveData.StatGain(st) * lv * 100f)}%</size>"
                    + System.Environment.NewLine + $"<size=14>{detail}</size>");
            }
        }

        void HeroCardComponent.IListener.OnClickHero(int index)
        {
            RunSelection.CharacterIndex = index;
            RefreshCards();
        }

        void LabelButtonComponent.IListener.OnClickLabelButton(int index)
        {
            if (index < 0 || index >= Stats.Length)
            {
                return;
            }
            // 「살 수 있나」의 판정은 SaveData(시스템)가 하고, 여기는 결과만 그린다.
            if (SaveData.BuyStat(Stats[index]) == false)
            {
                return;
            }
            RefreshShop();
        }
    }
}
