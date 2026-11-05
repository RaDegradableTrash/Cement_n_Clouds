using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Voyage.Lighting
{
    public sealed class VoyageLookStudio : MonoBehaviour
    {
        public Camera reviewCamera;
        public Transform[] viewpoints;
        public VoyageLookProfile[] profiles;
        public Light[] practicalLights;
        public bool showPanel=true;
        public Animator[] actors;
        public AnimationClip idlePose, walkingMotion;
        [Header("Presentation camera only")]
        public bool presentationDepthOfField;
        [Range(1,32)] public float aperture=8;
        VolumeProfile presentationProfile;
        DepthOfField depthOfField;
        bool animate;
        float motionTime;
        public int Selected { get; private set; }
        bool lampsOn=true;
        IEnumerator Start()
        {
            yield return null;
            if(DayNightSystem.Instance!=null) { DayNightSystem.Instance.advanceTime=false; DayNightSystem.Instance.SetTime(22); }
            // Scene startup must not overwrite a persistent manual look choice.
            reviewCamera.transform.SetPositionAndRotation(viewpoints[0].position,viewpoints[0].rotation);
            presentationProfile=ScriptableObject.CreateInstance<VolumeProfile>();
            depthOfField=presentationProfile.Add<DepthOfField>(true);
            var volume=gameObject.AddComponent<Volume>();
            volume.isGlobal=true; volume.priority=60; volume.sharedProfile=presentationProfile;
        }
        public void Select(int index)
        {
            Selected=Mathf.Clamp(index,0,viewpoints.Length-1);
            reviewCamera.transform.SetPositionAndRotation(viewpoints[Selected].position,viewpoints[Selected].rotation);
            if(VoyageLookController.Instance!=null) VoyageLookController.Instance.SetLook(profiles[Selected],.7f);
        }
        public void ToggleLights()
        {
            lampsOn=!lampsOn;
            foreach(var lamp in practicalLights) if(lamp!=null) lamp.enabled=lampsOn;
        }
        public void SetAnimation(bool value)
        {
            animate=value; motionTime=0;
            if(!animate && idlePose!=null) foreach(var actor in actors) idlePose.SampleAnimation(actor.gameObject,0);
        }
        void Update()
        {
            if(depthOfField!=null)
            {
                depthOfField.mode.value=presentationDepthOfField?DepthOfFieldMode.Bokeh:DepthOfFieldMode.Off;
                depthOfField.aperture.value=aperture;
                depthOfField.focalLength.value=45;
                depthOfField.focusDistance.value=Vector3.Distance(reviewCamera.transform.position,actors[Selected].transform.position+Vector3.up*1.5f);
            }
            if(!animate || walkingMotion==null) return;
            motionTime+=Time.deltaTime;
            foreach(var actor in actors) walkingMotion.SampleAnimation(actor.gameObject,Mathf.Repeat(motionTime,walkingMotion.length));
        }
        void OnDestroy()
        {
            if(presentationProfile==null) return;
            foreach(var component in presentationProfile.components) Destroy(component);
            Destroy(presentationProfile);
        }
        public void Capture()
        {
            string folder=Path.Combine(Application.persistentDataPath,"LookCaptures");
            Directory.CreateDirectory(folder);
            var rt=RenderTexture.GetTemporary(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var active=RenderTexture.active;
            Texture2D texture=null;
            try
            {
                RenderPipeline.SubmitRenderRequest(reviewCamera,new RenderPipeline.StandardRequest { destination=rt });
                RenderTexture.active=rt;
                texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                texture.ReadPixels(new Rect(0,0,1920,1080),0,0); texture.Apply();
                string path=Path.Combine(folder,profiles[Selected].name+"-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".png");
                File.WriteAllBytes(path,texture.EncodeToPNG()); Debug.Log("LOOK CAPTURE // "+path);
            }
            finally { RenderTexture.active=active; RenderTexture.ReleaseTemporary(rt); if(texture!=null) Destroy(texture); }
        }
        void OnGUI()
        {
            if(!showPanel) return;
            GUILayout.BeginArea(new Rect(18,18,235,270),GUI.skin.box);
            GUILayout.Label("VOYAGE / LIGHT STUDY");
            string[] names={"01  Cold dusk","02  Warm interior","03  Dramatic anime"};
            for(int i=0;i<profiles.Length;i++) if(GUILayout.Button(names[i])) Select(i);
            if(GUILayout.Button(lampsOn?"Practical lights: ON":"Practical lights: OFF")) ToggleLights();
            if(GUILayout.Button(animate?"Motion: ON":"Motion: OFF")) SetAnimation(!animate);
            if(GUILayout.Button(presentationDepthOfField?"Presentation DOF: ON":"Presentation DOF: OFF")) presentationDepthOfField=!presentationDepthOfField;
            if(GUILayout.Button("Capture 1920 x 1080")) Capture();
            GUILayout.Label("Inspector / SetLook: blend any preset");
            GUILayout.EndArea();
        }
    }
}
