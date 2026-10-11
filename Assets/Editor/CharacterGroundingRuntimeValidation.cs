using System.Linq;
using UnityEditor;
using UnityEngine;
using Voyage.Exploration;

[InitializeOnLoad]
public static class CharacterGroundingRuntimeValidation
{
    const string Pending = "Voyage.GroundingRuntimeValidation";
    static CharacterGroundingRuntimeValidation() => EditorApplication.playModeStateChanged += OnPlay;

    [MenuItem("Voyage/Validate Ground Adaptation Runtime")]
    static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }
    static void OnPlay(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        var stage = new GameObject("Grounding runtime validation");
        stage.transform.position = new Vector3(8000, 1000, 8000);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.SetParent(stage.transform, false);
        floor.transform.localPosition = new Vector3(0, -.5f, 0);
        floor.transform.localScale = new Vector3(100, 1, 100);
        var actor = Object.Instantiate(Resources.Load<GameObject>("Prefabs/ExplorerPlayer"), stage.transform);
        actor.transform.localPosition = Vector3.zero;
        var player = actor.GetComponent<ExplorerPlayer>();
        player.enabled = false; // The probe supplies deterministic inputs.
        var probe = stage.AddComponent<CharacterGroundingRuntimeProbe>(); probe.player = player;
        var observer = stage.AddComponent<CharacterGroundingRuntimeObserver>();
        observer.probe = probe; observer.grounding = actor.GetComponent<CharacterGrounding>();
        observer.animator = actor.GetComponentInChildren<Animator>();
        observer.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var bones = actor.GetComponentsInChildren<Transform>();
        observer.left = bones.Single(t => t.name == "Foot.L"); observer.right = bones.Single(t => t.name == "Foot.R");
        Physics.SyncTransforms();
    }
}
