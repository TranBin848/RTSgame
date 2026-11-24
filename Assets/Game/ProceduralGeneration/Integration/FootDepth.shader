Shader "RTS/Presentation/FootDepth"
{
    Properties { [PerRendererData] _MainTex("Sprite", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent+2" "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite On Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            TEXTURE2D(_MainTex);
            TEXTURE2D(_DepthFog); SAMPLER(sampler_DepthFog);
            float4 _DepthFogBounds;
            float _DepthFogEnabled, _DepthFootY, _DepthFade, _DepthRoofY, _DepthMobile;
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 world : TEXCOORD1; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input); SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                float3 world = TransformObjectToWorld(input.positionOS);
                world.z = -1 + _DepthFootY * .001;
                Varyings o; o.positionCS = TransformWorldToHClip(world); o.world = world.xy;
                float3 anchor=TransformObjectToWorld(float3(0,0,0)); anchor.z=world.z;
                float4 anchorCS=TransformWorldToHClip(anchor);
                float2 pixel=anchorCS.xy/anchorCS.w*_ScreenParams.xy*.5;
                o.positionCS.xy+=(round(pixel)-pixel)*2/_ScreenParams.xy*o.positionCS.w;
                o.uv = input.uv; o.color = input.color * unity_SpriteColor; return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_PointClamp, input.uv) * input.color;
                float visibility = 1;
                if (_DepthFogEnabled > .5)
                {
                    float2 uv = (input.world - _DepthFogBounds.xy) / max(_DepthFogBounds.zw, .001);
                    visibility = SAMPLE_TEXTURE2D(_DepthFog, sampler_DepthFog, uv).r
                        * step(0, uv.x) * step(0, uv.y) * step(uv.x, 1) * step(uv.y, 1);
                    if (_DepthMobile > .5) visibility = step(.9, visibility);
                }
                color.a = min(color.a, visibility);
                clip(color.a - .05);
                // Pixel stipple leaves real depth holes for the later instanced pass, only above roof/trunk.
                float coverage = lerp(1, .4, _DepthFade * step(.9, visibility) * smoothstep(_DepthRoofY, _DepthRoofY + .15, input.world.y));
                int2 p = int2(input.positionCS.xy) & 3;
                static const float bayer[16] = {0,8,2,10,12,4,14,6,3,11,1,9,15,7,13,5};
                clip(coverage - (bayer[p.y * 4 + p.x] + .5) / 16);
                return color;
            }
            ENDHLSL
        }
    }
}
