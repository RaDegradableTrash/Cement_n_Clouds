using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Voyage.Exploration;
using Object = UnityEngine.Object;

public static class VoyageCharacterInstaller
{
    const string ModelPath = "Assets/VoyageLullaby/VoyageLullaby.fbx";
    const string ControllerPath = "Assets/VoyageLullaby/VoyageLocomotion.controller";

    [MenuItem("Voyage/Refresh Character Prefabs")]
    public static void RefreshPrefabs()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        InstallPrefab("ExplorerPlayer", controller); InstallPrefab("NpcCapsule", controller);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Voyage/Install Ground Adaptation")]
    public static void InstallGroundAdaptation()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        ConfigureGaitRate(AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath));
        ConfigurePhysicalJumpTiming(AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath));
        foreach (string name in new[] { "ExplorerPlayer", "NpcCapsule" })
        {
            string path = "Assets/Resources/Prefabs/" + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var grounding = root.GetComponent<CharacterGrounding>();
                if (grounding == null) grounding = root.AddComponent<CharacterGrounding>();
                grounding.animator = root.GetComponentInChildren<Animator>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("LULLABY ground adaptation installed on player and NPC.");
    }

    static void ConfigureGaitRate(AnimatorController controller)
    {
        if (!controller.parameters.Any(p => p.name == "GaitRate"))
            controller.AddParameter(new AnimatorControllerParameter { name = "GaitRate", type = AnimatorControllerParameterType.Float, defaultFloat = 1 });
        var locomotion = controller.layers[0].stateMachine.states.Single(s => s.state.name == "Locomotion").state;
        locomotion.speedParameter = "GaitRate"; locomotion.speedParameterActive = true;
        EditorUtility.SetDirty(controller);
    }

    static void ConfigurePhysicalJumpTiming(AnimatorController controller)
    {
        var machine = controller.layers[0].stateMachine;
        var start = machine.states.Single(s => s.state.name == "JumpStart").state;
        var rise = machine.states.Single(s => s.state.name == "JumpRise").state;
        // The controller applies the jump impulse immediately. Do not replay
        // grounded anticipation in midair; enter at the push-off portion.
        foreach (var transition in machine.anyStateTransitions)
            if (transition.destinationState == start) transition.offset = .65f;
        start.speed = 1.25f;
        rise.speed = 1.3f;
        // With a late clip offset, an incoming blend can cross exitTime=1
        // before it is evaluated. Use the physics air clock, not a missed
        // animation loop boundary, to leave these short launch states.
        foreach (var state in new[] { start, rise })
            foreach (var transition in state.transitions)
                if (transition.destinationState.name == (state == start ? "JumpRise" : "JumpAir"))
                {
                    transition.hasExitTime = false;
                    transition.conditions = new[] { new AnimatorCondition {
                        mode = AnimatorConditionMode.Greater, parameter = "AirTime", threshold = state == start ? .075f : .20f } };
                }
        EditorUtility.SetDirty(controller);
    }

    [MenuItem("Voyage/Install Lullaby Character")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Install the character outside Play Mode.");
        AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        importer.optimizeGameObjects = false;
        var settings = importer.defaultClipAnimations;
        foreach (var clip in settings)
        {
            clip.name = clip.name.Split('|').Last();
            clip.loopTime = clip.name == "VOY_Idle" || clip.name == "VOY_Walk" || clip.name == "VOY_Run" || clip.name.EndsWith("_Hold") || (clip.name.EndsWith("Loop") || clip.name.Contains("Loop_Lead"));
            clip.loopPose = clip.loopTime;
            clip.lockRootRotation = true;
            clip.lockRootPositionXZ = true;
            clip.lockRootHeightY = true;
            clip.keepOriginalOrientation = true;
            clip.keepOriginalPositionXZ = true;
            clip.keepOriginalPositionY = true;
        }
        importer.clipAnimations = settings;
        importer.SaveAndReimport();
        var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);
        if (clips.Count != 633 || clips.Keys.Any(n => n.Contains("Preview")))
            throw new InvalidOperationException("Expected 633 exported character clips, found " + clips.Count);

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Locomotion", AnimatorControllerParameterType.Float);
            var tree = new BlendTree { name = "Idle Walk Run", blendType = BlendTreeType.Simple1D,
                blendParameter = "Locomotion", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(clips["VOY_Idle"], 0);
            tree.AddChild(clips["VOY_Walk"], 1);
            tree.AddChild(clips["VOY_Run"], 2);
            var state = controller.layers[0].stateMachine.AddState("Locomotion");
            state.motion = tree;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
        }
        ConfigureJump(controller, clips);
        foreach (var entry in controller.layers[0].stateMachine.states)
        {
            var state=entry.state;
            string[] names=state.name=="Locomotion"?new[]{"VOY_Idle","VOY_Walk","VOY_Run"}:state.name=="JumpStart"?new[]{"VOY_IdleToJump","VOY_WalkToJump","VOY_RunToJump"}:state.name=="JumpLand"?new[]{"VOY_JumpLandToIdle","VOY_JumpLandToWalk","VOY_JumpLandToRun"}:null;
            if(names!=null && state.motion is BlendTree tree)
            {
                var children=tree.children;for(int i=0;i<children.Length;i++)children[i].motion=clips[names[i]];tree.children=children;EditorUtility.SetDirty(tree);
            }
            if(state.name=="JumpRise")state.motion=clips["VOY_JumpRise"];
            if(state.name=="JumpAir")state.motion=clips["VOY_JumpAirLoop"];
            if(state.name=="JumpFall")state.motion=clips["VOY_JumpFallLoop"];
        }
        ConfigureGaitRate(controller);
        ConfigurePhysicalJumpTiming(controller);
        ConfigurePhaseTransitions(controller, clips);
        ConfigureJumpLeads(controller, clips);
        InstallPrefab("ExplorerPlayer", controller);
        InstallPrefab("NpcCapsule", controller);
        AssetDatabase.SaveAssets();
        Debug.Log("LULLABY INSTALL OK: player + NPC, 633 clips, left/right jump leads + phase transitions.");
    }

    static void ConfigureJumpLeads(AnimatorController controller, System.Collections.Generic.Dictionary<string, AnimationClip> clips)
    {
        if (!controller.parameters.Any(p => p.name == "JumpLead")) controller.AddParameter("JumpLead", AnimatorControllerParameterType.Float);
        BlendTree Tree(string name, string parameter)
        {
            var tree = AssetDatabase.LoadAllAssetsAtPath(ControllerPath).OfType<BlendTree>().FirstOrDefault(t => t.name == name);
            if (tree == null) { tree = new BlendTree { name = name }; AssetDatabase.AddObjectToAsset(tree, controller); }
            tree.blendType = BlendTreeType.Simple1D; tree.blendParameter = parameter; tree.useAutomaticThresholds = false;
            tree.children = Array.Empty<ChildMotion>(); return tree;
        }
        Motion Lead(string clip)
        {
            var tree = Tree("Lead " + clip, "JumpLead");
            tree.AddChild(clips[clip+"_LeadL"], -1); tree.AddChild(clips[clip], 0); tree.AddChild(clips[clip+"_LeadR"], 1);
            EditorUtility.SetDirty(tree); return tree;
        }
        var machine = controller.layers[0].stateMachine;
        foreach (var entry in machine.states)
        {
            var state = entry.state;
            if (state.name == "JumpStart" || state.name == "JumpLand")
            {
                var tree = Tree("Lead gait " + state.name, "Locomotion");
                string[] names = state.name == "JumpStart" ? new[]{"VOY_IdleToJump","VOY_WalkToJump","VOY_RunToJump"} : new[]{"VOY_JumpLandToIdle","VOY_JumpLandToWalk","VOY_JumpLandToRun"};
                for (int i = 0; i < names.Length; i++) tree.AddChild(Lead(names[i]), i);
                state.motion = tree;
            }
            else if (state.name == "JumpRise") state.motion = Lead("VOY_JumpRise");
            else if (state.name == "JumpAir") state.motion = Lead("VOY_JumpAirLoop");
            else if (state.name == "JumpFall") state.motion = Lead("VOY_JumpFallLoop");
            else if (state.name.StartsWith("PHASE_") && state.name.EndsWith("_To_Jump"))
            {
                var parts = state.name.Split('_');
                var tree = Tree("Lead " + state.name, "JumpLead");
                tree.AddChild(clips[$"JUMP_{parts[1]}_{parts[2]}_LeadL"], -1);
                tree.AddChild(clips[state.name], 0);
                tree.AddChild(clips[$"JUMP_{parts[1]}_{parts[2]}_LeadR"], 1);
                state.motion = tree;
            }
            EditorUtility.SetDirty(state);
        }
        EditorUtility.SetDirty(controller);
    }

    static void ConfigurePhaseTransitions(AnimatorController controller, System.Collections.Generic.Dictionary<string, AnimationClip> clips)
    {
        var machine = controller.layers[0].stateMachine;
        AnimatorState State(string name, string clip)
        {
            var state = machine.states.FirstOrDefault(s => s.state.name == name).state ?? machine.AddState(name);
            state.motion = clips[clip]; return state;
        }
        foreach (string name in new[] { "CarryBoth", "HandOnChest", "OfferOneHand", "OfferBothHands", "WaveHello" }) State("NPC_"+name,"NPC_"+name+"_Hold");
        foreach (string name in new[] { "DismountStart", "DismountAir", "DismountLand" }) State(name, "VOY_"+name);
        var locomotion = machine.states.Single(s=>s.state.name=="Locomotion").state;
        var rise = machine.states.Single(s=>s.state.name=="JumpRise").state;
        foreach (var pair in clips.Where(p=>p.Key.StartsWith("PHASE_")))
        {
            var parts=pair.Key.Split('_'); string destination=parts[4];
            var state=State(pair.Key,pair.Key);
            foreach(var old in state.transitions) state.RemoveTransition(old);
            var target=destination=="Jump" ? rise : new[]{"Idle","Walk","Run"}.Contains(destination) ? locomotion : machine.states.Single(s=>s.state.name=="NPC_"+destination).state;
            var transition=state.AddTransition(target); transition.hasFixedDuration=true;transition.duration=.025f;
            transition.hasExitTime=true;transition.exitTime=1;
            if (destination=="Jump")
            {
                transition.hasExitTime=false;
                transition.AddCondition(AnimatorConditionMode.Greater,.10f,"AirTime");
                foreach(string next in new[]{"JumpLand","JumpFall"})
                {
                    var cut=state.AddTransition(machine.states.Single(s=>s.state.name==next).state);
                    cut.hasExitTime=false;cut.hasFixedDuration=true;cut.duration=.03f;
                    cut.AddCondition(next=="JumpLand"?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"Grounded");
                    if(next=="JumpFall")cut.AddCondition(AnimatorConditionMode.Less,-.2f,"VerticalSpeed");
                }
            }
            else if(destination=="Walk" || destination=="Run") transition.offset=int.Parse(parts[2])/(parts[1]=="Walk"?32f:24f);
        }
        EditorUtility.SetDirty(controller);
    }

    static void ConfigureJump(AnimatorController controller, System.Collections.Generic.Dictionary<string, AnimationClip> clips)
    {
        void Parameter(string name, AnimatorControllerParameterType type)
        {
            if (!controller.parameters.Any(p => p.name == name)) controller.AddParameter(name, type);
        }
        Parameter("Grounded", AnimatorControllerParameterType.Bool);
        Parameter("VerticalSpeed", AnimatorControllerParameterType.Float);
        Parameter("AirTime", AnimatorControllerParameterType.Float);
        Parameter("Jump", AnimatorControllerParameterType.Trigger);
        var machine = controller.layers[0].stateMachine;
        if (machine.states.Any(s => s.state.name == "JumpStart")) return;
        AnimatorState State(string name, string clip)
        {
            var state = machine.AddState(name);
            state.motion = clips[clip];
            return state;
        }
        BlendTree GaitTree(string name, string idle, string walk, string run)
        {
            var tree = new BlendTree { name = name, blendType = BlendTreeType.Simple1D,
                blendParameter = "Locomotion", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(clips[idle], 0); tree.AddChild(clips[walk], 1); tree.AddChild(clips[run], 2);
            return tree;
        }
        var start = State("JumpStart", "VOY_IdleToJump");
        start.motion = GaitTree("Takeoff by gait", "VOY_IdleToJump", "VOY_WalkToJump", "VOY_RunToJump");
        var rise = State("JumpRise", "VOY_JumpRise");
        var air = State("JumpAir", "VOY_JumpAirLoop");
        var fall = State("JumpFall", "VOY_JumpFallLoop");
        var land = State("JumpLand", "VOY_JumpLandToIdle");
        land.motion = GaitTree("Landing by gait", "VOY_JumpLandToIdle", "VOY_JumpLandToWalk", "VOY_JumpLandToRun");
        var locomotion = machine.states.Single(s => s.state.name == "Locomotion").state;
        AnimatorStateTransition Link(AnimatorState from, AnimatorState to, float duration = .05f, bool exit = false)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = exit; transition.exitTime = 1;
            transition.hasFixedDuration = true; transition.duration = duration;
            transition.canTransitionToSelf = false;
            return transition;
        }
        var takeoff = machine.AddAnyStateTransition(start);
        takeoff.hasExitTime = false; takeoff.hasFixedDuration = true; takeoff.duration = .035f;
        takeoff.canTransitionToSelf = true;
        takeoff.AddCondition(AnimatorConditionMode.If, 0, "Jump");
        foreach (var state in new[] { start, rise, air, fall })
            Link(state, land, .025f).AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        foreach (var state in new[] { start, rise, air })
        {
            var descending = Link(state, fall);
            descending.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            descending.AddCondition(AnimatorConditionMode.Less, -.2f, "VerticalSpeed");
        }
        Link(start, rise, .02f, true);
        Link(rise, air, .04f, true);
        var ledge = Link(locomotion, fall);
        ledge.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        ledge.AddCondition(AnimatorConditionMode.Greater, .08f, "AirTime");
        Link(land, locomotion, .06f, true);
        // A ledge or another jump can interrupt recovery; never wait for idle.
        var leaveGround = Link(land, fall);
        leaveGround.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        leaveGround.AddCondition(AnimatorConditionMode.Greater, .08f, "AirTime");
        EditorUtility.SetDirty(controller);
    }

    static void InstallPrefab(string name, RuntimeAnimatorController controller)
    {
        string path = "Assets/Resources/Prefabs/" + name + ".prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            // Set an absolute size so repeated imports never multiply scale.
            var capsule = root.GetComponent<CharacterController>();
            var npcCapsule = root.GetComponent<CapsuleCollider>();
            if(capsule!=null){capsule.height=3.2f;capsule.radius=.56f;capsule.center=Vector3.up*1.6f;capsule.stepOffset=.48f;}
            if(npcCapsule!=null){npcCapsule.height=3.2f;npcCapsule.radius=.56f;npcCapsule.center=Vector3.up*1.6f;}
            // Only replace the placeholder visual; keep role, collider and prefab identity.
            var placeholder = root.transform.Find("Capsule visual");
            var existing = root.transform.Find("Lullaby visual");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), root.transform);
            model.name = "Lullaby visual";
            // FBX may retain the last action on bone transforms. Restore the
            // actual bind pose before fitting the model or caching foot anchors.
            var bindWorld = new System.Collections.Generic.Dictionary<Transform, Matrix4x4>();
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                for (int i = 0; i < skin.bones.Length; i++)
                    if (!bindWorld.ContainsKey(skin.bones[i])) bindWorld.Add(skin.bones[i], skin.transform.localToWorldMatrix * skin.sharedMesh.bindposes[i].inverse);
            int Depth(Transform t) { int depth=0; while(t.parent!=null){depth++;t=t.parent;}return depth; }
            foreach (var pair in bindWorld.OrderBy(p => Depth(p.Key)))
            {
                var local = pair.Key.parent.worldToLocalMatrix * pair.Value;
                pair.Key.SetLocalPositionAndRotation(local.GetColumn(3), local.rotation);
                pair.Key.localScale = local.lossyScale;
            }
            var bones = model.GetComponentsInChildren<Transform>(true);
            var foot = bones.Single(t => t.name == "Foot.R");
            var toe = bones.Single(t => t.name == "Toe.R");
            var forward = Vector3.ProjectOnPlane(toe.position - foot.position, Vector3.up);
            if (forward.sqrMagnitude < .000001f) throw new InvalidOperationException("Cannot determine model forward from the foot.");
            model.transform.rotation = Quaternion.FromToRotation(forward.normalized, Vector3.forward) * model.transform.rotation;

            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers.Length < 3 || !renderers.Any(m => m.name.StartsWith("Wear_"))) throw new InvalidOperationException("Expected body and skinned clothing meshes.");
            // Use bind-pose vertices, not the animation-expanded renderer bounds.
            Bounds bounds = GeometryBounds(renderers);
            float height = root.GetComponent<CharacterController>() != null ? root.GetComponent<CharacterController>().height : root.GetComponent<CapsuleCollider>().height;
            model.transform.localScale *= height / bounds.size.y;
            bounds = GeometryBounds(renderers);
            model.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            foreach (var renderer in renderers)
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => ClothingMaterial(m != null ? m.name : "Template_Skin")).ToArray();
            var animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            var driver = root.GetComponent<CharacterLocomotionAnimator>();
            if (driver == null) driver = root.AddComponent<CharacterLocomotionAnimator>();
            driver.animator = animator;
            driver.referenceWalkSpeed=2.9f;driver.referenceRunSpeed=5.2f;
            var grounding = root.GetComponent<CharacterGrounding>();
            if (grounding == null) grounding = root.AddComponent<CharacterGrounding>();
            grounding.animator = animator;
            grounding.probeUp=.64f;grounding.probeDown=.72f;grounding.pelvisTravel=.352f;grounding.turnStepHeight=.16f;
            if (placeholder != null) Object.DestroyImmediate(placeholder.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log($"LULLABY {name}: height={GeometryBounds(renderers).size.y:F3}, model scale={model.transform.localScale}, forward={Vector3.ProjectOnPlane(toe.position-foot.position, Vector3.up).normalized}");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static Material ClothingMaterial(string importedName)
    {
        string key = System.Text.RegularExpressions.Regex.Replace(importedName.Split(' ')[0], @"\.\d{3}$", "");
        var palette = new System.Collections.Generic.Dictionary<string,Color> {
            {"Casual_TShirt",new Color(.83f,.83f,.83f)}, {"Casual_Jacket",new Color(.24f,.24f,.24f)},
            {"Casual_Pants",new Color(.095f,.095f,.095f)}, {"Casual_Trousers",new Color(.11f,.11f,.11f)}, {"Casual_Shoes",new Color(.12f,.12f,.12f)},
            {"Template_Skin",new Color(.69f,.55f,.40f)}, {"Template_Inner",new Color(.72f,.75f,.68f)},
            {"Template_Coat",new Color(.13f,.28f,.27f)}, {"Template_Trousers",new Color(.15f,.19f,.23f)},
            {"Template_Boots",new Color(.16f,.12f,.09f)}, {"Template_Trim",new Color(.48f,.40f,.24f)},
            {"Template_Buckle",new Color(.66f,.46f,.18f)} };
        if (!palette.ContainsKey(key)) key = "Template_Skin";
        const string folder = "Assets/VoyageLullaby/Materials";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/VoyageLullaby", "Materials");
        string path = folder + "/" + key + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", palette[key]); material.SetFloat("_Smoothness", .05f);
        EditorUtility.SetDirty(material); return material;
    }

    public static Bounds GeometryBounds(SkinnedMeshRenderer[] renderers)
    {
        Bounds result = default;
        bool first = true;
        foreach (var renderer in renderers)
        {
            // Import-time bind-pose vertices avoid BakeMesh scale compensation
            // and animation-expanded renderer bounds when fitting the model.
            foreach (var vertex in renderer.sharedMesh.vertices)
            {
                var point = renderer.transform.TransformPoint(vertex);
                if (first) { result = new Bounds(point, Vector3.zero); first = false; }
                else result.Encapsulate(point);
            }
        }
        return result;
    }
}



