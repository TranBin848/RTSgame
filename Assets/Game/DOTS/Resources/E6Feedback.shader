Shader "RTS/E6 Feedback"
{
    Properties { [PerRendererData] _MainTex("Glyph", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent+4" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_FeedbackFog); SAMPLER(sampler_FeedbackFog);
            float4 _FeedbackFogBounds;
            float _FeedbackFogEnabled;
            struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; float2 world:TEXCOORD1; };
            Varyings vert(Attributes i)
            {
                UNITY_SETUP_INSTANCE_ID(i); SetUpSpriteInstanceProperties();
                Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS); o.uv=i.uv;
                o.color=i.color*unity_SpriteColor; o.world=TransformObjectToWorld(i.positionOS).xy; return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                if (_FeedbackFogEnabled > .5)
                {
                    float2 uv=(i.world-_FeedbackFogBounds.xy)/max(_FeedbackFogBounds.zw,float2(.001,.001));
                    clip(min(min(uv.x,uv.y),min(1-uv.x,1-uv.y)));
                    clip(SAMPLE_TEXTURE2D(_FeedbackFog,sampler_FeedbackFog,uv).r-.9);
                }
                return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;
            }
            ENDHLSL
        }
    }
}
