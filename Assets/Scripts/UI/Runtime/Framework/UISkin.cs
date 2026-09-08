using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jinhyeong_UI.Framework
{

    /// <summary>
    /// 화면에 쓰는 스프라이트를 «한 곳»에서 준다.
    ///
    /// 전부 흰색 + 알파라 색은 코드가 정한다(<see cref="UIBuild"/> 팔레트) — 스프라이트는 «모양»만 낸다.
    /// 같은 모양을 두 군데서 각자 로드하면 한쪽만 바꿨을 때 화면 절반이 옛 모양으로 남는다.
    ///
    /// <c>Resources</c> 에 두는 이유는 **굽는 쪽과 도는 쪽이 같은 길을 써야 하기 때문**이다.
    /// 프리팹 빌더는 에디터에서, HUD 는 런타임에 스프라이트를 붙이는데,
    /// 둘이 다른 방식(AssetDatabase / Addressable)으로 집으면 「에디터에선 보이는데 빌드에선 없다」가 난다.
    /// 11장 130KB 라 상주 비용도 문제가 아니다.
    /// </summary>
    public static class UISkin
    {
        private const string Root = "UI/";

        /// <summary>둥근 사각 — 카드 · 버튼 · 탭. 9슬라이스(고유 경계 15px).</summary>
        public const string CardBg = "ui_rectangle_small_01";
        public const string CardFrame = "ui_rectangle_frame_small_01";

        /// <summary>큰 둥근 사각 — 패널 · 레일. 9슬라이스(고유 경계 30px).</summary>
        public const string PanelBg = "ui_rectangle_big_02";
        public const string PanelFrame = "ui_rectangle_frame_big_02";

        /// <summary>원 — 성좌의 별. 정사각형에 <see cref="Image.Type.Simple"/> 로 써야 원이 된다.</summary>
        public const string CircleBg = "ui_circle_bg_small_01";
        public const string CircleFrame = "ui_circle_frame_small_01";

        /// <summary>막대 — 체력 · 경험치. 9슬라이스(고유 경계 7px).</summary>
        public const string BarBg = "ui_slider_bg_small_01";

        /// <summary>
        /// 스킬 아이콘. 파일 이름이 곧 스킬 id 라 표를 따로 들지 않는다
        /// (<c>_DataExporter/tools/build_skillicons.py</c> 가 굽는다).
        /// </summary>
        public static Sprite SkillIcon(int skillId)
        {
            return Get("Icons/" + skillId);
        }

        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>(12);

        public static Sprite Get(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                return null;
            }
            if (_cache.TryGetValue(spriteName, out Sprite cached))
            {
                return cached;
            }

            Sprite sprite = Resources.Load<Sprite>(Root + spriteName);
            if (sprite == null)
            {
                Debug.LogWarning($"[UISkin] '{Root}{spriteName}' 을 찾을 수 없습니다 — 단색으로 그립니다");
            }
            _cache[spriteName] = sprite;
            return sprite;
        }

        /// <summary>
        /// 9슬라이스로 붙인다.
        ///
        /// 경계 두께는 **스프라이트에서 읽는다.** 코드에 적어 두면 임포트 설정을 고쳤을 때
        /// 조용히 어긋나고, 어긋난 것은 「모서리가 좀 이상한데」로만 보여 원인을 못 찾는다.
        ///
        /// [사고] 받은 리소스의 경계가 실제 아트의 «네 배»(128px 스프라이트에 63px)로 잡혀 있었다.
        /// 그러면 늘어나는 가운데가 2px 밖에 안 남아 9슬라이스가 사실상 안 걸리고,
        /// 높이 44px 버튼에서 모서리 반지름이 63px 라 버튼이 알약이 된다.
        /// 그걸 <c>pixelsPerUnitMultiplier</c> 를 6~12배로 올려 가리고 있었는데,
        /// 그건 모서리 아트를 1/8 로 우겨넣는 것이라 곡률이 뭉개진다.
        /// **경계는 모서리 곡선이 끝나는 자리에 딱 맞춰야 한다** — 그래야 가운데가 넓게 늘어나고
        /// 아트가 1:1 로 그려진다. 실측해서 meta 를 15 / 30 / 7 로 고쳤다.
        ///
        /// <paramref name="cornerPixels"/> 는 «원하는 모서리 반지름»이다. 스프라이트 고유값과
        /// 비슷하게 두면 배율이 1 근처가 되어 아트가 그대로 나온다.
        /// </summary>
        public static void ApplySliced(Image image, string spriteName, float cornerPixels)
        {
            if (image == null)
            {
                return;
            }
            Sprite sprite = Get(spriteName);
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;

            Vector4 b = sprite.border;
            float border = Mathf.Max(Mathf.Max(b.x, b.y), Mathf.Max(b.z, b.w));
            if (border <= 0.01f)
            {
                // 경계가 없는 스프라이트는 자를 수 없다. 늘리면 모양이 통째로 일그러진다.
                image.type = Image.Type.Simple;
                return;
            }

            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = Mathf.Max(0.01f, border / Mathf.Max(1f, cornerPixels));
        }

        /// <summary>
        /// 테두리 전용. 가운데가 비어 있는 그림이라 가운데 조각을 그리지 않는다 —
        /// 투명한 사각형 하나를 매번 그리는 것은 그냥 낭비다.
        /// </summary>
        public static void ApplyFrame(Image image, string spriteName, float cornerPixels)
        {
            ApplySliced(image, spriteName, cornerPixels);
            if (image != null && image.type == Image.Type.Sliced)
            {
                image.fillCenter = false;
            }
        }

        /// <summary>원·아이콘처럼 «늘리면 안 되는» 모양. 9슬라이스로 붙이면 가운데가 늘어나 원이 캡슐이 된다.</summary>
        public static void ApplySimple(Image image, string spriteName)
        {
            if (image == null)
            {
                return;
            }
            Sprite sprite = Get(spriteName);
            if (sprite == null)
            {
                return;
            }
            image.sprite = sprite;
            image.type = Image.Type.Simple;
        }
    }
}
