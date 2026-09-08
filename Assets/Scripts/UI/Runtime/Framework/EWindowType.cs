namespace Jinhyeong_UI.Framework
{

    /// <summary>
    /// 창의 깊이 대역. 숫자를 창마다 손으로 정하면 화면이 늘 때마다 충돌한다 —
    /// **대역을 먼저 고르고**, 그 안의 층은 <see cref="UIManagement"/> 가 여는 순서대로 준다.
    /// 이름은 레퍼런스 프로젝트(ProjectSample)의 EWindowType 을 따르고, 범위는 UIFramework.md 밴드 표를 따른다.
    /// </summary>
    public enum EWindowType
    {
        /// <summary>씬 종속 상시 UI. 뒤를 끄지 않고, Close() 로는 안 닫힌다.</summary>
        Passive = 0,
        /// <summary>대부분의 콘텐츠 창. 전체 크기면 뒤 화면 캔버스를 끈다.</summary>
        Normal = 1,
        /// <summary>로딩 · 씬 전환. 전부 덮는다.</summary>
        Global = 2,
        /// <summary>확인 · 알림.</summary>
        Popup = 3,
        /// <summary>터치 이펙트 · 입력 차단막.</summary>
        Top = 4,
        /// <summary>튜토리얼 마스크.</summary>
        Tutorial = 5,
        /// <summary>미니 메뉴 · 컨텍스트.</summary>
        LowDepth = 6,
        /// <summary>창이 자기 <c>ConstantDepth</c> 를 쓴다. 명시적 예외.</summary>
        ConstantDepth = 7,
        /// <summary>종료 · 치명적 오류. 최상위.</summary>
        SystemPopup = 8,
        /// <summary>씬 종속이지만 조건에 따라 꺼질 수 있는 상시 UI.</summary>
        PassiveClosable = 9,
    }
}
