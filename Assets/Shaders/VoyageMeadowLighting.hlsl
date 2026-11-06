#ifndef VOYAGE_MEADOW_LIGHTING
#define VOYAGE_MEADOW_LIGHTING
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
float4 _VoyageGrassEnvironmentColor;
float _VoyageGrassEnvironmentLight;
float4 _VoyageLookShape;
float4 _VoyageLookShadow;
float3 VoyageGroundAlbedo(float3 position)
{
    // Shared continuous world-space palette: grass roots and terrain agree at
    // every LOD, without the old uniform golden multiplier.
    float variation = sin(position.x*.017 + sin(position.z*.013)*2)*.10
        + sin(position.z*.047-position.x*.009)*.045;
    float3 palette=lerp(float3(.43,.39,.29),float3(.235,.255,.235),_VoyageLookShape.w);
    return palette * (1+variation);
}
float3 VoyageSurfaceLight(float3 albedo, float3 position, float3 normal)
{
    Light key = GetMainLight(TransformWorldToShadowCoord(position));
    // Indirect grass draws do not receive the per-renderer SH probe state used
    // by mesh renderers. Use the shared scene's tri-light environment for both.
    float3 ambient = lerp(unity_AmbientEquator.rgb,
        normal.y>=0 ? unity_AmbientSky.rgb : unity_AmbientGround.rgb,abs(normal.y));
    ambient = max(ambient,0) * lerp(float3(1,1,1),_VoyageLookShadow.rgb,_VoyageLookShape.w*.35);
    float ndl=dot(normal,key.direction);
    float wrap=lerp(.18,.04,_VoyageLookShape.w);
    float diffuse=saturate(ndl*(1-wrap)+wrap);
    float band=smoothstep(_VoyageLookShape.x-max(.05,_VoyageLookShape.y),_VoyageLookShape.x+max(.05,_VoyageLookShape.y),ndl);
    diffuse=lerp(diffuse,band*.8+saturate(ndl)*.2,_VoyageLookShape.w*.25);
    float3 light = ambient + key.color * diffuse * key.shadowAttenuation;
    #if defined(_ADDITIONAL_LIGHTS)
    InputData inputData=(InputData)0;
    inputData.positionWS=position;
    float4 projected=ComputeScreenPos(TransformWorldToHClip(position));
    inputData.normalizedScreenSpaceUV=projected.xy/projected.w;
    #if USE_CLUSTER_LIGHT_LOOP
    [loop] for(uint lightIndex=0;lightIndex<min(URP_FP_DIRECTIONAL_LIGHTS_COUNT,MAX_VISIBLE_LIGHTS);lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
        Light lamp=GetAdditionalLight(lightIndex,position,half4(1,1,1,1));
        light+=lamp.color*lamp.distanceAttenuation*lamp.shadowAttenuation*saturate(dot(normal,lamp.direction));
    }
    #endif
    uint count=GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(count)
        Light lamp=GetAdditionalLight(lightIndex,position,half4(1,1,1,1));
        light+=lamp.color*lamp.distanceAttenuation*lamp.shadowAttenuation*saturate(dot(normal,lamp.direction));
    LIGHT_LOOP_END
    #endif
    // One multiplier for both surfaces; no independent unlit grass brightness floor.
    return albedo * lerp(.78,1,saturate(normal.y)) * light * max(_VoyageGrassEnvironmentColor.rgb,.01) * max(_VoyageGrassEnvironmentLight,.01);
}
#endif
