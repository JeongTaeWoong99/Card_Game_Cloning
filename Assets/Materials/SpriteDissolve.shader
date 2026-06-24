Shader "Custom/SpriteDissolve"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color)                        = (1, 1, 1, 1)

        [Header(Dissolve Settings)]
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0.0
        _NoiseScale ("Noise Scale", Float)               = 20.0

        [Header(Glow Settings)]
        _EdgeWidth ("Edge Width", Range(0, 0.2))        = 0.05
        [HDR] _EdgeColor ("Edge Color", Color)          = (1.0, 0.3, 0.0, 1.0)
        _GlowIntensity ("Glow Intensity", Range(1, 10)) = 4.0 // HDR Bloom 효과 반응을 위한 증폭 값
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
            Name "SpriteDissolvePass"

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
            float4    _Color;
            float     _DissolveAmount;
            float     _NoiseScale;
            float     _EdgeWidth;
            float4    _EdgeColor;
            float     _GlowIntensity;

            // 해시 연산 — 난수 입력을 위한 간이 2D 해시 함수
            float hash(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // 노이즈 연산 — 부드러운 2D 그라디언트 노이즈 생성
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(hash(i + float2(0.0, 0.0)), hash(i + float2(1.0, 0.0)), u.x),
                            lerp(hash(i + float2(0.0, 1.0)), hash(i + float2(1.0, 1.0)), u.x), u.y);
            }

            // FBM(Fractal Brownian Motion) — 여러 옥타브를 누적해 디테일한 연소 노이즈 생성
            float fbm(float2 p)
            {
                float  v     = 0.0;
                float  a     = 0.5;
                float2 shift = float2(100.0, 100.0);

                // 노이즈가 특정 축으로 쏠리는 현상을 방지하기 위한 소형 회전 행렬 상수
                float2 rot1  = float2(0.87758, 0.47942);  // cos(0.5), sin(0.5)
                float2 rot2  = float2(-0.47942, 0.87758); // -sin(0.5), cos(0.5)

                for (int i = 0; i < 4; ++i)
                {
                    v += a * noise(p);
                    p = float2(dot(p, rot1), dot(p, rot2)) * 2.0 + shift;
                    a *= 0.5;
                }

                return v;
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

            // 프래그먼트 쉐이더 — 디졸브 영역 계산, 픽셀 제거, 테두리 HDR 발광 및 투명 처리
            float4 frag(Varyings input) : SV_Target
            {
                float4 texColor = tex2D(_MainTex, input.uv) * input.color;

                // 절차적 FBM 노이즈 생성 [0, 1] 범위
                float n = fbm(input.uv * _NoiseScale);

                // 현재 타들어가는 양에 따라 임계값 계산
                float dissolveValue = n - _DissolveAmount;

                // 임계값보다 낮은 영역은 타버린 부위이므로 렌더링에서 영구 제외(discard)
                if (dissolveValue < 0.0)
                {
                    discard;
                }

                // 원본 스프라이트의 알파 값이 거의 없는 부위도 가이드 강조가 보이지 않도록 필터링
                if (texColor.a < 0.01)
                {
                    discard;
                }

                float4 finalColor = texColor;

                // 테두리 불꽃(Glow) 효과 연산
                if (_EdgeWidth > 0.001)
                {
                    float edgeLerp = saturate(dissolveValue / _EdgeWidth);

                    if (edgeLerp < 1.0)
                    {
                        // URP 포스트 프로세싱 Bloom에 강하게 반응하도록 RGB 강도를 HDR 규격(> 1.0)으로 증폭시킵니다.
                        float3 glow = _EdgeColor.rgb * (1.0 - edgeLerp) * _GlowIntensity;
                        
                        finalColor.rgb = lerp(_EdgeColor.rgb, texColor.rgb, edgeLerp) + glow;
                    }
                }

                return finalColor;
            }
            ENDHLSL
        }
    }
}