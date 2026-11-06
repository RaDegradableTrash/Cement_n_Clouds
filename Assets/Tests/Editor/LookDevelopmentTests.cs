using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Voyage.Tests.Editor
{
    public sealed class LookDevelopmentTests
    {
        static Type TypeOf(string name)=>Type.GetType("Voyage.Lighting."+name+", Assembly-CSharp",true);
        [Test]
        public void BlendKeepsBothEndpointsAndInterpolatesExposure()
        {
            var type=TypeOf("LookSettings");
            var a=type.GetProperty("Default").GetValue(null);
            var b=type.GetProperty("Default").GetValue(null);
            type.GetField("exposure").SetValue(a,-1f); type.GetField("exposure").SetValue(b,1f);
            var blend=type.GetMethod("Lerp");
            Assert.That(type.GetField("exposure").GetValue(blend.Invoke(null,new[]{a,b,(object)0f})),Is.EqualTo(-1f));
            Assert.That(type.GetField("exposure").GetValue(blend.Invoke(null,new[]{a,b,(object).5f})),Is.EqualTo(0f));
            Assert.That(type.GetField("exposure").GetValue(blend.Invoke(null,new[]{a,b,(object)1f})),Is.EqualTo(1f));
        }
        [Test]
        public void ManualChoiceOverridesSceneAndClearRestoresScene()
        {
            var scene=new GameObject("Look test scene"); var go=new GameObject("Look test controller"); go.SetActive(false);
            var sceneProfile=ScriptableObject.CreateInstance(TypeOf("VoyageLookProfile"));
            var manualProfile=ScriptableObject.CreateInstance(TypeOf("VoyageLookProfile"));
            try
            {
                var sceneType=TypeOf("VoyageLookScene"); var setting=scene.AddComponent(sceneType);
                sceneType.GetField("profile").SetValue(setting,sceneProfile);
                var type=TypeOf("VoyageLookController"); var controller=go.AddComponent(type); go.SetActive(true);
                type.GetField("sceneProfile",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(controller,sceneProfile);
                type.GetMethod("ClearManualLook").Invoke(controller,new object[]{0f});
                Assert.That(type.GetProperty("ActiveProfile").GetValue(controller),Is.SameAs(sceneProfile));
                type.GetMethod("SetLook").Invoke(controller,new object[]{manualProfile,0f});
                Assert.That(type.GetProperty("ActiveProfile").GetValue(controller),Is.SameAs(manualProfile));
                type.GetMethod("ClearManualLook").Invoke(controller,new object[]{0f});
                Assert.That(type.GetProperty("ActiveProfile").GetValue(controller),Is.SameAs(sceneProfile));
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(scene); Object.DestroyImmediate(sceneProfile); Object.DestroyImmediate(manualProfile); }
        }
        [Test]
        public void ReferenceVehicleHeadlightBindingUsesAuthoredFixtures()
        {
            var root=new GameObject("RV light fixture test");
            var lamps=new System.Collections.Generic.List<Light>();
            try
            {
                foreach(var fixture in new[]{"HeadLightL","HeadLightR","HeadLightFix","LightL","LightR","ReadingLightFix","BrakeLightR"})
                {
                    var parent=new GameObject(fixture);parent.transform.SetParent(root.transform);
                    var child=new GameObject("Spot Light");child.transform.SetParent(parent.transform);
                    var lamp=child.AddComponent<Light>();lamp.type=LightType.Spot;lamps.Add(lamp);
                }
                var type=TypeOf("VoyageVehicleLighting");var lighting=root.AddComponent(type);
                type.GetMethod("Bind").Invoke(lighting,null);
                for(int i=0;i<5;i++) Assert.That(lamps[i].enabled,Is.False);
                Assert.That(lamps[5].enabled,Is.True,"Reading light is independently authored");
                type.GetMethod("SetHeadlights").Invoke(lighting,new object[]{true});
                for(int i=0;i<5;i++) Assert.That(lamps[i].enabled,Is.True);
                Assert.That(lamps[0].shadows,Is.EqualTo(LightShadows.Soft));
                type.GetMethod("SetHeadlights").Invoke(lighting,new object[]{false});
                Assert.That(lamps[6].enabled,Is.True,"Headlight toggle must not take ownership of the brake lights");
                type.GetMethod("Bind").Invoke(lighting,null);
                Assert.That(((System.Collections.ICollection)type.GetProperty("Headlights").GetValue(lighting)).Count,Is.EqualTo(5),"Rebinding does not duplicate fixtures");
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test]
        public void StudioEmittersKeepEmissionAfterAssetImport()
        {
            foreach(var name in new[]{"Amber light","Cool light"})
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/LookDevelopment/"+name+".mat");
                Assert.That(material,Is.Not.Null);
                Assert.That(material.IsKeywordEnabled("_EMISSION"),Is.True,name);
                Assert.That((material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack)==0,Is.True,name);
            }
        }
        [Test]
        public void NightLookPublishesReadableEnvironmentAfterClockUpdate()
        {
            var root=new GameObject("Look environment regression");
            var camera=new GameObject("Look environment camera"); camera.AddComponent<Camera>();
            var sky=RenderSettings.ambientSkyColor; var equator=RenderSettings.ambientEquatorColor;
            var ground=RenderSettings.ambientGroundColor; var mode=RenderSettings.ambientMode;
            var probe=RenderSettings.ambientProbe;
            try
            {
                var controller=camera.AddComponent(TypeOf("VoyageLookController"));
                TypeOf("VoyageLookController").GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,null);
                root.SetActive(false);
                var clock=root.AddComponent(TypeOf("DayNightSystem"));
                TypeOf("DayNightSystem").GetMethod("SetTime").Invoke(clock,new object[]{22f});
                Assert.That(RenderSettings.ambientEquatorColor.r,Is.GreaterThan(.1f),"Look environment must not be overwritten by legacy clock colors");
                Assert.That(RenderSettings.ambientSkyColor.b,Is.GreaterThan(RenderSettings.ambientSkyColor.r));
            }
            finally
            {
                var controller=camera.GetComponent(TypeOf("VoyageLookController"));
                if(controller!=null) TypeOf("VoyageLookController").GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,null);
                Object.DestroyImmediate(root); Object.DestroyImmediate(camera);
                RenderSettings.ambientMode=mode; RenderSettings.ambientSkyColor=sky;
                RenderSettings.ambientEquatorColor=equator; RenderSettings.ambientGroundColor=ground; RenderSettings.ambientProbe=probe;
            }
        }
        [Test]
        public void SharedEnvironmentShadersHaveDepthNormalsAndNoCompileErrors()
        {
            foreach(var name in new[]{"Voyage/Terrain/Stylized","GrassFlow/Voyage/Grass"})
            {
                var shader=Shader.Find(name); Assert.That(shader,Is.Not.Null,name);
                var material=new Material(shader);
                try
                {
                    Assert.That(material.FindPass("DepthNormals"),Is.GreaterThanOrEqualTo(0),name);
                    foreach(var message in ShaderUtil.GetShaderMessages(shader))
                        Assert.That(message.severity.ToString(),Is.Not.EqualTo("Error"),message.message);
                }
                finally { Object.DestroyImmediate(material); }
            }
        }
        [Test]
        public void CharacterShaderPreservesAuxiliaryPassesAndCompiles()
        {
            var shader=Shader.Find("Voyage/Character/Layered"); Assert.That(shader,Is.Not.Null);
            var material=new Material(shader);
            try
            {
                foreach(var pass in new[]{"ForwardLit","ShadowCaster","DepthOnly","DepthNormals","Meta","MotionVectors"})
                    Assert.That(material.FindPass(pass),Is.GreaterThanOrEqualTo(0),pass);
                foreach(var message in ShaderUtil.GetShaderMessages(shader))
                    Assert.That(message.severity.ToString(),Is.Not.EqualTo("Error"),message.message);
            }
            finally { Object.DestroyImmediate(material); }
        }
    }
}
