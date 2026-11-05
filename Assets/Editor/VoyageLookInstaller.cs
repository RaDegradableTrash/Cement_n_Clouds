using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Voyage.Lighting;

public static class VoyageLookInstaller
{
    const string Root="Assets/Resources/Looks";
    const string Stage="Assets/LookDevelopment";
    static readonly List<Light> lamps=new List<Light>();
    static readonly List<Material> materials=new List<Material>();

    [MenuItem("Voyage/Look Development/Install profiles and studio")]
    public static void Install()
    {
        Directory.CreateDirectory(Root); Directory.CreateDirectory(Stage);
        AssetDatabase.Refresh();
        var cold=LookSettings.Default;
        var warm=cold; warm.saturation=-16; warm.exposure=.25f; warm.contrast=12; warm.ambient=.8f;
        warm.shadow=new Color(.9f,.83f,.75f); warm.fogTint=new Color(1,.9f,.76f); warm.transition=.3f; warm.bloom=.23f;
        var anime=cold; anime.saturation=-12; anime.exposure=.1f; anime.contrast=18; anime.transition=.045f; anime.threshold=.42f;
        anime.ambient=.65f; anime.rim=.2f; anime.bloom=.12f; anime.shadow=new Color(.74f,.74f,.85f);
        var profiles=new[]{SaveProfile("ColdDusk",cold),SaveProfile("WarmInterior",warm),SaveProfile("DramaticAnime",anime)};
        var shader=Shader.Find("Voyage/Character/Layered");
        if(shader==null) throw new System.InvalidOperationException("Character shader unavailable");
        foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/VoyageLullaby"}))
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            material.shader=shader;
            CoreUtils.SetKeyword(material,"_NORMALMAP",material.GetTexture("_BumpMap")!=null);
            CoreUtils.SetKeyword(material,"_ALPHATEST_ON",material.GetFloat("_AlphaClip")>.5f);
            EditorUtility.SetDirty(material);
        }
        var rp=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        var serialized=new SerializedObject(rp);
        serialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue=true;
        serialized.FindProperty("m_AdditionalLightsPerObjectLimit").intValue=8;
        serialized.FindProperty("m_AdditionalLightsShadowmapResolution").intValue=2048;
        serialized.FindProperty("m_ColorGradingMode").intValue=1;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        foreach(var feature in renderer.rendererFeatures)
            if(feature!=null && feature.GetType().Name=="ScreenSpaceAmbientOcclusion")
            {
                feature.SetActive(true);
                var ao=new SerializedObject(feature);
                ao.FindProperty("m_Settings.Downsample").boolValue=true;
                ao.FindProperty("m_Settings.Intensity").floatValue=.28f;
                ao.FindProperty("m_Settings.DirectLightingStrength").floatValue=.12f;
                ao.FindProperty("m_Settings.Radius").floatValue=.18f;
                ao.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(feature);
            }
        CreateStudio(profiles);
        var monitor=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Monitor.mat");
        if(monitor==null) { monitor=new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(monitor,Root+"/Monitor.mat"); }
        monitor.SetColor("_BaseColor",new Color(.045f,.065f,.075f));
        EditorUtility.SetDirty(monitor);
        var vehicle=Resources.Load<GameObject>("Prefabs/RV1.0");
        foreach(var rendererPart in vehicle.GetComponentsInChildren<Renderer>(true))
            foreach(var material in rendererPart.sharedMaterials)
                if(material!=null && (material.shader==null || !material.shader.isSupported || material.shader.name.Contains("Error")))
                    Debug.LogWarning("LOOK MATERIAL // "+rendererPart.name+" // "+AssetDatabase.GetAssetPath(material)+" // "+material.name+" // "+material.shader);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("LOOK INSTALL // profiles, character materials, PC lighting and studio saved");
    }

    static VoyageLookProfile SaveProfile(string name,LookSettings settings)
    {
        string path=Root+"/"+name+".asset";
        var profile=AssetDatabase.LoadAssetAtPath<VoyageLookProfile>(path);
        if(profile==null) { profile=ScriptableObject.CreateInstance<VoyageLookProfile>(); AssetDatabase.CreateAsset(profile,path); }
        profile.settings=settings; EditorUtility.SetDirty(profile); return profile;
    }

    static Material Mat(string name,Color color,float smooth=0,float metal=0,Color? emission=null)
    {
        string path=Stage+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null) { m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
        m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",smooth); m.SetFloat("_Metallic",metal);
        if(emission.HasValue)
        {
            m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",emission.Value);
        }
        materials.Add(m); EditorUtility.SetDirty(m); return m;
    }
    static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material mat)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube); o.name=name; o.transform.SetParent(parent,false);
        o.transform.localPosition=position; o.transform.localScale=size; o.GetComponent<Renderer>().sharedMaterial=mat;
        if(mat.IsKeywordEnabled("_EMISSION")) o.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        return o;
    }
    static Light Lamp(Transform parent,string name,Vector3 position,Vector3 target,Color color,float intensity,float angle=70)
    {
        var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=position;
        go.transform.LookAt(parent.TransformPoint(target));
        var light=go.AddComponent<Light>(); light.type=LightType.Spot; light.color=color;
        light.intensity=intensity; light.range=14; light.spotAngle=angle; light.innerSpotAngle=angle*.65f;
        light.shadows=LightShadows.Soft; light.shadowBias=.05f; light.shadowNormalBias=.15f;
        var data=light.GetUniversalAdditionalLightData(); data.usePipelineSettings=false;
        lamps.Add(light); return light;
    }
    static void CreateStudio(VoyageLookProfile[] profiles)
    {
        lamps.Clear(); materials.Clear();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("VOYAGE // LOOK DEVELOPMENT");
        root.AddComponent<VoyageLookScene>().profile=profiles[0];
        var studio=root.AddComponent<VoyageLookStudio>(); studio.profiles=profiles;
        var camera=new GameObject("Main Camera").AddComponent<Camera>(); camera.tag="MainCamera";
        camera.fieldOfView=35; camera.nearClipPlane=.05f; camera.farClipPlane=100;
        camera.allowHDR=true; camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        camera.gameObject.AddComponent<AudioListener>();
        camera.GetUniversalAdditionalCameraData().antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        studio.reviewCamera=camera; studio.viewpoints=new Transform[3];
        studio.actors=new Animator[3];
        var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/VoyageLullaby/VoyageLullaby.fbx").OfType<AnimationClip>().ToArray();
        studio.idlePose=clips.First(c=>c.name=="VOY_Idle"); studio.walkingMotion=clips.First(c=>c.name=="VOY_Walk");
        var plaster=Mat("Charcoal plaster",new Color(.16f,.18f,.20f),.2f);
        var warmWall=Mat("Ochre plaster",new Color(.38f,.24f,.15f),.12f);
        var black=Mat("Black fabric swatch",new Color(.025f,.03f,.04f),.12f);
        var white=Mat("Ivory swatch",new Color(.72f,.69f,.61f),.25f);
        var wet=Mat("Wet slate",new Color(.07f,.1f,.12f),.88f,.25f);
        var brass=Mat("Brushed brass",new Color(.43f,.29f,.12f),.7f,.85f);
        var wood=Mat("Walnut",new Color(.16f,.075f,.035f),.4f);
        var glow=Mat("Amber light",Color.white,0,0,new Color(4,1.6f,.5f));
        var blueGlow=Mat("Cool light",Color.white,0,0,new Color(.7f,2,3));
        var normal=CreateSurfaceNormal();
        foreach(var m in new[]{wet,plaster,warmWall,wood})
        {
            m.SetTexture("_BumpMap",normal); m.SetFloat("_BumpScale",m==wet?.22f:.13f);
            m.SetTextureScale("_BaseMap",new Vector2(4,4)); m.EnableKeyword("_NORMALMAP");
        }
        for(int i=0;i<3;i++)
        {
            var set=new GameObject(new[]{"Cold street","Window room","Chiaroscuro stage"}[i]).transform;
            set.SetParent(root.transform); set.position=new Vector3(i*30,100,0);
            Box(set,"Floor",new Vector3(0,-.1f,0),new Vector3(12,.2f,14),i==1?wood:wet);
            if(i!=1) Box(set,"Backdrop",new Vector3(0,2,4),new Vector3(12,4.2f,.2f),plaster);
            else
            {
                Box(set,"Window wall right",new Vector3(2.3f,2,4),new Vector3(7.4f,4.2f,.2f),warmWall);
                Box(set,"Window wall left",new Vector3(-4.9f,2,4),new Vector3(2.2f,4.2f,.2f),warmWall);
                Box(set,"Window header",new Vector3(-2.6f,3.75f,4),new Vector3(2.4f,.8f,.2f),warmWall);
                Box(set,"Window sill",new Vector3(-2.6f,.3f,4),new Vector3(2.4f,.6f,.2f),warmWall);
            }
            Box(set,"Left wall",new Vector3(-4,2,0),new Vector3(.2f,4,8),plaster);
            Box(set,"Right wall",new Vector3(4,2,0),new Vector3(.2f,4,8),plaster);
            var actor=(GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Prefabs/ExplorerPlayer"));
            actor.name="Lullaby / existing rig"; actor.transform.SetParent(set,false); actor.transform.localRotation=Quaternion.Euler(0,165,0);
            foreach(var behaviour in actor.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled=false;
            foreach(var collider in actor.GetComponentsInChildren<Collider>(true)) collider.enabled=false;
            var animator=actor.GetComponentInChildren<Animator>(); animator.applyRootMotion=false;
            studio.actors[i]=animator;
            var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/VoyageLullaby/VoyageLullaby.fbx").OfType<AnimationClip>().First(c=>c.name=="VOY_Idle");
            clip.SampleAnimation(animator.gameObject,0);
            // Keep pose fixed for comparable material and lighting captures.
            animator.enabled=false;
            for(int s=0;s<3;s++)
            {
                var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere.name="Material witness "+s;
                sphere.transform.SetParent(set,false); sphere.transform.localPosition=new Vector3(1.5f+s*.6f,.6f,1.3f);
                sphere.transform.localScale=Vector3.one*.45f;
                sphere.GetComponent<Renderer>().sharedMaterial=new[]{black,white,brass}[s];
                Box(set,"Plinth",new Vector3(1.5f+s*.6f,.2f,1.3f),new Vector3(.5f,.4f,.5f),plaster);
            }
            if(i==0)
            {
                for(int z=0;z<6;z++) for(int x=0;x<8;x++)
                    Box(set,"Paving joint",new Vector3(-3.5f+x,-.002f,-2+z),new Vector3(.975f,.012f,.975f),wet);
                for(int n=0;n<5;n++)
                {
                    Box(set,"Facade pilaster",new Vector3(-3.6f+n*1.8f,1.8f,3.8f),new Vector3(.18f,3.6f,.3f),black);
                    Box(set,"Window",new Vector3(-2.8f+n*1.7f,2.5f,3.85f),new Vector3(.7f,.9f,.06f),n%2==0?glow:blueGlow);
                }
                Lamp(set,"Cool sky side",new Vector3(-3,3,-2),new Vector3(0,1,0),new Color(.65f,.77f,.9f),18,85);
                Lamp(set,"Warm practical rim",new Vector3(2,2.8f,2),new Vector3(0,1,0),new Color(1,.72f,.49f),24,75);
                Lamp(set,"Street diffuse fill",new Vector3(0,3,-3),new Vector3(0,1,0),new Color(.72f,.8f,.9f),3,100);
                Box(set,"Lamp post",new Vector3(2.5f,1.4f,2),new Vector3(.08f,2.8f,.08f),brass);
                Box(set,"Lantern",new Vector3(2.5f,2.8f,2),new Vector3(.22f,.4f,.22f),glow);
            }
            else if(i==1)
            {
                Box(set,"Window luminous pane",new Vector3(-2.6f,2,3.8f),new Vector3(2.4f,2.7f,.06f),glow);
                for(int n=0;n<4;n++) Box(set,"Window mullion",new Vector3(-3.7f+n*.75f,2,3.65f),new Vector3(.07f,2.8f,.1f),wood);
                Box(set,"Window crossbar",new Vector3(-2.6f,2,3.6f),new Vector3(2.5f,.08f,.15f),wood);
                Box(set,"Worktop",new Vector3(-2,.85f,1.4f),new Vector3(2.8f,.12f,1),wood);
                Lamp(set,"Window key",new Vector3(-2,3,4.4f),new Vector3(0,.8f,0),new Color(1,.82f,.61f),65,95);
                Lamp(set,"Room bounce",new Vector3(1,2,-3),new Vector3(0,1,0),new Color(.9f,.83f,.73f),5,95);
            }
            else
            {
                Lamp(set,"Hard side key",new Vector3(-2,2.8f,-2),new Vector3(0,1,0),new Color(1,.82f,.6f),42,55);
                Lamp(set,"Blue edge",new Vector3(2,2,1.5f),new Vector3(0,1,0),new Color(.5f,.55f,1),22,50);
            }
            // A broad, unshadowed bounce represents indirect light in this unbaked studio.
            // Keep it separate from the practical-light toggle so dark-material response
            // can be judged even with the key switched off.
            var bounce = new GameObject("Broad indirect bounce").AddComponent<Light>();
            bounce.transform.SetParent(set,false);
            bounce.transform.localPosition=new Vector3(0,2,-2);
            bounce.type=LightType.Point; bounce.range=13;
            bounce.intensity=i==2?2:i==1?5:3;
            bounce.color=i==1?new Color(1,.84f,.67f):new Color(.66f,.78f,1);
            bounce.shadows=LightShadows.None;
            var view=new GameObject("Review viewpoint "+i).transform; view.SetParent(set,false);
            view.localPosition=new Vector3(3,2,-7); view.LookAt(set.TransformPoint(new Vector3(0,1.45f,.3f)));
            studio.viewpoints[i]=view;
            var probe=new GameObject("Local reflection").AddComponent<ReflectionProbe>(); probe.transform.SetParent(set,false);
            probe.transform.localPosition=new Vector3(0,1.5f,0); probe.size=new Vector3(12,5,12);
            probe.mode=ReflectionProbeMode.Realtime; probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces; probe.resolution=128; probe.boxProjection=true;
        }
        studio.practicalLights=lamps.ToArray();
        camera.transform.SetPositionAndRotation(studio.viewpoints[0].position,studio.viewpoints[0].rotation);
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/LookDevelopment.unity");
    }
    static Texture2D CreateSurfaceNormal()
    {
        const int size=128;
        var texture=new Texture2D(size,size,TextureFormat.RGB24,false,true);
        for(int y=0;y<size;y++) for(int x=0;x<size;x++)
        {
            float u=x/(float)size*Mathf.PI*2, v=y/(float)size*Mathf.PI*2;
            // Periodic waves tile without a seam; two scales break up smooth specular highlights.
            float dx=Mathf.Cos(u*7+Mathf.Sin(v*3))*.24f+Mathf.Cos(u*23+v*11)*.09f;
            float dy=Mathf.Sin(v*9+Mathf.Cos(u*2))*.20f+Mathf.Sin(v*19-u*13)*.08f;
            var n=new Vector3(dx,dy,1).normalized;
            texture.SetPixel(x,y,new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f));
        }
        texture.Apply(); string path=Stage+"/SurfaceNormal.png"; File.WriteAllBytes(path,texture.EncodeToPNG()); Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.NormalMap; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
