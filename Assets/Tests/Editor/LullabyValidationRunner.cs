using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Voyage.Tests.Editor
{
    public static class LullabyValidationRunner
    {
        [MenuItem("Voyage/Validate Lullaby Character")]
        static void Run()
        {
            // Execute the synchronous assertions in place so the Test Runner
            // does not save/reload the user's unsaved terrain scene.
            int passed = 0, failed = 0;
            var report = new StringBuilder();
            void Check(string name, Action test)
            {
                try { test(); passed++; report.AppendLine("PASS " + name); }
                catch (Exception error) { failed++; report.AppendLine("FAIL " + name + "\n" + error); }
            }
            var character = new LullabyCharacterTests();
            Check("Import settings", character.ExportedClipsHaveCorrectLoopAndRootMotionSettings);
            Check("Player model and animation", () => character.CharacterFitsColliderFacesForwardAndAnimationsDeformBones("ExplorerPlayer"));
            Check("NPC model and animation", () => character.CharacterFitsColliderFacesForwardAndAnimationsDeformBones("NpcCapsule"));
            foreach (float gait in new[] { 0f, 1f, 2f })
                Check("Jump transitions, gait " + gait, () => character.JumpTransitionsFollowPhysicsAndCanBeInterrupted(gait));
            foreach (var method in typeof(CharacterGroundingTests).GetMethods())
            {
                if (!Attribute.IsDefined(method, typeof(TestAttribute))) continue;
                Check(method.Name, () =>
                {
                    var test = new CharacterGroundingTests();
                    try { test.Setup(); method.Invoke(test, null); }
                    finally { test.Cleanup(); }
                });
            }
            foreach (var method in typeof(ExplorerTests).GetMethods())
            {
                if (!Attribute.IsDefined(method, typeof(TestAttribute))) continue;
                Check(method.Name, () =>
                {
                    var test = new ExplorerTests();
                    try { test.Setup(); method.Invoke(test, null); }
                    finally { test.Cleanup(); }
                });
            }
            report.AppendLine($"{passed} passed, {failed} failed");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/LullabyValidation.txt", report.ToString());
            Debug.Log("LULLABY VALIDATION: " + report);
        }
    }
}
