Shader "Custom/DodgeObstacle"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.05, 0.05, 0.05, 1)
        _GridColor("Grid Color", Color) = (1, 0.2, 0.2, 1)
        _Tiling("Tiling", Vector) = (8, 8, 0, 0)
        _ScrollSpeed("Scroll Speed", Float) = 0.2
        _LineWidth("Line Width", Float) = 0.02
    }

        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        Pass
        {
            Name "GridPass"
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
            float4 _GridColor;
            float4 _Tiling;
            float _ScrollSpeed;
            float _LineWidth;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv * _Tiling.xy;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                uv.x += _Time.y * _ScrollSpeed;

                float lineX = abs(frac(uv.x) - 0.5);
                float lineY = abs(frac(uv.y) - 0.5);
                float grid = step(lineX, _LineWidth) + step(lineY, _LineWidth);

                float4 color = lerp(_BaseColor, _GridColor, saturate(grid));
                return color;
            }
            ENDHLSL
        }
    }
}
