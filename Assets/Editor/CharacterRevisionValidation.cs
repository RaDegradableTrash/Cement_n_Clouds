using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Voyage.Exploration;
using Object=UnityEngine.Object;

public static class CharacterRevisionValidation
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    [MenuItem("Voyage/Validate Character Revision")]
    public static void Run()
    {
        var log=new StringBuilder();int passed=0,failed=0;
        void Check(string name,Action test){try{test();log.AppendLine("PASS "+name);passed++;}catch(Exception e){log.AppendLine("FAIL "+name+": "+e);failed++;}}
        void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        var root=new GameObject("Character revision validation");root.transform.position=new Vector3(9000,1000,9000);
        try
        {
            Check("Orbit keeps requested radius through repeated turns",()=>{
                var target=new GameObject("Target");target.transform.SetParent(root.transform,false);
                var camObject=new GameObject("Validation camera");camObject.transform.SetParent(root.transform,false);
                var cam=camObject.AddComponent<FollowCamera>();cam.SetOnFoot(true);cam.SetTarget(target.transform);
                typeof(FollowCamera).GetField("hadFocus",Flags).SetValue(cam,false);
                for(int i=0;i<1440;i++){
                    typeof(FollowCamera).GetField("currentYaw",Flags).SetValue(cam,(float)i);
                    typeof(FollowCamera).GetMethod("LateUpdate",Flags).Invoke(cam,null);
                    float radius=Vector3.Distance(cam.transform.position,target.transform.position+Vector3.up*cam.onFootFocusHeight);
                    Require(Mathf.Abs(radius-6.5f)<.005f,"orbit radius drifted: "+radius);
                }
                var solve=typeof(FollowCamera).GetMethod("ResolveCollision",Flags);
                var distance=typeof(FollowCamera).GetField("currentCollisionDistance",Flags);
                Vector3 pivot=target.transform.position;
                solve.Invoke(cam,new object[]{pivot,pivot+Vector3.back*2,false});
                Require(Mathf.Abs((float)distance.GetValue(cam)-6.5f)<.005f,"safety cast changed persistent radius");
                Object.DestroyImmediate(camObject);Object.DestroyImmediate(target);
            });
            var player=Object.Instantiate(Resources.Load<GameObject>("Prefabs/ExplorerPlayer"),root.transform);
            player.transform.localPosition=Vector3.zero;
            var animator=player.GetComponentInChildren<Animator>();
            Check("Imported elbow flexion and rearward forearm recovery",()=>{
                var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/VoyageLullaby/VoyageLullaby.fbx").OfType<AnimationClip>().ToDictionary(c=>c.name);
                var bones=animator.GetComponentsInChildren<Transform>();
                foreach(string gait in new[]{"Walk","Run"})foreach(string side in new[]{"L","R"}){
                    var upper=bones.Single(b=>b.name=="UpperArm."+side);
                    var elbow=bones.Single(b=>b.name=="Forearm."+side);
                    var hand=bones.Single(b=>b.name=="Hand."+side);
                    float min=180,max=0,rear=1,front=-1,sideGap=float.MaxValue;
                    var clip=clips["VOY_"+gait];
                    for(int i=0;i<=120;i++){
                        clip.SampleAnimation(animator.gameObject,clip.length*i/120f);
                        var a=elbow.position-upper.position;var b=hand.position-elbow.position;
                        float angle=Vector3.Angle(a,b);min=Mathf.Min(min,angle);max=Mathf.Max(max,angle);
                        float forward=Vector3.Dot(b.normalized,player.transform.forward);
                        rear=Mathf.Min(rear,forward);front=Mathf.Max(front,forward);
                        sideGap=Mathf.Min(sideGap,Mathf.Abs(Vector3.Dot(hand.position-upper.position,player.transform.right)));
                    }
                    log.AppendLine($"ARM {gait}.{side}: flexion={min:F2}..{max:F2}, forward={rear:F3}..{front:F3}");
                    Require(min>=29 && max<=61 && max-min>15,"elbow flexion outside natural 30..60 range");
                    Require(rear<-.04f && front>.4f,"forearm never recovers rearward");
                    log.AppendLine($"ARM lateral shoulder-to-hand clearance={sideGap:F3}m");
                    Require(sideGap>.24f,"moving arm still hugs the torso");
                }
                animator.Rebind();
            });
            Check("633 clips and all 448 phase states",()=>{
                var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/VoyageLullaby/VoyageLullaby.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
                Require(clips.Length==633,"clip count "+clips.Length);
                animator.Rebind();animator.Update(0);
                foreach(var c in clips.Where(c=>c.name.StartsWith("PHASE_")))Require(animator.HasState(0,Animator.StringToHash(c.name)),"state missing "+c.name);
                Require(Mathf.Abs(player.GetComponent<CharacterController>().height-3.2f)<.01f,"size not 1.6x");
                Require(Mathf.Abs(VoyageCharacterInstaller.GeometryBounds(player.GetComponentsInChildren<SkinnedMeshRenderer>()).size.y-3.2f)<.08f,"visual/collider mismatch");
            });
            Check("Breathing remains small and longitudinal in all imported clips",()=>{
                var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/VoyageLullaby/VoyageLullaby.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__"));
                var chest=animator.GetComponentsInChildren<Transform>().Single(b=>b.name=="Chest");
                float sideways=0,vertical=0;
                foreach(var clip in clips)for(int i=0;i<=8;i++){
                    clip.SampleAnimation(animator.gameObject,clip.length*i/8f);
                    var scale=chest.localScale;
                    sideways=Mathf.Max(sideways,Mathf.Abs(scale.x-1),Mathf.Abs(scale.z-1));
                    vertical=Mathf.Max(vertical,Mathf.Abs(scale.y-1));
                }
                log.AppendLine($"BREATH lateralScaleDelta={sideways:F5}, longitudinalScaleDelta={vertical:F5}");
                Require(sideways<.002f && vertical<.008f && vertical>.003f,"breathing is inflated, missing or on the wrong axis");
                animator.Rebind();
            });
            Check("Every phase transition starts at the corresponding authored gait frame",()=>{
                var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/VoyageLullaby/VoyageLullaby.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToDictionary(c=>c.name);
                var bones=animator.GetComponentsInChildren<Transform>();
                foreach(var entry in clips.Where(c=>c.Key.StartsWith("PHASE_") || c.Key.StartsWith("JUMP_")))
                {
                    var parts=entry.Key.Split('_');var source=clips["VOY_"+parts[1]];int frame=int.Parse(parts[2]);
                    source.SampleAnimation(animator.gameObject,frame/30f);
                    var rotations=bones.Select(b=>b.localRotation).ToArray();var positions=bones.Select(b=>b.localPosition).ToArray();
                    entry.Value.SampleAnimation(animator.gameObject,0);
                    for(int i=0;i<bones.Length;i++){
                        Require(Quaternion.Angle(rotations[i],bones[i].localRotation)<.15f,entry.Key+" rotation discontinuity: "+bones[i].name);
                        Require(Vector3.Distance(positions[i],bones[i].localPosition)<.001f,entry.Key+" position discontinuity: "+bones[i].name);
                    }
                }
                animator.Rebind();
            });
            Check("Jump lead lifts first, lands first and standing jumps cover all three variants",()=>{
                var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/VoyageLullaby/VoyageLullaby.fbx").OfType<AnimationClip>().ToDictionary(c=>c.name);
                var bones=animator.GetComponentsInChildren<Transform>();
                var left=bones.Single(b=>b.name=="Foot.L");var right=bones.Single(b=>b.name=="Foot.R");
                foreach(string lead in new[]{"L","R"})
                {
                    var first=lead=="L"?left:right;var second=lead=="L"?right:left;
                    clips["VOY_JumpRise_Lead"+lead].SampleAnimation(animator.gameObject,0);
                    Require(first.position.y>second.position.y+.08f,"lead foot did not lift first: "+lead);
                    clips["VOY_JumpFallLoop_Lead"+lead].SampleAnimation(animator.gameObject,0);
                    Require(first.position.y<second.position.y-.05f,"lead foot did not extend for landing: "+lead);
                    clips["VOY_JumpLandToIdle_Lead"+lead].SampleAnimation(animator.gameObject,0);
                    Require(first.position.y<second.position.y-.08f,"lead foot did not land first: "+lead);
                }
                var driver=player.GetComponent<CharacterLocomotionAnimator>();
                var variants=new System.Collections.Generic.HashSet<int>();
                for(int i=0;i<6;i++)variants.Add(driver.SelectJumpLead(false));
                Require(variants.SetEquals(new[]{-1,0,1}),"standing jump variants missing");
                animator.Rebind();
            });
            Check("Driver chooses the matching jump transition for all 56 gait frames",()=>{
                var driver=player.GetComponent<CharacterLocomotionAnimator>();
                var select=typeof(CharacterLocomotionAnimator).GetMethod("PlayPhaseTransition",Flags);
                foreach(string gait in new[]{"Walk","Run"})for(int i=0;i<(gait=="Walk"?32:24);i++)
                {
                    animator.Rebind();animator.SetFloat("Locomotion",gait=="Walk"?1:2);animator.SetBool("Grounded",true);
                    animator.Play("Locomotion",0,i/(gait=="Walk"?32f:24f));animator.Update(0);
                    Require((bool)select.Invoke(driver,new object[]{"Jump"}),"phase not selected");
                    animator.SetBool("Grounded",false);animator.SetFloat("VerticalSpeed",5);animator.Update(.01f);
                    string expected=$"PHASE_{gait}_{i:00}_To_Jump";
                    Require(animator.GetNextAnimatorStateInfo(0).IsName(expected)||animator.GetCurrentAnimatorStateInfo(0).IsName(expected),"wrong phase: "+expected);
                    for(int j=0;j<35;j++){animator.SetFloat("AirTime",(j+1)/120f);animator.Update(1f/120);}
                    Require(animator.GetCurrentAnimatorStateInfo(0).IsName("JumpAir")||animator.GetNextAnimatorStateInfo(0).IsName("JumpAir"),"phase jump stalled: "+expected);
                }
                animator.Rebind();
            });
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform,false);floor.transform.localPosition=Vector3.down*.5f;floor.transform.localScale=new Vector3(100,1,100);
            var explorer=player.GetComponent<ExplorerPlayer>();explorer.enabled=false;
            Check("Backward dismount locks input, faces vehicle, lands then unlocks",()=>{
                Physics.SyncTransforms();Vector3 start=root.transform.position+Vector3.up*1.2f;
                explorer.BeginDismount(start,root.transform.position+Vector3.back*1.25f,Vector3.forward,Array.Empty<Collider>());
                Vector3 before=player.transform.position;explorer.Move(Vector2.right,true,.1f,true);
                Require(player.transform.position==before && explorer.ControlLocked,"input moved character during dismount");
                bool airborne=false;int i;
                for(i=0;i<240 && explorer.ControlLocked;i++){
                    explorer.TickDismount(1f/60);airborne|=explorer.DismountPhase==1;
                    Require(Vector3.Dot(player.transform.forward,Vector3.forward)>.999f,"dismount turned away from vehicle");
                }
                Require(airborne && !explorer.ControlLocked && explorer.IsGrounded,"dismount did not land/unlock");
                Require(player.transform.localPosition.z<-.9f,"jump was not backward");
            });
            Check("Actual RV door supplies a clear exit corridor",()=>{
                var rv=Object.Instantiate(Resources.Load<GameObject>("Prefabs/RV1.0"),root.transform);rv.transform.localPosition=Vector3.zero;
                try {
                    var session=root.AddComponent<ExplorationSession>();
                    var car=rv.GetComponent<PlayerCar>()??rv.AddComponent<PlayerCar>();
                    typeof(ExplorationSession).GetField("car",Flags).SetValue(session,car);
                    typeof(ExplorationSession).GetField("control",Flags).SetValue(session,rv.GetComponent<CarControl>());
                    typeof(ExplorationSession).GetField("<Explorer>k__BackingField",Flags).SetValue(session,explorer);
                    player.transform.localPosition=Vector3.forward*30;Physics.SyncTransforms();
                    foreach(var r in rv.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Door")))log.AppendLine($"DOOR {r.name}: localCenter={r.bounds.center-root.transform.position}, size={r.bounds.size}");
                    bool valid=session.TryFindDoorExit(out var spawn,out var landing,out var inward);
                    log.AppendLine($"DOOR route: {valid}, spawn={spawn-root.transform.position}, landing={landing-root.transform.position}, inward={inward}");
                    Require(valid,"no usable door");
                    Require(Vector3.Dot((landing-spawn).normalized,inward)<-.2f,"exit is not backward");
                    // Session does not own this temporary player's lifetime.
                    typeof(ExplorationSession).GetField("<Explorer>k__BackingField",Flags).SetValue(session,null);
                    Object.DestroyImmediate(session);
                } finally {Object.DestroyImmediate(rv);}
            });
        }
        finally{Object.DestroyImmediate(root);}
        log.AppendLine($"{passed} passed, {failed} failed");Directory.CreateDirectory("Logs");File.WriteAllText("Logs/CharacterRevisionValidation.txt",log.ToString());Debug.Log(log);
    }
}

