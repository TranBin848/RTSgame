Shader "RTS/DOTS/SwarmTerrain"
{
    Properties { _BaseColor("Color", Color) = (0.22, 0.3, 0.34, 1) }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _BaseColor;
            float4 Vert(float3 positionOS : POSITION) : SV_POSITION { return TransformObjectToHClip(positionOS); }
            half4 Frag() : SV_Target { return _BaseColor; }
            ENDHLSL
        }
    }
}
