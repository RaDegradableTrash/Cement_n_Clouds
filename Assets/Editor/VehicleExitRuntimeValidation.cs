using UnityEditor;
using UnityEngine;
using Voyage.Exploration;

[InitializeOnLoad]
public static class VehicleExitRuntimeValidation
{
    const string Pending="Voyage.ExitValidation";
    static VehicleExitRuntimeValidation()=>EditorApplication.update+=Poll;
    [MenuItem("Voyage/Validate Vehicle Exit Runtime")]
    static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    static void Poll()
    {
        if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying)return;
        var s=ExplorationSession.Instance;
        if(s==null||s.Explorer==null)return;
        SessionState.SetBool(Pending,false);
        var probe=new GameObject("Vehicle exit validation").AddComponent<VehicleExitRuntimeProbe>();probe.session=s;
    }
}
