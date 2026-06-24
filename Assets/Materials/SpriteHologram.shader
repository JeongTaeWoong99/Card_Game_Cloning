Shader "Custom/SpriteHologram"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color)                        = (1, 1, 1, 1)

        [Header(Hologram Color)]
        [HDR] _HoloColor ("Hologram Tint", Color)       = (0.3, 0.8, 1.0, 1.0)
        _HoloStrength ("Hologram Strength", Range(0, 1)) = 0.6  // 원본 색 위에 홀로그램 색을 덮는 정도
        _Alpha ("Overall Alpha", Range(0, 1))            = 0.85 // 전체 반투명도

        [Header(Scanline)]
        _ScanlineDensity ("Scanline Density", Float)        = 120.0 // 주사선 개수(클수록 촘촘)
        _ScanlineSpeed ("Scanline Speed", Float)            = 2.0   // 주사선이 위로 흐르는 속도
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.4  // 주사선 명암 대비

        [Header(Glitch)]
        _FlickerSpeed ("Flicker Speed", Float)            = 8.0   // 깜빡임 속도
        _FlickerStrength ("Flicker Strength", Range(0, 1)) = 0.15 // 깜빡임으로 인한 밝기 변동
        _ChromaShift ("Chromatic Shift", Range(0, 0.05))   = 0.006 // RGB 색수차 분리 폭

        [Header(Rim Glow)]
        _RimColor ("Rim Color", Color)             = (0.5, 0.9, 1.0, 1.0)
        _RimPower ("Rim Power", Range(0.1, 8))     = 2.0 // 외곽 발광이 가장자리로 모이는 정도
        _RimIntensity ("Rim Intensity", Range(0, 8)) = 1.5 // HDR Bloom 반응용 외곽 발광 세기
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "RenderType"        = "Transparent"
            "IgnoreProjector"   = "True"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SpriteHologramPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4    _MainTex_TexelSize;
            float4    _Color;

            float4    _HoloColor;
            float     _HoloStrength;
            float     _Alpha;

            float     _ScanlineDensity;
            float     _ScanlineSpeed;
            float     _ScanlineStrength;

            float     _FlickerSpeed;
            float     _FlickerStrength;
            float     _ChromaShift;

            float4    _RimColor;
            float     _RimPower;
            float     _RimIntensity;

            // 해시 연산 — 시간 기반 의사 난수(글리치 깜빡임용)
            float hash11(float p)
            {
                return frac(sin(p * 12.9898) * 43758.5453);
            }

            // 버텍스 쉐이더 — 로컬 좌표를 클립 공간으로 투영 및 버텍스 컬러 연산
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color      = input.color * _Color;
                output.uv         = input.uv;

                return output;
            }

            // 프래그먼트 쉐이더 — 색수차 샘플 → 홀로그램 틴트 → 주사선·깜빡임 → 외곽 발광 합성
            float4 frag(Varyings input) : SV_Target
            {
                // 1. RGB 색수차 — 채널별로 UV를 살짝 어긋나게 샘플해 홀로그램 분광 느낌
                float shift = _ChromaShift;
                float r = tex2D(_MainTex, input.uv + float2( shift, 0.0)).r;
                float g = tex2D(_MainTex, input.uv).g;
                float b = tex2D(_MainTex, input.uv - float2( shift, 0.0)).b;
                float a = tex2D(_MainTex, input.uv).a;

                float4 texColor = float4(r, g, b, a) * input.color;

                // 원본 알파가 거의 없는 영역은 그대로 투명 처리
                if (texColor.a < 0.01)
                {
                    discard;
                }

                float3 col = texColor.rgb;

                // 2. 홀로그램 틴트 — 원본 밝기를 유지하면서 홀로그램 색으로 물들임
                float luminance = dot(col, float3(0.299, 0.587, 0.114));
                float3 holo     = _HoloColor.rgb * (luminance + 0.2);
                col = lerp(col, holo, _HoloStrength);

                // 3. 주사선 — 시간에 따라 위로 흐르는 명암 줄무늬
                float scan = sin((input.uv.y * _ScanlineDensity) - (_Time.y * _ScanlineSpeed));
                scan = saturate(scan * 0.5 + 0.5);
                col *= lerp(1.0, scan, _ScanlineStrength);

                // 4. 글리치 깜빡임 — 시간 구간별 난수로 전체 밝기를 미세하게 떨리게
                float flickerSeed = floor(_Time.y * _FlickerSpeed);
                float flicker     = 1.0 - _FlickerStrength * hash11(flickerSeed);
                col *= flicker;

                // 5. 외곽 발광(Rim) — 알파 경계에 가까울수록 강해지는 HDR 글로우
                float rim  = pow(1.0 - texColor.a, _RimPower);
                col += _RimColor.rgb * rim * _RimIntensity;

                return float4(col, texColor.a * _Alpha);
            }
            ENDHLSL
        }
    }
}
