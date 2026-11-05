using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VoyageLookReview
{
    static VoyageLookReview()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("LookReview", false))
                new GameObject("Look review capture").AddComponent<Voyage.Tests.LookReviewProbe>();
        };
    }

    public static void Baseline()
    {
        SessionState.SetString("LookReviewLabel", "before");
        Begin("Assets/Scenes/SampleScene.unity");
    }

    public static void MainScene()
    {
        SessionState.SetString("LookReviewLabel", "after");
        Begin("Assets/Scenes/SampleScene.unity");
    }

    public static void MainIteration()
    {
        VoyageLookInstaller.Install();
        MainScene();
    }

    public static void TestScene()
    {
        SessionState.SetString("LookReviewLabel", "studio");
        Begin("Assets/Scenes/LookDevelopment.unity");
    }
    public static void StudioIteration()
    {
        VoyageLookInstaller.Install();
        TestScene();
    }

    static void Begin(string path)
    {
        Directory.CreateDirectory("Logs/LookReview");
        SessionState.SetBool("LookReview", true);
        EditorSceneManager.OpenScene(path);
        EditorApplication.EnterPlaymode();
    }
}
