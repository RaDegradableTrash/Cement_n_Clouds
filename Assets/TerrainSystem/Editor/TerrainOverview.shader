Shader "Hidden/Voyage/TerrainOverview"
{
    Properties
    {
        _ShowWeights ("Show weights", Float) = 1
        _DensityMap ("Density overview", 2D) = "white" {}
        _RoadMap ("Road preview", 2D) = "black" {}
        _MapBounds ("Map bounds XZ", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float _ShowWeights;
            float4 _MapBounds;
            TEXTURE2D(_DensityMap); SAMPLER(sampler_DensityMap);
            TEXTURE2D(_RoadMap); SAMPLER(sampler_RoadMap);
            struct Attributes { float3 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 world : TEXCOORD0; float3 normal : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.world = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.world);
                output.normal = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float light = .65 + .35 * saturate(dot(normalize(input.normal), normalize(float3(.4,1,.3))));
                if (_ShowWeights > .5)
                {
                    float2 uv=(input.world.xz-_MapBounds.xy)/_MapBounds.zw;
                    float weight=saturate(SAMPLE_TEXTURE2D(_DensityMap,sampler_DensityMap,uv).r);
                    return half4(half3(weight,1-weight,0) * light,1);
                }
                float2 roadUV=(input.world.xz-_MapBounds.xy)/_MapBounds.zw;
                float road=SAMPLE_TEXTURE2D(_RoadMap,sampler_RoadMap,roadUV).r;
                return half4(lerp(half3(.38,.40,.23),half3(.76,.60,.41),road) * light, 1);
            }
            ENDHLSL
        }
    }
}
