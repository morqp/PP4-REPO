Shader "Hidden/Outline/Unlit"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,1,1,1)
        _OutlineWidth ("Outline Width (world units)", Range(0, 0.2)) = 0.03
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        LOD 100

        Cull Front       // draw backfaces only (inverted hull shell)
        ZWrite On
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };
            struct Varyings {
                float4 positionHCS : SV_POSITION;
            };

            float4 _OutlineColor;
            float  _OutlineWidth;

            Varyings vert (Attributes v)
            {
                Varyings o;
                // Extrude along *smoothed* normals (the script will bake them)
                float3 n = normalize(v.normalOS);
                float3 pos = v.positionOS.xyz + n * _OutlineWidth;
                o.positionHCS = TransformObjectToHClip(pos);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
    FallBack Off
}