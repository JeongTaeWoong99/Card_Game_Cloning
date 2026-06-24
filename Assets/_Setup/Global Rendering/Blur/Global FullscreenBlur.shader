// Full Screen Pass Renderer Feature 전용 전체화면 블러 셰이더.
// 카메라가 그린 색(_BlitTexture)을 받아 박스 블러로 흐리게 출력한다.
// 글로벌 _BlurIntensity(0이면 원본 그대로, 커질수록 강하게)로 게이팅 — 평소엔 비용 없이 패스스루.
// 결과 화면에서 게임 보드만 흐리게 하려고 보드 렌더러(Main Camera)에만 추가한다. (UI 오버레이 카메라엔 미적용)
Shader "Custom/FullscreenBlur"
{
    Properties
    {
        _BlurSize ("Blur Size", Range(0, 8)) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "FullscreenBlurPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            // Blit.hlsl: 풀스크린 삼각형 Vert + _BlitTexture / sampler_LinearClamp 제공
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _BlurSize;
            float _BlurIntensity; // GameManager가 Shader.SetGlobalFloat로 제어 (0~1)

            // 프래그먼트 — 강도가 0이면 원본 1탭, 아니면 3x3 박스 블러
            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // 평소(강도 0)엔 블러 비용 없이 원본 그대로 통과시킨다
                if (_BlurIntensity <= 0.001)
                {
                    return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                }

                float2 texel  = _BlitTexture_TexelSize.xy;
                float2 offset = texel * _BlurSize * _BlurIntensity;

                half4 sum = 0;

                [unroll]
                for (int x = -1; x <= 1; x++)
                {
                    [unroll]
                    for (int y = -1; y <= 1; y++)
                    {
                        sum += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp,
                                                uv + float2(x, y) * offset);
                    }
                }

                return sum / 9.0;
            }
            ENDHLSL
        }
    }
}
