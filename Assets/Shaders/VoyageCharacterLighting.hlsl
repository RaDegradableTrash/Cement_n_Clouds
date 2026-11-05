#ifndef VOYAGE_CHARACTER_LIGHTING
#define VOYAGE_CHARACTER_LIGHTING
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
float4 _VoyageLookShape; // threshold, softness, rim, enabled
float4 _VoyageLookShadow;

half3 VoyageCharacterDirect(BRDFData brdf, Light lamp, half3 normal, half3 view)
{
    half ndl=dot(normal,lamp.direction);
    half width=max(.01,_VoyageLookShape.y);
    half band=smoothstep(_VoyageLookShape.x-width,_VoyageLookShape.x+width,ndl);
    // The band shapes diffuse light only. Shadow maps and distance attenuation
    // retain their physical support, so a rim cannot glow through a wall.
    half diffuse=lerp(saturate(ndl),band*.82+saturate(ndl)*.18,_VoyageLookShape.w);
    half3 specular=0;
    #ifndef _SPECULARHIGHLIGHTS_OFF
    specular=DirectBRDFSpecular(brdf,normal,lamp.direction,view)*brdf.specular*saturate(ndl)*.65;
    #endif
    half rim=pow(1-saturate(dot(normal,view)),4)*smoothstep(-.1,.6,ndl)*_VoyageLookShape.z;
    return (brdf.diffuse*diffuse+specular+brdf.diffuse*rim)*lamp.color*lamp.distanceAttenuation*lamp.shadowAttenuation;
}

half4 VoyageCharacterFragment(InputData inputData, SurfaceData surfaceData)
{
    BRDFData brdf;
    InitializeBRDFData(surfaceData,brdf);
    BRDFData coat=CreateClearCoatBRDFData(surfaceData,brdf);
    half4 shadowMask=CalculateShadowMask(inputData);
    AmbientOcclusionFactor ao=CreateAmbientOcclusionFactor(inputData,surfaceData);
    Light main=GetMainLight(inputData,shadowMask,ao);
    #if !defined(LIGHTMAP_ON) && !defined(DYNAMICLIGHTMAP_ON)
    // Runtime-spawned rigs have no baked probe assignment. Use the same
    // authored tri-light environment as the streamed terrain in this case.
    half3 environment=lerp(unity_AmbientEquator.rgb,
        inputData.normalWS.y>=0?unity_AmbientSky.rgb:unity_AmbientGround.rgb,abs(inputData.normalWS.y));
    // A stylized indirect response keeps dark cloth readable at night without
    // adding light when the authored environment is black.
    inputData.bakedGI=lerp(inputData.bakedGI,environment*2.5,_VoyageLookShape.w);
    #endif
    MixRealtimeAndBakedGI(main,inputData.normalWS,inputData.bakedGI);
    // Compress only indirect diffuse reflectance: charcoal remains darker than
    // ivory, but its folds survive a low-key exposure. No emission or light floor.
    // Direct diffuse/specular keep the authored material response.
    BRDFData indirectBrdf=brdf;
    indirectBrdf.diffuse=lerp(brdf.diffuse,sqrt(max(brdf.diffuse,0)),.45*_VoyageLookShape.w);
    half3 gi=GlobalIllumination(indirectBrdf,coat,surfaceData.clearCoatMask,inputData.bakedGI,
        ao.indirectAmbientOcclusion,inputData.positionWS,inputData.normalWS,inputData.viewDirectionWS,inputData.normalizedScreenSpaceUV);
    half3 color=gi*lerp(half3(1,1,1),_VoyageLookShadow.rgb,_VoyageLookShape.w)+surfaceData.emission;
    uint layers=GetMeshRenderingLayer();
    if(IsMatchingLightLayer(main.layerMask,layers))
        color+=VoyageCharacterDirect(brdf,main,inputData.normalWS,inputData.viewDirectionWS);
    #if defined(_ADDITIONAL_LIGHTS)
    #if USE_CLUSTER_LIGHT_LOOP
    [loop] for(uint lightIndex=0;lightIndex<min(URP_FP_DIRECTIONAL_LIGHTS_COUNT,MAX_VISIBLE_LIGHTS);lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
        Light lamp=GetAdditionalLight(lightIndex,inputData,shadowMask,ao);
        if(IsMatchingLightLayer(lamp.layerMask,layers)) color+=VoyageCharacterDirect(brdf,lamp,inputData.normalWS,inputData.viewDirectionWS);
    }
    #endif
    uint count=GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(count)
        Light lamp=GetAdditionalLight(lightIndex,inputData,shadowMask,ao);
        if(IsMatchingLightLayer(lamp.layerMask,layers)) color+=VoyageCharacterDirect(brdf,lamp,inputData.normalWS,inputData.viewDirectionWS);
    LIGHT_LOOP_END
    #endif
    #ifdef _ADDITIONAL_LIGHTS_VERTEX
    color+=inputData.vertexLighting*brdf.diffuse;
    #endif
    return half4(min(color,HALF_MAX),surfaceData.alpha);
}

// Keep URP's vertex, normal mapping, alpha clipping, decals, fog and instancing.
#define UniversalFragmentPBR VoyageCharacterFragment
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
#undef UniversalFragmentPBR
#endif
