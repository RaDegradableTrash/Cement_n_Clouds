using System.Linq;
using UnityEditor;
using UnityEngine;
using Voyage.Exploration;

// An isolated, unsaved preview that exercises the imported rig and real colliders.
// Closing the window restores the Scene view and removes only its own objects.
public sealed class CharacterGroundingPreview : EditorWindow
{
    GameObject stage, actor, ground;
    CharacterGrounding grounding;
    Animator animator;
    AnimationClip idle, walk, run;
    double last;
    float time;
    int scenario;
    bool playing = true;
    float simulationSpeed = .5f;
    bool jumpTriggered;
    SceneView sceneView;
    Vector3 oldPivot;
    Quaternion oldRotation;
    float oldSize;
    bool oldOrtho;
    Material floorMaterial;

    [MenuItem("Voyage/Preview Ground Adaptation")]
    public static void Open() => GetWindow<CharacterGroundingPreview>("Ground adaptation").Show();

    void OnEnable() { EditorApplication.update += Tick; last = EditorApplication.timeSinceStartup; }
    void OnDisable()
    {
        EditorApplication.update -= Tick;
        if (stage != null) DestroyImmediate(stage);
        if (floorMaterial != null) DestroyImmediate(floorMaterial);
        if (sceneView != null) sceneView.LookAt(oldPivot, oldRotation, oldSize, oldOrtho, true);
    }
    void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        stage = new GameObject("Grounding preview (unsaved)") { hideFlags = HideFlags.HideAndDontSave };
        stage.transform.position = new Vector3(8000, 1000, 8000);
        actor = Instantiate(Resources.Load<GameObject>("Prefabs/ExplorerPlayer"), stage.transform);
        actor.hideFlags = HideFlags.HideAndDontSave;
        actor.transform.localPosition = Vector3.zero;
        animator = actor.GetComponentInChildren<Animator>();
        animator.enabled = true;
        grounding = actor.GetComponent<CharacterGrounding>(); grounding.enabled = false;
        grounding.Initialize();
        var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/VoyageLullaby/VoyageLullaby.fbx").OfType<AnimationClip>().ToArray();
        idle = clips.Single(c => c.name == "VOY_Idle"); walk = clips.Single(c => c.name == "VOY_Walk"); run = clips.Single(c => c.name == "VOY_Run");
        ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.hideFlags = HideFlags.HideAndDontSave;
        ground.transform.SetParent(stage.transform, false);
        floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { hideFlags = HideFlags.HideAndDontSave };
        floorMaterial.color = new Color(.35f, .39f, .43f);
        ground.GetComponent<Renderer>().sharedMaterial = floorMaterial;
        sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null) sceneView = GetWindow<SceneView>();
        oldPivot = sceneView.pivot; oldRotation = sceneView.rotation; oldSize = sceneView.size; oldOrtho = sceneView.orthographic;
        sceneView.LookAt(stage.transform.position + Vector3.up, Quaternion.Euler(15, 205, 0), 2.8f, false, true);
        ResetScenario();
    }
    void ResetScenario()
    {
        if (stage == null) return;
        grounding.RestoreAnimationPose(); grounding.ResetContacts(); time = 0;
        jumpTriggered = false;
        animator.Rebind(); animator.SetFloat("GaitRate", 1); animator.SetFloat("Locomotion", 0);
        animator.SetBool("Grounded", true); animator.Play("Locomotion", 0, 0); animator.Update(0);
        actor.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        ground.transform.localScale = new Vector3(8, 1, 100);
        ground.transform.localPosition = new Vector3(0, -.5f, 35);
        ground.transform.localRotation = Quaternion.identity;
        if (scenario == 1) { ground.transform.localRotation = Quaternion.Euler(0, 0, 15); ground.transform.localPosition = new Vector3(0, -.55f, 0); }
        Physics.SyncTransforms();
    }
    void OnGUI()
    {
        if (stage == null) { if (GUILayout.Button("Start isolated preview")) Setup(); return; }
        EditorGUI.BeginChangeCheck();
        scenario = GUILayout.Toolbar(scenario, new[] { "Turn", "Slope", "Walk", "Run", "Jump" });
        if (EditorGUI.EndChangeCheck()) ResetScenario();
        playing = GUILayout.Toggle(playing, "Play");
        simulationSpeed = EditorGUILayout.Slider("Simulation speed", simulationSpeed, .1f, 1);
        grounding.weight = EditorGUILayout.Slider("Ground adaptation", grounding.weight, 0, 1);
        EditorGUILayout.LabelField("Left support", grounding.LeftPlanted.ToString());
        EditorGUILayout.LabelField("Right support", grounding.RightPlanted.ToString());
        EditorGUILayout.LabelField("Turn step", grounding.TurningStep.ToString());
        EditorGUILayout.LabelField("Time", time.ToString("F2"));
        if (GUILayout.Button("Restart")) ResetScenario();
        if (GUILayout.Button("Side view") && sceneView != null) sceneView.LookAt(actor.transform.position + Vector3.up*1.6f, Quaternion.Euler(8, 90, 0), 2.4f, false, true);
        EditorGUILayout.HelpBox("Inspect the Scene view. This preview does not save or reload your scene. Close this window to remove the preview.", MessageType.Info);
    }
    void Tick()
    {
        double now = EditorApplication.timeSinceStartup;
        float dt = Mathf.Min(.033f, (float)(now - last)) * simulationSpeed; last = now;
        if (stage == null || !playing || EditorApplication.isPlayingOrWillChangePlaymode) return;
        grounding.RestoreAnimationPose(); time += dt;
        var clip = scenario == 2 ? walk : scenario == 3 ? run : idle;
        if (scenario == 0) actor.transform.rotation = Quaternion.Euler(0, Mathf.Max(0, time - 1) * 45, 0);
        if (scenario == 2 || scenario == 3) actor.transform.localPosition += Vector3.forward * (dt * (scenario == 2 ? 4.5f : 8));
        if (actor.transform.localPosition.z > 70) ResetScenario();
        float rate = scenario == 2 ? Mathf.Clamp(4.5f / 2.9f, .65f, 2.2f) : scenario == 3 ? Mathf.Clamp(8f / 5.2f, .65f, 2.2f) : 1;
        if (scenario == 4)
        {
            float flight = time - .5f;
            float launchSpeed = Mathf.Sqrt(2 * 24 * 1.1f);
            bool grounded = flight <= 0 || flight >= 2 * launchSpeed / 24;
            if (flight >= 0 && !jumpTriggered) { animator.SetTrigger("Jump"); jumpTriggered = true; }
            float vertical = grounded ? -2 : launchSpeed - 24 * flight;
            actor.transform.localPosition = Vector3.up * (grounded ? 0 : launchSpeed * flight - 12 * flight * flight);
            animator.SetBool("Grounded", grounded); animator.SetFloat("VerticalSpeed", vertical);
            animator.SetFloat("AirTime", grounded ? 0 : flight); animator.Update(dt);
            grounding.Evaluate(dt, grounded, vertical);
            if (time >= 2) ResetScenario();
        }
        else
        {
            clip.SampleAnimation(animator.gameObject, time * rate % clip.length);
            grounding.Evaluate(dt, true, -2);
        }
        if (sceneView != null)
        {
            sceneView.pivot = actor.transform.position + Vector3.up*1.6f;
            sceneView.Repaint();
        }
        Repaint();
    }
}

