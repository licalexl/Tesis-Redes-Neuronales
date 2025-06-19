Shader "Custom/TrainingRoomWall"
{
    Properties
    {
        _MainColor("Base Color", Color) = (0.05, 0.1, 0.15, 1)
        _LineColor("Line Color", Color) = (0.2, 1, 0.6, 1)
        _LineThickness("Line Thickness", Float) = 0.05
        _Speed("Line Scroll Speed", Float) = 0.5
        _Tiling("Tiling", Vector) = (10, 10, 0, 0)
    }

        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        Pass
        {
            Name "TrainingWallPass"
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

            float4 _MainColor;
            float4 _LineColor;
            float _LineThickness;
            float _Speed;
            float4 _Tiling;

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
                uv.y += _Time.y * _Speed;

                float lineX = abs(frac(uv.x) - 0.5);
                float lineY = abs(frac(uv.y) - 0.5);

                float grid = step(lineX, _LineThickness) + step(lineY, _LineThickness);

                float4 color = lerp(_MainColor, _LineColor, saturate(grid));
                return color;
            }
            ENDHLSL
        }
    }
        FallBack "Hidden/InternalErrorShader"
}
