Shader "RTS/DOTS/SwarmSprite"
{
    Properties { _MainTex("Sprite sheet", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite On Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_SwarmFog); SAMPLER(sampler_SwarmFog);
            float4 _SwarmFogBounds, _SwarmLight;
            float _SwarmFogEnabled;
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _UVRect)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceColor)
            UNITY_INSTANCING_BUFFER_END(Props)
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 world : TEXCOORD1; float4 color : COLOR0; };
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings o;
                float3 world = TransformObjectToWorld(input.positionOS);
                o.positionCS = TransformWorldToHClip(world);
                float4 rect = UNITY_ACCESS_INSTANCED_PROP(Props, _UVRect);
                o.uv = rect.xy + input.uv * rect.zw;
                o.world = world.xy;
                o.color = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);
                return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                if (_SwarmFogEnabled > 0.5)
                {
                    float2 uv = (input.world - _SwarmFogBounds.xy) / max(_SwarmFogBounds.zw, 0.001);
                    float visible = SAMPLE_TEXTURE2D(_SwarmFog, sampler_SwarmFog, uv).r;
                    color.a *= visible * step(0, uv.x) * step(0, uv.y) * step(uv.x, 1) * step(uv.y, 1);
                }
                clip(color.a - 0.05);
                color.rgb *= _SwarmLight.rgb;
                return color;
            }
            ENDHLSL
        }
    }
}
