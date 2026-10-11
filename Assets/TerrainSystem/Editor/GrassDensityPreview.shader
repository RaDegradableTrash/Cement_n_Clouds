Shader "Hidden/Voyage/GrassDensityPreview"
{
    Properties
    {
        _DensityMap ("Density", 2D) = "white" {}
        _SurfaceMap ("Coverage", 2D) = "white" {}
        _PatchBounds ("World XZ bounds", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10" }
        Pass
        {
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_DensityMap); SAMPLER(sampler_DensityMap);
            TEXTURE2D(_SurfaceMap); SAMPLER(sampler_SurfaceMap);
            float4 _PatchBounds;
            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(world);
                output.uv = (world.xz - _PatchBounds.xy) / _PatchBounds.zw;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float density = saturate(SAMPLE_TEXTURE2D(_DensityMap, sampler_DensityMap, input.uv).r);
                float coverage = SAMPLE_TEXTURE2D(_SurfaceMap, sampler_SurfaceMap, input.uv).g;
                return half4(coverage < .99 ? half3(.15,.15,.15) : half3(density, 1.0-density, 0), 1);
            }
            ENDHLSL
        }
    }
}
