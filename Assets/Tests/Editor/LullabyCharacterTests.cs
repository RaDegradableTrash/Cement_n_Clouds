using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Voyage.Tests.Editor
{
    public sealed class LullabyCharacterTests
    {
        const string Model = "Assets/VoyageLullaby/VoyageLullaby.fbx";

        [Test]
        public void ExportedClipsHaveCorrectLoopAndRootMotionSettings()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(Model);
            Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Generic));
            Assert.That(importer.clipAnimations.Length, Is.EqualTo(633));
            foreach (var clip in importer.clipAnimations)
            {
                Assert.That(clip.name, Does.Not.Contain("Preview"));
                bool loop = new[] { "VOY_Idle", "VOY_Walk", "VOY_Run" }.Contains(clip.name) || clip.name.EndsWith("_Hold") || (clip.name.EndsWith("Loop") || clip.name.Contains("Loop_Lead"));
                Assert.That(clip.loopTime, Is.EqualTo(loop), clip.name);
                Assert.That(clip.lockRootPositionXZ, Is.True, clip.name);
            }
        }

        [TestCase(0f)]
        [TestCase(1f)]
        [TestCase(2f)]
        public void JumpTransitionsFollowPhysicsAndCanBeInterrupted(float gait)
        {
            var instance = Object.Instantiate(Resources.Load<GameObject>("Prefabs/ExplorerPlayer"));
            try
            {
                var animator = instance.GetComponentInChildren<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.SetFloat("Locomotion", gait);
                animator.SetBool("Grounded", true);
                animator.Play("Locomotion", 0, 0); animator.Update(0);
                if (gait == 0)
                {
                    var trace = new System.Text.StringBuilder();
                    animator.SetBool("Grounded", false); animator.SetFloat("VerticalSpeed", 7); animator.SetTrigger("Jump");
                    for (int i = 0; i < 48; i++)
                    {
                        animator.SetFloat("AirTime", (i + 1) / 120f);
                        animator.Update(1f / 120);
                        var current = animator.GetCurrentAnimatorStateInfo(0);
                        string label = new[] { "Locomotion", "JumpStart", "JumpRise", "JumpAir", "JumpFall", "JumpLand" }.FirstOrDefault(n => current.IsName(n));
                        trace.AppendLine($"{(i + 1) / 120f:F3}: {label} {current.normalizedTime:F3} transition={animator.IsInTransition(0)}");
                    }
                    System.IO.Directory.CreateDirectory("Logs"); System.IO.File.WriteAllText("Logs/JumpTiming.txt", trace.ToString());
                    animator.Rebind(); animator.SetFloat("Locomotion", gait); animator.SetBool("Grounded", true);
                    animator.Play("Locomotion", 0, 0); animator.Update(0);
                }
                float airTime = 0;
                void Advance(float seconds)
                {
                    for (int i = 0; i < Mathf.CeilToInt(seconds * 120); i++)
                    {
                        airTime = animator.GetBool("Grounded") ? 0 : airTime + 1f / 120;
                        animator.SetFloat("AirTime", airTime);
                        animator.Update(1f / 120);
                    }
                }
                void State(string expected)
                {
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(expected) ||
                        animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(expected), Is.True, expected);
                }
                animator.SetBool("Grounded", false); animator.SetFloat("VerticalSpeed", 7);
                animator.SetTrigger("Jump"); Advance(.02f); State("JumpStart");
                Advance(.10f); State("JumpRise");
                Advance(.18f); State("JumpAir");
                // Remain airborne until physics reports contact, even on long falls.
                animator.SetFloat("VerticalSpeed", -3); Advance(.1f); State("JumpFall");
                Advance(1); State("JumpFall");
                animator.SetBool("Grounded", true); Advance(.06f); State("JumpLand");
                animator.SetBool("Grounded", false); animator.SetFloat("VerticalSpeed", 7);
                animator.SetTrigger("Jump"); Advance(.02f); State("JumpStart");
                // A ceiling hit can cut the ascent short.
                animator.SetFloat("VerticalSpeed", -1); Advance(.1f); State("JumpFall");
                animator.SetBool("Grounded", true); Advance(.6f); State("Locomotion");
                // Walking off a ledge does not play an invented takeoff.
                animator.SetBool("Grounded", false); animator.SetFloat("VerticalSpeed", -2);
                animator.SetFloat("AirTime", .2f); Advance(.1f); State("JumpFall");
                Assert.That(instance.transform.position, Is.EqualTo(Vector3.zero));
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [TestCase("ExplorerPlayer")]
        [TestCase("NpcCapsule")]
        public void CharacterFitsColliderFacesForwardAndAnimationsDeformBones(string name)
        {
            var instance = Object.Instantiate(Resources.Load<GameObject>("Prefabs/" + name));
            try
            {
                var bones = instance.GetComponentsInChildren<Transform>();
                var foot = bones.Single(t => t.name == "Foot.R");
                var toe = bones.Single(t => t.name == "Toe.R");
                Assert.That(Vector3.ProjectOnPlane(toe.position - foot.position, Vector3.up).normalized.z, Is.GreaterThan(.99f));
                var meshes = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
                Assert.That(meshes.Length, Is.EqualTo(6));
                Assert.That(meshes.Any(m => m.name == "Wear_TShirt"), Is.True);
                Assert.That(meshes.Any(m => m.name == "Wear_LooseJacket"), Is.True);
                Assert.That(meshes.Any(m => m.name == "Wear_Collar" || m.name == "Wear_Buckle"), Is.False);
                Bounds bounds = default; bool first = true;
                var baked = new Mesh();
                try
                {
                    foreach (var renderer in meshes)
                    {
                        Assert.That(renderer.sharedMaterial.shader.name, Does.Contain("Universal Render Pipeline"));
                        var vertices = renderer.sharedMesh.vertices;
                        var weights = renderer.sharedMesh.boneWeights;
                        var skinMatrices = renderer.bones.Select((bone, i) => bone.localToWorldMatrix * renderer.sharedMesh.bindposes[i]).ToArray();
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            var w = weights[i]; var v = vertices[i];
                            Vector3 p = skinMatrices[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0
                                + skinMatrices[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1
                                + skinMatrices[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2
                                + skinMatrices[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;
                            if (first) { bounds = new Bounds(p, Vector3.zero); first = false; }
                            else bounds.Encapsulate(p);
                        }
                    }
                }
                finally { Object.DestroyImmediate(baked); }
                Assert.That(bounds.size.y, Is.EqualTo(3.2f).Within(.01f));
                Assert.That(bounds.min.y, Is.EqualTo(0).Within(.01f));
                var animator = instance.GetComponentInChildren<Animator>();
                Assert.That(animator.applyRootMotion, Is.False);
                var controller = (AnimatorController)animator.runtimeAnimatorController;
                var tree = (BlendTree)controller.layers[0].stateMachine.defaultState.motion;
                Assert.That(tree.children.Select(c => c.motion.name), Is.EqualTo(new[] { "VOY_Idle", "VOY_Walk", "VOY_Run" }));
                var clips = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>();
                foreach (string clipName in new[] { "VOY_Walk", "VOY_Run" })
                {
                    var clip = clips.Single(c => c.name == clipName);
                    var hand = bones.Single(t => t.name == "Hand.R");
                    Vector3 rootPosition = instance.transform.position;
                    clip.SampleAnimation(animator.gameObject, 0);
                    Vector3 start = hand.position;
                    clip.SampleAnimation(animator.gameObject, clip.length * .5f);
                    Assert.That(Vector3.Distance(hand.position, start), Is.GreaterThan(.01f), clipName + " must animate the mesh skeleton");
                    Assert.That(instance.transform.position, Is.EqualTo(rootPosition));
                    // Clothing must bind to the original skeleton; garment fixes
                    // must not reintroduce the discarded clothing-only bones.
                    Assert.That(bones.Any(t => t.name.StartsWith("SleeveFlex") || t.name.StartsWith("CoatHem.")), Is.False);
                    foreach (var garment in meshes.Where(m => m.name.StartsWith("Wear_")))
                    {
                        Assert.That(garment.bones.All(b => b != null && bones.Contains(b)), Is.True);
                        foreach (var weight in garment.sharedMesh.boneWeights)
                        {
                            Assert.That(weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3,
                                Is.EqualTo(1f).Within(.001f), garment.name + " unweighted garment vertex");
                        }
                    }
                }
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}

