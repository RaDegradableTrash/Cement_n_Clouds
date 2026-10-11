using System.IO;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Voyage.Dialogue;

public static class DialogueTools
{
    [MenuItem("Voyage/Dialogue/Run Scene Input Probe %#&v")]
    public static void RunSceneProbe()
    {
        if(!EditorApplication.isPlaying) { Debug.LogWarning("Enter Play Mode before running the scene input probe.");return; }
        var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView, UnityEditor"));view.Focus();
        if(UnityEngine.Object.FindAnyObjectByType<Voyage.Tests.DialogueRuntimeProbe>()==null)
            new GameObject("Dialogue input validation").AddComponent<Voyage.Tests.DialogueRuntimeProbe>();
    }
    [MenuItem("Voyage/Dialogue/Run Core Tests In Place %#&d")]
    public static void RunInPlace()
    {
        // These tests only create managed dialogue state; keep the user's dirty scene untouched.
        var type=Type.GetType("Voyage.Dialogue.Tests.ConversationTests, Voyage.Dialogue.Tests",true);
        int passed=0,failed=0;
        var report=new StringBuilder();
        foreach(var method in type.GetMethods().Where(m=>m.GetCustomAttributes(false).Any(a=>a.GetType().Name=="TestAttribute")))
        {
            try
            {
                object test=Activator.CreateInstance(type);
                type.GetMethod("Setup").Invoke(test,null);method.Invoke(test,null);
                report.AppendLine("PASS "+method.Name);passed++;
            }
            catch(Exception e) { failed++;report.AppendLine("FAIL "+method.Name+"\n"+(e.InnerException??e)); }
        }
        report.AppendLine("Passed: "+passed+", failed: "+failed);
        Directory.CreateDirectory("Logs/DialogueValidation");File.WriteAllText("Logs/DialogueValidation/core-tests.txt",report.ToString());
        if(failed>0)Debug.LogError(report.ToString());else Debug.Log(report.ToString());
    }
    [MenuItem("Voyage/Dialogue/Validate CoBlues Content")]
    public static void Validate()
    {
        var content=JsonUtility.FromJson<DialogueContent>(Resources.Load<TextAsset>("Dialogue/CoBlues").text);
        var errors=content.Validate();
        foreach(var error in errors)Debug.LogError(error);
        if(errors.Count==0)Debug.Log("CoBlues dialogue: "+content.nodes.Length+" valid nodes.");
    }
    [MenuItem("Voyage/Dialogue/Run Dialogue Tests")]
    public static void RunTests()
    {
        var api=ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Results());
        api.Execute(new ExecutionSettings(new Filter { testMode=TestMode.EditMode, assemblyNames=new[]{"Voyage.Dialogue.Tests"} }));
    }
    sealed class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            Directory.CreateDirectory("Logs/DialogueValidation");
            TestRunnerApi.SaveResultToFile(result,"Logs/DialogueValidation/results.xml");
            Debug.Log("Dialogue tests: "+result.PassCount+" passed, "+result.FailCount+" failed.");
        }
    }
}

public sealed class DialogueBuildValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        var asset=Resources.Load<TextAsset>("Dialogue/CoBlues");
        if(asset==null)throw new BuildFailedException("Missing CoBlues dialogue content");
        var errors=JsonUtility.FromJson<DialogueContent>(asset.text).Validate();
        if(errors.Count>0)throw new BuildFailedException(string.Join("\n",errors));
    }
}
