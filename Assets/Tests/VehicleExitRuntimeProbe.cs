#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using Voyage.Exploration;

[DefaultExecutionOrder(1100)]
public sealed class VehicleExitRuntimeProbe : MonoBehaviour
{
    public ExplorationSession session;
    float elapsed;
    bool started, sawAir, sawLand, inputLocked=true, facingCorrect=true, cameraBehind=true;
    Vector3 inward, start;
    float startedAt;
    void LateUpdate()
    {
        elapsed+=Time.unscaledDeltaTime;
        if(!started && elapsed>2)
        {
            started=true;
            Time.timeScale=.15f;
            if(!session.ExitVehicle()){Finish(false,"Actual scene has no clear door exit");return;}
            start=session.Explorer.transform.position;inward=session.Explorer.transform.forward;startedAt=elapsed;
        }
        if(!started)return;
        var p=session.Explorer;
        if(p.ControlLocked)
        {
            Vector3 before=p.transform.position;
            p.Move(Vector2.one,true,1f/60,true);
            inputLocked&=Vector3.Distance(before,p.transform.position)<.0001f;
            facingCorrect&=Vector3.Dot(p.transform.forward,inward)>.999f;
            if(p.DismountPhase==1)sawAir=true;
            if(p.DismountPhase==2)sawLand=true;
            var cam=Camera.main;
            if(cam!=null && elapsed-startedAt>.1f)
                cameraBehind&=Vector3.Dot(Vector3.ProjectOnPlane(cam.transform.position-p.transform.position,Vector3.up).normalized,inward)<-.8f;
        }
        else
        {
            bool backward=Vector3.Dot(p.transform.position-start,inward)<-.5f;
            Finish(inputLocked&&facingCorrect&&cameraBehind&&sawAir&&sawLand&&backward&&p.IsGrounded,
                $"locked={inputLocked}, faceVehicle={facingCorrect}, rearCamera={cameraBehind}, air={sawAir}, land={sawLand}, backward={backward}, grounded={p.IsGrounded}");
        }
        if(elapsed>25)Finish(false,"Dismount timed out before landing");
    }
    void Finish(bool pass,string message)
    {
        Time.timeScale=1;enabled=false;
        File.WriteAllText("Logs/VehicleExitRuntime.txt",(pass?"PASS ":"FAIL ")+message);
        Debug.Log("VEHICLE EXIT: "+message);
        UnityEditor.EditorApplication.isPlaying=false;
    }
}
#endif
