Shader "RTS/Presentation/NightGlow"
{
    Properties { [PerRendererData] _MainTex("Sprite", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NightFog); SAMPLER(sampler_NightFog);
            float4 _NightFogBounds;
            float _NightFogEnabled;
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 world : TEXCOORD1; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings o;
                float3 world = TransformObjectToWorld(input.positionOS);
                o.positionCS = TransformWorldToHClip(world); o.world = world.xy; o.uv = input.uv; o.color = input.color * unity_SpriteColor;
                return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                if (_NightFogEnabled > .5)
                {
                    float2 uv = (input.world - _NightFogBounds.xy) / max(_NightFogBounds.zw, .001);
                    color.a *= SAMPLE_TEXTURE2D(_NightFog, sampler_NightFog, uv).r
                        * step(0, uv.x) * step(0, uv.y) * step(uv.x, 1) * step(uv.y, 1);
                }
                return color;
            }
            ENDHLSL
        }
    }
}
