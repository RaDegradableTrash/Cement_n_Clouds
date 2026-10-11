using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Voyage.Tests.Editor
{
    public sealed class CharacterGroundingTests
    {
        GameObject root, character;
        Component grounding;
        Animator animator;
        AnimationClip idle;
        Transform left, right;
        Type type;
        object Call(string method, params object[] args) => type.GetMethod(method).Invoke(grounding, args);
        T Value<T>(string name) => (T)type.GetProperty(name).GetValue(grounding);

        [SetUp] public void Setup()
        {
            root = new GameObject("Ground adaptation validation");
            root.transform.position = new Vector3(8000, 1000, 8000);
            character = Object.Instantiate(Resources.Load<GameObject>("Prefabs/ExplorerPlayer"), root.transform);
            character.transform.localPosition = Vector3.zero;
            type = Type.GetType("Voyage.Exploration.CharacterGrounding, Assembly-CSharp", true);
            grounding = character.GetComponent(type);
            Assert.That(grounding, Is.Not.Null, "Install ground adaptation first");
            animator = character.GetComponentInChildren<Animator>();
            var bones = character.GetComponentsInChildren<Transform>();
            left = bones.Single(t => t.name == "Foot.L"); right = bones.Single(t => t.name == "Foot.R");
            Assert.That(Call("Initialize"), Is.EqualTo(true), "Leg chains must resolve on the actual imported rig");
            idle = AssetDatabase.LoadAllAssetsAtPath("Assets/VoyageLullaby/VoyageLullaby.fbx").OfType<AnimationClip>().Single(c => c.name == "VOY_Idle");
        }
        [TearDown] public void Cleanup() { if (root != null) Object.DestroyImmediate(root); }
        GameObject Floor(Vector3 position, Vector3 scale, float roll = 0)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(root.transform, false);
            floor.transform.localPosition = position; floor.transform.localScale = scale;
            floor.transform.localRotation = Quaternion.Euler(0, 0, roll);
            Physics.SyncTransforms();
            return floor;
        }
        void Frame(bool grounded = true, float vertical = 0)
        {
            Call("RestoreAnimationPose"); idle.SampleAnimation(animator.gameObject, 0);
            Call("Evaluate", 1f / 60, grounded, vertical);
        }

        [Test] public void FeetAdaptToDifferentHeightsWithoutChangingBoneLengths()
        {
            float lx = left.position.x - root.transform.position.x, rx = right.position.x - root.transform.position.x;
            Floor(new Vector3(lx, -.5f, 0), new Vector3(.28f, 1, 4));
            Floor(new Vector3(rx, -.38f, 0), new Vector3(.28f, 1, 4));
            float leftLength = left.parent.localPosition.magnitude + left.localPosition.magnitude;
            for (int i = 0; i < 60; i++) Frame();
            Assert.That(Value<bool>("LeftPlanted") && Value<bool>("RightPlanted"), Is.True);
            Assert.That(right.position.y - left.position.y, Is.EqualTo(.12f).Within(.025f));
            Assert.That(Vector3.Distance(left.position, Value<Vector3>("LeftTarget")), Is.LessThan(.025f));
            float after = left.parent.localPosition.magnitude + left.localPosition.magnitude;
            Assert.That(after, Is.EqualTo(leftLength).Within(.0001f));
        }

        [Test] public void TurningKeepsOneFootFixedAndStepsTheOther()
        {
            Floor(new Vector3(0, -.5f, 0), new Vector3(8, 1, 8));
            for (int i = 0; i < 20; i++) Frame();
            var l = left.position; var r = right.position;
            character.transform.rotation = Quaternion.Euler(0, 45, 0);
            for (int i = 0; i < 20 && !Value<bool>("TurningStep"); i++) Frame();
            Assert.That(Value<bool>("TurningStep"), Is.True);
            Assert.That(Value<bool>("LeftPlanted") ^ Value<bool>("RightPlanted"), Is.True);
            bool leftSupport = Value<bool>("LeftPlanted");
            var support = leftSupport ? left : right;
            var original = leftSupport ? l : r;
            for (int i = 0; i < 8; i++)
            {
                Frame();
                Assert.That(Vector3.Distance(support.position, original), Is.LessThan(.025f), "Support foot must stay in world space");
            }
            var swing = leftSupport ? right : left;
            Assert.That(swing.position.y, Is.GreaterThan(support.position.y + .04f), "Turning foot must actually lift");
            for (int i = 0; i < 80; i++) Frame();
            Assert.That(Value<bool>("TurningStep"), Is.False);
            Assert.That(Value<bool>("LeftPlanted") && Value<bool>("RightPlanted"), Is.True);
        }

        [Test] public void LargeDirectionChangeTurnsTheVisualGradually()
        {
            Floor(new Vector3(0, -.5f, 0), new Vector3(8, 1, 8));
            for (int i = 0; i < 20; i++) Frame();
            Quaternion before = animator.transform.rotation;
            character.transform.rotation = Quaternion.Euler(0, 180, 0); Frame();
            Assert.That(Quaternion.Angle(before, animator.transform.rotation), Is.LessThan(5));
            for (int i = 0; i < 160; i++) Frame();
            Assert.That(Quaternion.Angle(before, animator.transform.rotation), Is.GreaterThan(175));
            Assert.That(Value<bool>("TurningStep"), Is.False);
            Assert.That(Value<bool>("LeftPlanted") && Value<bool>("RightPlanted"), Is.True);
        }

        [Test] public void PlantedFootStaysOnLedgeUntilItActuallyLifts()
        {
            float footZ=(left.position.z+right.position.z)*.5f-root.transform.position.z;
            Floor(new Vector3(0, -.5f, footZ), new Vector3(4, 1, .14f));
            for (int i = 0; i < 20; i++) Frame();
            Assert.That(Value<bool>("LeftPlanted"), Is.True);
            Vector3 planted = left.position;
            character.transform.position += Vector3.forward * .13f;
            Frame();
            Assert.That(Value<bool>("LeftPlanted"), Is.True);
            Assert.That(Vector3.Distance(planted, left.position), Is.LessThan(.025f));
        }

        [Test] public void AirborneAndTeleportReleaseContactsAndCorrectionsDoNotAccumulate()
        {
            Floor(new Vector3(0, -.5f, 0), new Vector3(20, 1, 20));
            for (int i = 0; i < 60; i++) Frame();
            Vector3 settled = left.position;
            for (int i = 0; i < 300; i++) Frame();
            Assert.That(Vector3.Distance(left.position, settled), Is.LessThan(.002f));
            Call("RestoreAnimationPose"); idle.SampleAnimation(animator.gameObject, 0);
            Vector3 animated = left.position;
            Call("Evaluate", 1f / 60, false, 5f);
            Assert.That(Value<bool>("LeftPlanted") || Value<bool>("RightPlanted"), Is.False);
            Assert.That(Vector3.Distance(animated, left.position), Is.LessThan(.0001f));
            character.transform.position += Vector3.right * 4;
            for (int i = 0; i < 20; i++) Frame();
            Assert.That(Vector3.Distance(left.position, Value<Vector3>("LeftTarget")), Is.LessThan(.025f));
        }

        [Test] public void SlopeAlignsFeetAndNoGroundDoesNotInventSupport()
        {
            Vector3 soleUp = Quaternion.Inverse(left.rotation) * Vector3.up;
            var floor = Floor(new Vector3(0, -.55f, 0), new Vector3(8, 1, 8), 15);
            Frame();
            for (int i = 0; i < 30; i++) Frame();
            Assert.That(Value<bool>("LeftPlanted"), Is.True);
            Assert.That(Vector3.Angle(left.rotation * soleUp, floor.transform.up), Is.LessThan(1));
            // A tilted surface must not trigger endless idle turning steps.
            Assert.That(Value<bool>("TurningStep"), Is.False);
            floor.SetActive(false); Physics.SyncTransforms(); Frame();
            Assert.That(Value<bool>("LeftPlanted") || Value<bool>("RightPlanted"), Is.False);
        }

        [Test] public void MovingGaitsAlternateSupportAndKeepPlantedFootStable()
        {
            Floor(new Vector3(0, -.5f, 30), new Vector3(10, 1, 80));
            var report = new System.Text.StringBuilder();
            foreach (string name in new[] { "VOY_Walk", "VOY_Run" })
            {
                var clip = AssetDatabase.LoadAllAssetsAtPath("Assets/VoyageLullaby/VoyageLullaby.fbx").OfType<AnimationClip>().Single(c => c.name == name);
                Call("RestoreAnimationPose"); character.transform.localPosition = Vector3.zero; Call("ResetContacts");
                float speed = name == "VOY_Walk" ? 4.5f : 8;
                float rate = Mathf.Clamp(speed / (name == "VOY_Walk" ? 2.9f : 5.2f), .65f, 2.2f);
                int leftContacts = 0, rightContacts = 0, lockedFrames = 0;
                bool wasLeft = false, wasRight = false;
                Vector3 previousLeft = left.position, previousRight = right.position;
                float maxSlip = 0, minHeight = 100, maxHeight = -100;
                for (int i = 0; i < 240; i++)
                {
                    Call("RestoreAnimationPose");
                    character.transform.position += Vector3.forward * (speed / 60);
                    clip.SampleAnimation(animator.gameObject, i / 60f * rate % clip.length);
                    float height = left.position.y - character.transform.position.y;
                    minHeight = Mathf.Min(minHeight, height); maxHeight = Mathf.Max(maxHeight, height);
                    Call("Evaluate", 1f / 60, true, -2f);
                    bool plantedLeft = Value<bool>("LeftPlanted"), plantedRight = Value<bool>("RightPlanted");
                    if (plantedLeft && !wasLeft) leftContacts++;
                    if (plantedRight && !wasRight) rightContacts++;
                    if (plantedLeft && wasLeft) { maxSlip = Mathf.Max(maxSlip, Vector3.Distance(previousLeft, left.position)); lockedFrames++; }
                    if (plantedRight && wasRight) { maxSlip = Mathf.Max(maxSlip, Vector3.Distance(previousRight, right.position)); lockedFrames++; }
                    previousLeft = left.position; previousRight = right.position; wasLeft = plantedLeft; wasRight = plantedRight;
                }
                report.AppendLine($"{name}: duration={clip.length:F3}, height={minHeight:F3}..{maxHeight:F3}, contacts={leftContacts}/{rightContacts}, supportFrames={lockedFrames}, slip={maxSlip:F4}");
                System.IO.Directory.CreateDirectory("Logs"); System.IO.File.WriteAllText("Logs/GroundingGaits.txt", report.ToString());
                Assert.That(leftContacts, Is.GreaterThan(2), name + " left foot must repeatedly land");
                Assert.That(rightContacts, Is.GreaterThan(2), name + " right foot must repeatedly land");
                Assert.That(lockedFrames, Is.GreaterThan(20), name + " must have sustained support");
                Assert.That(maxSlip, Is.LessThan(.035f), name + " support foot slip per frame");
            }
        }
    }
}

