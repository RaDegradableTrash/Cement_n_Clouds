#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Voyage.Lighting;

namespace Voyage.Tests
{
    public sealed class LookReviewProbe : MonoBehaviour
    {
        IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Random.InitState(1741);
            yield return new WaitForSecondsRealtime(25);
            if(FindAnyObjectByType<VoyageLookStudio>()==null)
            {
                float deadline=Time.realtimeSinceStartup+120;
                while((DrivingCore.Instance==null || DrivingCore.Instance.Player==null ||
                       DrivingCore.Instance.Player.GetComponent<CarControl>()==null) && Time.realtimeSinceStartup<deadline)
                    yield return null;
                if(DrivingCore.Instance==null || DrivingCore.Instance.Player==null)
                { Debug.LogError("LOOK CHECK // vehicle startup timed out"); EditorApplication.Exit(3); yield break; }
                yield return null;
            }
            var camera = Camera.main;
            if (camera == null) { Debug.LogError("Look review has no camera"); EditorApplication.Exit(2); yield break; }
            foreach (var script in camera.GetComponents<MonoBehaviour>())
                if (script is FollowCamera) script.enabled = false;
            foreach(var look in FindObjectsByType<VoyageLookController>(FindObjectsInactive.Include))
                Debug.Log("LOOK CONTROLLER // "+look.gameObject.name+" active="+look.gameObject.activeInHierarchy+" enabled="+look.enabled);
            var clock = DayNightSystem.Instance;
            clock.advanceTime = false;
            var label = SessionState.GetString("LookReviewLabel", "capture");
            Directory.CreateDirectory("Logs/LookReview/" + label);
            File.WriteAllText("Logs/LookReview/" + label + "/metrics.txt", "GPU: " + SystemInfo.graphicsDeviceName + "\n1920x1080 Editor Play Mode; 120 explicit rendered frames after 30 rendered warmup frames. Synchronized GPU readback included; not standalone FPS.\n");
            var studio=FindAnyObjectByType<VoyageLookStudio>();
            if(studio!=null)
            {
                studio.showPanel=false;
                clock.SetTime(22);
                for(int i=0;i<studio.profiles.Length;i++)
                {
                    studio.Select(i);
                    yield return Sample(camera,label,studio.profiles[i].name);
                    // A translated viewpoint catches view-dependent rims and unstable shadows.
                    camera.transform.Translate(.25f,0,0);
                    yield return Sample(camera,label,studio.profiles[i].name+"-offset");
                }
                studio.Select(0); studio.ToggleLights();
                yield return Sample(camera,label,"ColdDusk-lamps-off");
                studio.ToggleLights();
                studio.SetAnimation(true);
                yield return Sample(camera,label,"ColdDusk-motion");
                studio.SetAnimation(false);
                studio.presentationDepthOfField=true;
                yield return Sample(camera,label,"ColdDusk-presentation-dof");
                studio.presentationDepthOfField=false;
                var controller=VoyageLookController.Instance;
                controller.SetLook(studio.profiles[1],0);
                if(controller.ActiveProfile!=studio.profiles[1]) Debug.LogError("LOOK CHECK // manual priority failed");
                controller.ClearManualLook(0);
                if(controller.ActiveProfile!=studio.profiles[0]) Debug.LogError("LOOK CHECK // scene fallback failed");
                File.AppendAllText("Logs/LookReview/"+label+"/metrics.txt","Runtime profile priority and fallback checked.\n");
                controller.SetLook(studio.profiles[1],0);
                var expected=studio.profiles[1];
                DontDestroyOnLoad(gameObject);
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Assets/Scenes/LookDevelopment.unity");
                yield return null;
                if(VoyageLookController.Instance.ActiveProfile!=expected) Debug.LogError("LOOK CHECK // manual look lost on scene reload");
                VoyageLookController.Instance.ClearManualLook(0);
                File.AppendAllText("Logs/LookReview/"+label+"/metrics.txt","Scene reload and persistent manual profile checked.\n");
            }
            else
            {
                foreach(var body in FindObjectsByType<Rigidbody>())
                    if(!body.isKinematic) { body.linearVelocity=Vector3.zero; body.angularVelocity=Vector3.zero; body.constraints=RigidbodyConstraints.FreezeAll; }
                if(DrivingCore.Instance!=null && DrivingCore.Instance.Player!=null)
                {
                    var car=DrivingCore.Instance.Player.transform;
                    foreach(var renderer in car.GetComponentsInChildren<Renderer>(true))
                        foreach(var material in renderer.sharedMaterials)
                            if(material!=null && (renderer.name.Contains("Screen") || renderer.name.Contains("Monitor") || material.shader.name.Contains("Error") || !material.shader.name.Contains("Universal")))
                                Debug.Log("LOOK SCREEN // "+renderer.name+" // "+material.name+" // "+material.shader.name);
                    if(Physics.Raycast(new Vector3(-24,1000,-24),Vector3.down,out var ground,2000))
                    {
                        car.SetPositionAndRotation(ground.point+Vector3.up*3.2f,Quaternion.identity);
                        Physics.SyncTransforms();
                        var wheels=car.GetComponentsInChildren<WheelCollider>();
                        if(wheels.Length>0)
                        {
                            float bottom=wheels.Min(w=>w.transform.TransformPoint(w.center).y-w.radius*Mathf.Abs(w.transform.lossyScale.y));
                            car.position+=Vector3.up*(ground.point.y+.05f-bottom);
                        }
                    }
                    Physics.SyncTransforms();
                    var center=car.position+Vector3.up*2;
                    camera.transform.position=center+new Vector3(10,8,-14);
                    camera.transform.LookAt(center); camera.fieldOfView=55;
                }
                foreach (float hour in new[] { 12f, 17.5f, 22f })
                {
                    clock.SetTime(hour);
                    yield return Sample(camera, label, "time-" + hour.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    if(DrivingCore.Instance!=null && DrivingCore.Instance.Player!=null)
                    {
                        var savedPosition=camera.transform.position;
                        var savedRotation=camera.transform.rotation;
                        var carPosition=DrivingCore.Instance.Player.transform.position;
                        camera.transform.position=carPosition+new Vector3(10,5,18);
                        camera.transform.LookAt(carPosition+Vector3.up*2);
                        yield return Sample(camera,label,"driving-"+hour.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        camera.transform.SetPositionAndRotation(savedPosition,savedRotation);
                    }
                }
                if(label!="before" && DrivingCore.Instance!=null && DrivingCore.Instance.Player!=null)
                {
                    var car=DrivingCore.Instance.Player;
                    var lighting=car.GetComponent<VoyageVehicleLighting>();
                    car.SetHeadlights(true);
                    if(lighting!=null && lighting.Headlights.Count>0)
                    {
                        var beam=lighting.Headlights.FirstOrDefault(l=>l.transform.parent.name=="HeadLightFix") ?? lighting.Headlights[0];
                        Vector3 front=beam.transform.position;
                        Vector3 forward=Vector3.ProjectOnPlane(beam.transform.forward,Vector3.up).normalized;
                        camera.transform.position=front+forward*10+Vector3.up*4+Vector3.Cross(forward,Vector3.up)*5;
                        camera.transform.LookAt(front+forward*3); camera.fieldOfView=58;
                        yield return Sample(camera,label,"vehicle-headlights");
                        var cockpit=car.GetComponentsInChildren<Camera>(true).FirstOrDefault(c=>c.transform.parent!=null && c.transform.parent.name=="Camera_Anchor");
                        if(cockpit!=null) camera.transform.SetPositionAndRotation(cockpit.transform.position,cockpit.transform.rotation);
                        else { camera.transform.position=front-forward*2+Vector3.up; camera.transform.LookAt(front+forward*5+Vector3.up); }
                        camera.fieldOfView=65;
                        var ray=camera.ViewportPointToRay(new Vector3(.67f,.12f,0));
                        foreach(var renderer in car.GetComponentsInChildren<Renderer>())
                            if(renderer.bounds.IntersectRay(ray,out float distance) && distance<5)
                                Debug.Log("LOOK CABIN RAY // "+renderer.name+" // "+distance+" // "+string.Join(",",renderer.sharedMaterials.Select(m=>m==null?"null":m.name+":"+m.shader.name)));
                        yield return Sample(camera,label,"vehicle-cabin");
                    }
                    File.AppendAllText("Logs/LookReview/"+label+"/metrics.txt","Vehicle lights enabled: "+car.GetComponentsInChildren<Light>().Count(l=>l.enabled)+"\n");
                    car.SetHeadlights(false);
                }
                var npc=FindAnyObjectByType<Voyage.Dialogue.CoBluesDialogue>();
                if(npc!=null)
                {
                    var meshes=npc.GetComponentsInChildren<SkinnedMeshRenderer>();
                    var bounds=meshes.Length>0?meshes[0].bounds:new Bounds(npc.transform.position+Vector3.up,Vector3.one*2);
                    foreach(var mesh in meshes) bounds.Encapsulate(mesh.bounds);
                    var center=bounds.center;
                    float distance=Mathf.Max(4,bounds.size.y*1.75f);
                    camera.transform.position=center+npc.transform.forward*distance+Vector3.up*.3f;
                    camera.transform.LookAt(center); camera.fieldOfView=42;
                    File.AppendAllText("Logs/LookReview/"+label+"/metrics.txt","NPC bounds="+bounds+"\n");
                    clock.SetTime(17.5f); yield return Sample(camera,label,"character-dusk");
                    clock.SetTime(22); yield return Sample(camera,label,"character-night");
                    if(label!="before")
                    {
                        var rendererData=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
                        var ambientOcclusion=rendererData.rendererFeatures.FirstOrDefault(f=>f!=null && f.GetType().Name=="ScreenSpaceAmbientOcclusion");
                        if(ambientOcclusion!=null)
                        {
                            bool active=ambientOcclusion.isActive;
                            ambientOcclusion.SetActive(false);
                            yield return Sample(camera,label,"character-night-ao-off");
                            ambientOcclusion.SetActive(active);
                        }
                    }
                }
            }
            SessionState.SetBool("LookReview", false);
            File.WriteAllText("Logs/LookReview/" + label + "/complete.txt", "Completed");
            EditorApplication.Exit(0);
        }

        public static IEnumerator Sample(Camera camera, string label, string name)
        {
            var target = new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            target.Create();
            var previous = camera.targetTexture;
            bool wasEnabled=camera.enabled;
            camera.enabled=false;
            camera.targetTexture = target;
            yield return new WaitForSecondsRealtime(3);
            Debug.Log("LOOK STATE // "+name+" // controller="+(VoyageLookController.Instance==null?"NULL":VoyageLookController.Instance.ActiveProfile.name)+" // ambient="+RenderSettings.ambientEquatorColor+" // shape="+Shader.GetGlobalVector("_VoyageLookShape"));
            var times = new List<float>();
            var request=new RenderPipeline.StandardRequest { destination=target };
            for (int i=0;i<150;i++)
            {
                var timer=System.Diagnostics.Stopwatch.StartNew();
                RenderPipeline.SubmitRenderRequest(camera,request);
                // A batch Editor has no guaranteed Game view repaint. Explicit
                // rendering and readback ensure these timings contain GPU work.
                var readback=AsyncGPUReadback.Request(target,0); readback.WaitForCompletion();
                if(readback.hasError) Debug.LogError("LOOK CHECK // GPU readback failed");
                timer.Stop(); if(i>=30) times.Add((float)timer.Elapsed.TotalMilliseconds);
                yield return null;
            }
            times.Sort();
            File.AppendAllText("Logs/LookReview/"+label+"/metrics.txt", name+" median_ms="+times[60]+" p95_ms="+times[114]+" camera="+camera.transform.position+" rotation="+camera.transform.eulerAngles+"\n");
            var active = RenderTexture.active;
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest { destination=target });
            RenderTexture.active=target;
            var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,1920,1080),0,0); texture.Apply();
            File.WriteAllBytes("Logs/LookReview/"+label+"/"+name+".png",texture.EncodeToPNG());
            RenderTexture.active=active; camera.targetTexture=previous; camera.enabled=wasEnabled;
            Destroy(texture); target.Release(); Destroy(target);
        }
    }
}
#endif
