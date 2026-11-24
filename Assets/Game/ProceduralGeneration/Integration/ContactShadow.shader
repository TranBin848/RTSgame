Shader "RTS/Presentation/ContactShadow"
{
    Properties { _ContactColor("Tint", Color) = (.08,.075,.1,.28) _ContactRing("Selection ring", Float) = 0 _ContactZTest("Depth test", Float) = 4 }
    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest [_ContactZTest] Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_ContactFog); SAMPLER(sampler_ContactFog);
            float4 _ContactFogBounds; float _ContactFogEnabled;
            float4 _ContactColor; float _ContactRing;
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 world : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input); Varyings o;
                float3 world = TransformObjectToWorld(input.positionOS);
                o.positionCS = TransformWorldToHClip(world); o.uv = input.uv; o.world = world.xy;
                return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float r = length(input.uv * 2 - 1);
                float alpha = (_ContactRing > .5 ? (1 - smoothstep(.92, 1, r)) * smoothstep(.72, .82, r)
                    : (1 - smoothstep(.25, 1, r))) * _ContactColor.a;
                if (_ContactFogEnabled > .5)
                {
                    float2 uv = (input.world - _ContactFogBounds.xy) / max(_ContactFogBounds.zw, .001);
                    alpha *= SAMPLE_TEXTURE2D(_ContactFog, sampler_ContactFog, uv).r
                        * step(0, uv.x) * step(0, uv.y) * step(uv.x, 1) * step(uv.y, 1);
                }
                return half4(_ContactColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
