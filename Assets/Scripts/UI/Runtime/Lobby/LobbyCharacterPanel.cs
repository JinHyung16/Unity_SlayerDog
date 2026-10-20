using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Jinhyeong_Common;
using Jinhyeong_GameData;
using Jinhyeong_SkillSystem;
using Jinhyeong_UI.Framework;

namespace Jinhyeong_UI.Lobby
{

    public class LobbyCharacterPanel : BasePanel
        , HeroCardComponent.IListener
        , LabelButtonComponent.IListener
    {
        public static readonly PrefabAuto<LobbyCharacterPanel> PrefabAuto = new PrefabAuto<LobbyCharacterPanel>(PrefabPath);
        public const string PrefabPath = "UI/Lobby/CharacterPanel";

        private static readonly SaveData.EStat[] Stats = { SaveData.EStat.Hp, SaveData.EStat.Attack, SaveData.EStat.Speed };

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

        private static string StatName(SaveData.EStat st)
        {
            switch (st)
            {
                case SaveData.EStat.Hp: return Loc.T("Char_StatHp", "체력");
                case SaveData.EStat.Attack: return Loc.T("Char_StatAttack", "공격력");
                default: return Loc.T("Char_StatSpeed", "이동속도");
            }
        }

        private void RefreshCards()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                HeroCardComponent card = _cards[i];
                bool locked = SaveData.IsHeroUnlocked(card.Index) == false;
                card.SetLocked(locked);
                card.SetSelected(card.Index == RunSelection.CharacterIndex);

                if (locked)
                {
                    MapData origin = MapRegistry.Get(MapRegistry.OriginMapOf(card.Index));
                    card.SetDetail(origin != null
                        ? Loc.F("Char_JoinsOnClear", "<size=75%>{0} 클리어</size>", origin.DisplayName)
                        : Loc.T("Char_Locked", "<size=75%>잠김</size>"));
                    continue;
                }
                PartData unique = PartRegistry.GetUniqueFor(card.Index);
                card.SetDetail(unique != null
                    ? "<size=75%>" + unique.DisplayName + " · " + LobbyText.FamilyName(unique.Family) + "</size>"
                    : Loc.T("Char_NoUniqueSkill", "<size=75%>고유 스킬 없음</size>"));
            }
        }

        private void RefreshShop()
        {
            if (_shopHint != null)
            {
                _shopHint.text = Loc.F("Char_ShopHint", "사료 {0} · 눌러서 영구 강화", SaveData.Kibble);
            }

            for (int i = 0; i < _statButtons.Count && i < Stats.Length; i++)
            {
                SaveData.EStat st = Stats[i];
                int lv = SaveData.StatLevel(st);
                bool max = lv >= SaveData.StatMaxLevel;
                string detail = max ? Loc.T("Char_StatMax", "최대") : Loc.F("Char_StatCost", "사료 {0}", SaveData.StatCost(st));
                // 살 수 없으면 비용을 붉게, 최대면 회색 — 누를 수 있는 것과 없는 것이 한눈에 갈리게
                if (max)
                    detail = "<color=#8C8C96>" + detail + "</color>";
                else if (SaveData.Kibble < SaveData.StatCost(st))
                    detail = "<color=#E0705E>" + detail + "</color>";
                _statButtons[i].SetText($"{StatName(st)} Lv.{lv}"
                    + $"  <size=14>+{Mathf.RoundToInt(SaveData.StatGain(st) * lv * 100f)}%</size>"
                    + System.Environment.NewLine + $"<size=14>{detail}</size>");
            }
        }

        void HeroCardComponent.IListener.OnClickHero(int index)
        {
            if (SaveData.IsHeroUnlocked(index) == false)
            {
                return;
            }
            RunSelection.CharacterIndex = index;
            RefreshCards();
        }

        void LabelButtonComponent.IListener.OnClickLabelButton(int index)
        {
            if (index < 0 || index >= Stats.Length)
            {
                return;
            }
            if (SaveData.BuyStat(Stats[index]) == false)
            {
                return;
            }
            RefreshShop();
        }
    }
}
