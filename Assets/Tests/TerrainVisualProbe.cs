#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Profiling;
using Voyage.TerrainSystem;
using Voyage.Lighting;
using GrassFlow;

namespace Voyage.Tests
{
    // Opt-in Play Mode diagnosis. Never saves or edits scene/paint assets.
    public sealed class TerrainVisualProbe : MonoBehaviour
    {
        const string Folder="Logs/TerrainVisualProbe";
        Camera camera;
        FollowCamera follow;
        Vector3 oldPosition;
        Quaternion oldRotation;
        float oldTime;
        bool oldAdvance;
        readonly List<string> report=new List<string>();
        void Log(string line){report.Add(line);File.WriteAllLines(Folder+"/report.txt",report);}
        IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);
            camera=Camera.main;follow=camera.GetComponent<FollowCamera>();
            oldPosition=camera.transform.position;oldRotation=camera.transform.rotation;
            if(follow!=null)follow.enabled=false;
            var clock=DayNightSystem.Instance;oldTime=clock.currentTime;oldAdvance=clock.advanceTime;clock.advanceTime=false;clock.SetTime(22);
            var car=DrivingCore.Instance.Player.GetComponent<Rigidbody>();var oldConstraints=car.constraints;car.constraints=RigidbodyConstraints.FreezeAll;
            Vector3 focus=DrivingCore.Instance.Player.transform.position;
            Log("Player "+focus+" GPU "+SystemInfo.graphicsDeviceName);
            // Reproduce the low-sun triangle seam beside the dialogue NPC.
            var npc=FindAnyObjectByType<Voyage.Dialogue.CoBluesDialogue>();
            focus=npc.transform.position+new Vector3(-6,0,3);
            camera.transform.position=focus+new Vector3(0,13,-5);
            camera.transform.LookAt(focus);
            Log("Near-NPC focus "+focus+"; 1400x1400 render target; 180 frames per sample, captures excluded.");
            yield return new WaitForSecondsRealtime(10);
            clock.SetTime(17.7f);
            foreach(var t in FindObjectsByType<TerrainTileRuntime>().Where(t=>t.Bounds.SqrDistance(focus)<150*150))
                Log("Tile "+t.Coordinate+" lod "+t.CurrentLod+" position "+t.transform.position);
            foreach(var g in FindObjectsByType<GrassFlowRenderer>())
                Log("Grass "+g.name+" bounds "+g.patch?.bounds+" surface "+g.patch?.surface?.width+" ready "+g.Ready);
            yield return Sample("dusk-grass");
            var clouds=Resources.FindObjectsOfTypeAll<Voyage.Rendering.Clouds.VolumetricCloudFeature>().First(f=>f.isActive);
            int oldSlices=clouds.settings.reflectionSlices;
            try { clouds.settings.reflectionSlices=1;yield return Sample("dusk-full-reflection"); }
            finally { clouds.settings.reflectionSlices=oldSlices; }
            clock.SetTime(6.3f);yield return Sample("dawn-grass");
            clock.SetTime(12);
            yield return Sample("day-grass");
            Log("FINISHED");
            clock.SetTime(oldTime);clock.advanceTime=oldAdvance;
            camera.transform.SetPositionAndRotation(oldPosition,oldRotation);if(follow!=null)follow.enabled=true;
            car.constraints=oldConstraints;
        }
        IEnumerator Sample(string label)
        {
            var target=RenderTexture.GetTemporary(1400,1400,24);var previous=camera.targetTexture;var active=RenderTexture.active;
            camera.targetTexture=target;
            yield return new WaitForSecondsRealtime(1);
            var times=new List<float>();var main=new List<double>();
            var names=new[]{"PlayerLoop","Gfx.WaitForPresentOnGfxThread","GfxDeviceD3D12.WaitForLastPresentation.WaitForGPU","GC.Alloc","Voyage.Terrain.StreamUpdate","Voyage.Grass.ContactUpdate","Voyage.Grass.Prepare"};
            var handles=new List<Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle>();Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetAvailable(handles);
            var recorders=new List<ProfilerRecorder>();var totals=new double[names.Length];
            foreach(var name in names){var category=ProfilerCategory.Scripts;foreach(var h in handles){var d=Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetDescription(h);if(d.Name==name){category=d.Category;break;}}recorders.Add(ProfilerRecorder.StartNew(category,name,1));}
            using(var recorder=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1))
            {
                for(int i=0;i<180;i++){yield return null;times.Add(Time.unscaledDeltaTime*1000);if(recorder.Valid)main.Add(recorder.LastValue/1e6);for(int j=0;j<recorders.Count;j++)if(recorders[j].Valid)totals[j]+=recorders[j].LastValue;}
            }
            times.Sort();main.Sort();Log(label+" frame median="+times[90]+" p95="+times[171]+" max="+times[179]+" main="+(main.Count>0?main[main.Count/2]:0));
            for(int j=0;j<recorders.Count;j++){Log(names[j]+" average raw="+totals[j]/180);recorders[j].Dispose();}
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;
            var image=new Texture2D(1400,1400,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1400,1400),0,0);image.Apply();
            File.WriteAllBytes(Folder+"/"+label+".png",image.EncodeToPNG());
            var feature=Resources.FindObjectsOfTypeAll<Voyage.Rendering.Clouds.VolumetricCloudFeature>().FirstOrDefault(f=>f.isActive);
            var flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
            var pass=feature?.GetType().GetField("cloudPass",flags).GetValue(feature);
            var histories=pass?.GetType().GetField("histories",flags).GetValue(pass) as IDictionary;
            var history=histories?[camera];
            var panorama=(history?.GetType().GetField("reflection",flags).GetValue(history) as RTHandle)?.rt;
            if(panorama!=null)
            {
                RenderTexture.active=panorama;
                var reflectionImage=new Texture2D(panorama.width,panorama.height,TextureFormat.RGBAFloat,false,true);
                reflectionImage.ReadPixels(new Rect(0,0,panorama.width,panorama.height),0,0);reflectionImage.Apply();
                var before=reflectionImage.GetPixels();
                if(feature!=null)
                {
                    int slices=feature.settings.reflectionSlices;feature.settings.reflectionSlices=1;
                    try { RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target}); }
                    finally { feature.settings.reflectionSlices=slices; }
                    RenderTexture.active=panorama;
                    reflectionImage.ReadPixels(new Rect(0,0,panorama.width,panorama.height),0,0);reflectionImage.Apply();
                    var after=reflectionImage.GetPixels();double error=0;float maxError=0;
                    for(int i=0;i<before.Length;i++){float e=Mathf.Abs(before[i].r-after[i].r)+Mathf.Abs(before[i].g-after[i].g)+Mathf.Abs(before[i].b-after[i].b)+Mathf.Abs(before[i].a-after[i].a);error+=e;maxError=Mathf.Max(maxError,e);}
                    Log(label+" reflection mean RGBA error="+error/(before.Length*4)+" max channel-sum="+maxError);
                    Log((error/(before.Length*4)<.01?"PASS ":"FAIL ")+label+" sliced panorama matches full refresh within 1% mean RGBA error");
                }
                var preview=new Texture2D(panorama.width,panorama.height,TextureFormat.RGBA32,false);preview.SetPixels(before);preview.Apply();
                File.WriteAllBytes(Folder+"/"+label+"-panorama.png",preview.EncodeToPNG());Destroy(preview);Destroy(reflectionImage);
            }
            else Log("FAIL "+label+" reflection panorama missing");
            camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);Destroy(image);
        }
    }
}
#endif
