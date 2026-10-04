// 배경 · 소품용 스프라이트. 채도와 밝기를 눌러 캐릭터 · 이펙트가 뜨게 한다.
// 원본 팩(64px)은 다시 구울 수 없어서 톤은 머티리얼 값으로 조정한다 — 목표값은 spec 아트 규격 절(배경 채도 · 명도).
// 비용: 샘플 1회 + 휘도 내적 1회. TilemapRenderer 와 SpriteRenderer 둘 다 쓴다.
Shader "Jinhyeong/ToneSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Saturation ("Saturation", Range(0, 1.5)) = 1
        _Brightness ("Brightness", Range(0, 1.5)) = 1
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex ToneVertex
            #pragma fragment ToneFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Saturation;
                half _Brightness;
            CBUFFER_END

            Varyings ToneVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 ToneFragment(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                half luma = dot(tex.rgb, half3(0.2126, 0.7152, 0.0722));
                tex.rgb = lerp(luma.xxx, tex.rgb, _Saturation) * _Brightness;
                return tex;
            }
            ENDHLSL
        }
    }
}
