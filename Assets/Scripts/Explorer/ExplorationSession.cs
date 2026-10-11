using UnityEngine;
using Voyage.TerrainSystem;

namespace Voyage.Exploration
{
    [DefaultExecutionOrder(-300)]
    public sealed class ExplorationSession : MonoBehaviour
    {
        public static ExplorationSession Instance { get; private set; }
        public ExplorerPlayer Explorer { get; private set; }
        public bool OnFoot { get; private set; }
        public int TransitionFrame { get; private set; }=-1;
        PlayerCar car;
        CarControl control;
        FollowCamera cameraFollow;
        VehicleInteractable vehicleTarget;
        Rigidbody parkedBody;
        bool wasKinematic;
        string exitMessage;
        float exitMessageUntil;
        GameObject npc;
        bool dismountCamera;
        readonly RaycastHit[] groundHits=new RaycastHit[32];
        void Awake()=>Instance=this;
        public void Initialize(PlayerCar vehicle,FollowCamera follow)
        {
            if(Explorer!=null)return;
            car=vehicle;control=car.GetComponent<CarControl>();cameraFollow=follow;
            var playerPrefab=Resources.Load<GameObject>("Prefabs/ExplorerPlayer");
            var npcPrefab=Resources.Load<GameObject>("Prefabs/NpcCapsule");
            if(playerPrefab==null || npcPrefab==null){Debug.LogError("Explorer/NPC prefabs are missing. Run Voyage/Generate Explorer Prefabs.",this);return;}
            Explorer=Instantiate(playerPrefab).GetComponent<ExplorerPlayer>();
            Explorer.gameObject.SetActive(false);
            vehicleTarget=car.GetComponent<VehicleInteractable>();
            if(vehicleTarget==null)vehicleTarget=car.gameObject.AddComponent<VehicleInteractable>();
            vehicleTarget.displayName="Vehicle";vehicleTarget.prompt="Enter";vehicleTarget.interactable=false;
            if(TryFindExit(out Vector3 spawn))
            {
                // Reusable example NPC beside the starting vehicle, not a follower.
                if(TryGround(spawn+car.transform.forward*4f,out Vector3 npcPoint))
                {
                    npc=Instantiate(npcPrefab,npcPoint,Quaternion.identity);
                    npc.AddComponent<Voyage.Dialogue.CoBluesDialogue>().Initialize(Explorer);
                }
            }
        }
        void Update()
        {
            if (OnFoot && dismountCamera && Explorer != null && !Explorer.ControlLocked)
            {
                dismountCamera = false;
                if (cameraFollow != null) cameraFollow.SetDismountView(false);
                if (vehicleTarget != null) vehicleTarget.interactable = true;
            }
            if(car==null || Explorer==null || OnFoot || Time.timeScale<=0 || !Application.isFocused || VoyageCommandConsole.IsOpen || VoyageCommandConsole.ConsumedInputThisFrame)return;
            if(ExplorerPlayer.InteractPressed && !ExitVehicle())
            {
                exitMessage=control!=null && Mathf.Abs(control.CurrentSpeedKmh)>5 ? "Stop the vehicle before exiting." : "No clear ground beside the vehicle.";
                exitMessageUntil=Time.time+3;
            }
        }
        public bool ExitVehicle()
        {
            if(Explorer==null || OnFoot || (control!=null && Mathf.Abs(control.CurrentSpeedKmh)>5f) || !TryFindDoorExit(out Vector3 spawn, out Vector3 point, out Vector3 inward))return false;
            OnFoot=true;TransitionFrame=Time.frameCount;
            if(control!=null)control.ActiveControl=false;
            car.SetControlEnabled(false);
            // Park while walking: streamed terrain may eventually unload under
            // the unattended car. Preserve its pose until the player returns.
            parkedBody=car.GetComponent<Rigidbody>();
            if(parkedBody!=null)
            {
                wasKinematic=parkedBody.isKinematic;
                if(!wasKinematic){parkedBody.linearVelocity=Vector3.zero;parkedBody.angularVelocity=Vector3.zero;}
                parkedBody.isKinematic=true;
            }
            Explorer.transform.SetPositionAndRotation(spawn,Quaternion.LookRotation(inward));
            Explorer.gameObject.SetActive(true);
            Explorer.BeginDismount(spawn, point, inward, car.GetComponentsInChildren<Collider>());
            vehicleTarget.anchorOffset=car.transform.InverseTransformPoint(point+Vector3.up);
            vehicleTarget.interactable=false;
            dismountCamera = true;
            if(cameraFollow!=null){cameraFollow.SetOnFoot(true);cameraFollow.SetTarget(Explorer.transform);cameraFollow.SetDismountView(true,Explorer.transform.eulerAngles.y);}
            if(GrassInteractionSystem.Instance!=null)GrassInteractionSystem.Instance.SetTarget(Explorer.transform);
            return true;
        }
        public void EnterVehicle()
        {
            if(!OnFoot || Explorer==null || Explorer.ControlLocked)return;
            OnFoot=false;TransitionFrame=Time.frameCount;
            Explorer.gameObject.SetActive(false);vehicleTarget.interactable=false;
            if(control!=null)control.ActiveControl=true;
            car.SetControlEnabled(true);
            if(parkedBody!=null) { parkedBody.isKinematic=wasKinematic; if(!wasKinematic)parkedBody.WakeUp(); }
            if(cameraFollow!=null){cameraFollow.SetOnFoot(false);cameraFollow.SetTarget(car.transform);}
            if(GrassInteractionSystem.Instance!=null)GrassInteractionSystem.Instance.SetTarget(car.transform);
        }
        public bool TryFindExit(out Vector3 point)
        {
            for(int ring=0;ring<2;ring++)for(int i=0;i<8;i++)
            {
                Vector3 offset=Quaternion.Euler(0,i*45,0)*car.transform.right*(5+ring*2);
                if(TryGround(car.transform.position+offset,out point) &&
                    !Physics.CheckCapsule(point+Vector3.up*.45f,point+Vector3.up*1.6f,.42f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))return true;
            }
            point=default;return false;
        }
        public bool TryFindDoorExit(out Vector3 spawn, out Vector3 landing, out Vector3 inward)
        {
            spawn = landing = inward = default;
            Vector3 forward = control != null ? control.DriveForward : car.transform.right;
            forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            var capsule = Explorer.GetComponent<CharacterController>();
            float radius = capsule.radius, height = capsule.height;
            foreach (var door in car.GetComponentsInChildren<Renderer>())
            {
                // Exact door names exclude toolbox lids, handles and trim.
                if (door.name != "DoorL" && door.name != "DoorR" && door.name != "Door" && door.name != "Door (1)") continue;
                Bounds bounds = door.bounds;
                if (bounds.size.y < .75f) continue;
                float side = Mathf.Sign(Vector3.Dot(bounds.center - car.transform.position, right));
                Vector3 outward = right * side;
                float extent = Mathf.Abs(outward.x)*bounds.extents.x + Mathf.Abs(outward.z)*bounds.extents.z;
                Vector3 threshold = bounds.center + outward * (extent + radius + .08f);
                if (!TryGround(threshold + outward * 1.25f, out landing)) continue;
                if (Physics.CheckCapsule(landing+Vector3.up*(radius+.03f),landing+Vector3.up*(height-radius),radius,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) continue;
                spawn = new Vector3(threshold.x, Mathf.Max(bounds.min.y+.10f, landing.y+.65f), threshold.z);
                // Reject ceilings and obstacles along the backward jump corridor.
                bool blocked = false;
                float flight=(3.2f+Mathf.Sqrt(3.2f*3.2f+2*Explorer.gravity*Mathf.Max(0,spawn.y-landing.y)))/Explorer.gravity;
                for (int i=0;i<=8;i++)
                {
                    float t=i/8f, seconds=t*flight;
                    Vector3 p=spawn+Vector3.ProjectOnPlane(landing-spawn,Vector3.up)*t+Vector3.up*(3.2f*seconds-.5f*Explorer.gravity*seconds*seconds);
                    foreach (var hit in Physics.OverlapCapsule(p+Vector3.up*(radius+.08f),p+Vector3.up*(height-radius),radius,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                        if (!hit.transform.IsChildOf(car.transform) && !hit.transform.IsChildOf(Explorer.transform)) blocked=true;
                }
                if (blocked) continue;
                inward = -outward; return true;
            }
            return false;
        }
        bool TryGround(Vector3 around,out Vector3 point)
        {
            int count=Physics.RaycastNonAlloc(around+Vector3.up*12,Vector3.down,groundHits,40,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            float nearest=float.MaxValue;point=default;bool found=false;
            for(int i=0;i<count;i++)
            {
                var hit=groundHits[i];
                if(hit.transform.IsChildOf(car.transform) || Vector3.Angle(hit.normal,Vector3.up)>45 || hit.distance>=nearest)continue;
                nearest=hit.distance;point=hit.point+Vector3.up*.08f;found=true;
            }
            return found;
        }
        void OnDestroy()
        {
            if(OnFoot && parkedBody!=null)parkedBody.isKinematic=wasKinematic;
            if(Instance==this)Instance=null;
            if(Explorer!=null)Destroy(Explorer.gameObject);
            if(npc!=null)Destroy(npc);
        }
        void OnGUI()
        {
            if(Time.time<exitMessageUntil && !VoyageCommandConsole.IsOpen)
                GUI.Box(new Rect(Screen.width*.5f-180,Screen.height-100,360,40),exitMessage);
        }
    }
}
