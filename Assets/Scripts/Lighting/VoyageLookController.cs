using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace Voyage.Lighting
{
    /// <summary>Owns look parameters and a private Volume; the clock and fog retain ownership of RenderSettings.</summary>
    [DefaultExecutionOrder(-200), DisallowMultipleComponent]
    public sealed class VoyageLookController : MonoBehaviour
    {
        public static VoyageLookController Instance { get; private set; }
        public static bool UsesPointer => Instance!=null && Instance.showPanel;
        public VoyageLookProfile defaultProfile;
        [SerializeField] VoyageLookProfile manualProfile;
        VoyageLookProfile sceneProfile, selected;
        public VoyageLookProfile ActiveProfile => selected;
        public LookSettings Current { get; private set; } = LookSettings.Default;
        public static LookSettings Settings => Instance != null ? Instance.Current : LookSettings.Default;
        LookSettings from;
        float elapsed, duration;
        Volume volume;
        VolumeProfile volumeProfile;
        ColorAdjustments grading;
        Bloom bloom;
        Vignette vignette;
        VoyageLookProfile[] presets;
        bool showPanel;

        void OnEnable() => EnsureInitialized();
        public void EnsureInitialized()
        {
            if (Instance != null && Instance != this) { enabled=false; return; }
            if(Instance==this && volumeProfile!=null) return;
            Instance=this;
            if(volumeProfile!=null) { ResolveScene(); Select(0); Publish(); return; }
            if (defaultProfile == null) defaultProfile=Resources.Load<VoyageLookProfile>("Looks/ColdDusk");
            presets=new[] { defaultProfile, Resources.Load<VoyageLookProfile>("Looks/WarmInterior"), Resources.Load<VoyageLookProfile>("Looks/DramaticAnime") };
            volumeProfile=ScriptableObject.CreateInstance<VolumeProfile>();
            volumeProfile.name="Voyage runtime look (private)";
            grading=volumeProfile.Add<ColorAdjustments>(true);
            bloom=volumeProfile.Add<Bloom>(true);
            bloom.threshold.value=1.1f; bloom.scatter.value=.45f;
            vignette=volumeProfile.Add<Vignette>(true); vignette.smoothness.value=.5f;
            volumeProfile.Add<Tonemapping>(true).mode.value=TonemappingMode.ACES;
            volumeProfile.Add<MotionBlur>(true).intensity.value=0;
            volumeProfile.Add<DepthOfField>(true).mode.value=DepthOfFieldMode.Off;
            var child=new GameObject("Voyage Look Volume"); child.transform.SetParent(transform,false);
            volume=child.AddComponent<Volume>(); volume.isGlobal=true; volume.priority=50; volume.sharedProfile=volumeProfile;
            SceneManager.sceneLoaded+=OnSceneLoaded;
            SceneManager.activeSceneChanged+=OnActiveSceneChanged;
            ResolveScene(); Select(0); Publish();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ResolveScene();
        void OnActiveSceneChanged(Scene before, Scene after) => ResolveScene();
        void ResolveScene()
        {
            sceneProfile=null;
            if(!SceneManager.GetActiveScene().isLoaded) { Select(0); return; }
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var settings=root.GetComponentInChildren<VoyageLookScene>();
                if(settings!=null) { sceneProfile=settings.profile; break; }
            }
            Select(.7f);
        }

        public void SetLook(VoyageLookProfile profile, float blendSeconds=.7f)
        {
            manualProfile=profile; Select(blendSeconds);
        }
        public void ClearManualLook(float blendSeconds=.7f) => SetLook(null,blendSeconds);
        void Select(float seconds)
        {
            selected=manualProfile != null ? manualProfile : sceneProfile != null ? sceneProfile : defaultProfile;
            from=Current; elapsed=0; duration=Mathf.Max(0,seconds);
            if(duration==0)
            {
                Current=selected != null ? selected.settings : LookSettings.Default;
                if(grading!=null) Publish();
            }
        }
        void Update()
        {
            if(Keyboard.current!=null && Keyboard.current.f7Key.wasPressedThisFrame) showPanel=!showPanel;
            var requested=manualProfile != null ? manualProfile : sceneProfile != null ? sceneProfile : defaultProfile;
            if (requested != selected) Select(.7f);
            elapsed+=Time.unscaledDeltaTime;
            float t=duration>0 ? Mathf.SmoothStep(0,1,elapsed/duration) : 1;
            Current=LookSettings.Lerp(from,selected != null ? selected.settings : LookSettings.Default,t);
            Publish();
        }
        void Publish()
        {
            var s=Current;
            grading.postExposure.value=s.exposure; grading.saturation.value=s.saturation;
            grading.contrast.value=s.contrast; grading.colorFilter.value=s.filter;
            bloom.intensity.value=s.bloom; vignette.intensity.value=s.vignette;
            Shader.SetGlobalVector("_VoyageLookShape",new Vector4(s.threshold,Mathf.Max(.01f,s.transition),s.rim,1));
            Shader.SetGlobalColor("_VoyageLookShadow",s.shadow);
        }
        void OnDisable()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded-=OnSceneLoaded;
            SceneManager.activeSceneChanged-=OnActiveSceneChanged;
            Instance=null;
            Shader.SetGlobalVector("_VoyageLookShape",Vector4.zero);
            Shader.SetGlobalColor("_VoyageLookShadow",Color.white);
            if(volume!=null) Release(volume.gameObject);
            if(volumeProfile!=null)
            {
                foreach(var component in volumeProfile.components) Release(component);
                Release(volumeProfile);
            }
        }
        static void Release(Object value) { if(Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
        void OnGUI()
        {
            if(!showPanel) return;
            GUILayout.BeginArea(new Rect(18,260,235,170),GUI.skin.box);
            GUILayout.Label("VOYAGE / LOOK  [F7]");
            string[] titles={"Cold dusk","Warm interior","Dramatic anime"};
            for(int i=0;i<presets.Length;i++) if(presets[i]!=null && GUILayout.Button(titles[i])) SetLook(presets[i]);
            if(GUILayout.Button("Scene default")) ClearManualLook();
            GUILayout.EndArea();
        }
    }
}
