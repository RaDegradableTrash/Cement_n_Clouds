using UnityEditor;
using UnityEngine;
public static class TerrainProbeTools
{
    [MenuItem("Voyage/Terrain/Run Focused Regression Tests %#&g")]
    public static void Test()
    {
        if(EditorApplication.isPlaying)return;
        var type=System.Type.GetType("Voyage.Tests.Editor.DayNightVehicleIssueTests, Voyage.ColliderTools.Editor.Tests",true);
        var report=new System.Text.StringBuilder();int failures=0;
        foreach(string name in new[]{"ManagedSunUsesItsOwnBiasAndKeepsShadows","RegenerationAddsFuelWithoutPerStepLogs","EmptyFuelShutdownPreservesDriveGearAndVelocityAndRefillRestarts"})
        {
            try{type.GetMethod(name).Invoke(System.Activator.CreateInstance(type),null);report.AppendLine("PASS "+name);}
            catch(System.Exception e){failures++;report.AppendLine("FAIL "+name+" "+(e.InnerException??e));}
        }
        System.IO.Directory.CreateDirectory("Logs/TerrainVisualProbe");System.IO.File.WriteAllText("Logs/TerrainVisualProbe/regression.txt",report.ToString());
        if(failures==0)Debug.Log(report);else Debug.LogError(report);
    }
    [MenuItem("Voyage/Terrain/Run Seam and Performance Probe %#&t")]
    public static void Run()
    {
        if(!EditorApplication.isPlaying)return;
        EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView, UnityEditor")).Focus();
        if(Object.FindAnyObjectByType<Voyage.Tests.TerrainVisualProbe>()==null)new GameObject("Terrain visual probe").AddComponent<Voyage.Tests.TerrainVisualProbe>();
    }
}
