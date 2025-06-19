Shader "Custom/JumpObstacle"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0, 0, 0, 1)
        _WaveColor("Wave Color", Color) = (0.2, 0.8, 1, 1)
        _Speed("Speed", Float) = 2
        _Frequency("Frequency", Float) = 10
        _Amplitude("Amplitude", Float) = 0.5
    }

        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        Pass
        {
            Name "WavePulsePass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _BaseColor;
            float4 _WaveColor;
            float _Speed;
            float _Frequency;
            float _Amplitude;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float wave = sin((IN.uv.y + _Time.y * _Speed) * _Frequency) * _Amplitude;
                float brightness = smoothstep(0.4, 0.6, wave);
                float4 color = lerp(_BaseColor, _WaveColor, brightness);
                return color;
            }
            ENDHLSL
        }
    }
}
